using System.Diagnostics;
using StockLedgerRetail.Insights;

namespace StockLedgerRetail.Application.Insights;

/// <summary>
/// Pure C# successive-shortest-path min-cost flow on a bipartite surplus→need graph (per SKU).
/// Quantities are scaled to integers (×100) for the solver.
/// </summary>
internal static class TransferMinCostFlowSolver
{
    private const int Scale = 100;

    public static List<TransferRebalanceEngine.Allocation>? Solve(
        TransferRebalanceEngine.SkuNodes nodes,
        List<TransferRebalanceEngine.CandidateEdge> edges,
        TransferRebalanceOptions options)
    {
        if (edges.Count == 0)
        {
            return new List<TransferRebalanceEngine.Allocation>();
        }

        var timeoutMs = Math.Max(50, options.MinCostFlowTimeoutMs);
        var sw = Stopwatch.StartNew();
        var sourceIndex = nodes.Sources
            .Select((s, i) => (s.WarehouseId, i))
            .ToDictionary(x => x.WarehouseId, x => x.i);
        var destIndex = nodes.Destinations
            .Select((d, i) => (d.WarehouseId, i))
            .ToDictionary(x => x.WarehouseId, x => x.i);

        var nSrc = nodes.Sources.Count;
        var nDst = nodes.Destinations.Count;
        // Nodes: 0=superSource, 1..nSrc=sources, nSrc+1..nSrc+nDst=dests, last=superSink
        var superSource = 0;
        var superSink = nSrc + nDst + 1;
        var nodeCount = superSink + 1;

        var graph = new List<McEdge>[nodeCount];
        for (var i = 0; i < nodeCount; i++)
        {
            graph[i] = new List<McEdge>();
        }

        void AddEdge(int from, int to, int capacity, long cost)
        {
            var forward = new McEdge(to, capacity, cost, graph[to].Count);
            var backward = new McEdge(from, 0, -cost, graph[from].Count);
            graph[from].Add(forward);
            graph[to].Add(backward);
        }

        var totalSupply = 0;
        for (var i = 0; i < nSrc; i++)
        {
            var cap = ToScaled(nodes.Sources[i].Surplus);
            if (cap <= 0)
            {
                continue;
            }

            AddEdge(superSource, 1 + i, cap, 0);
            totalSupply += cap;
        }

        var totalDemand = 0;
        for (var i = 0; i < nDst; i++)
        {
            var dem = ToScaled(nodes.Destinations[i].Need);
            if (dem <= 0)
            {
                continue;
            }

            AddEdge(1 + nSrc + i, superSink, dem, 0);
            totalDemand += dem;
        }

        var flowOnCandidate = new int[edges.Count];
        for (var e = 0; e < edges.Count; e++)
        {
            var edge = edges[e];
            if (!sourceIndex.TryGetValue(edge.Source.WarehouseId, out var si)
                || !destIndex.TryGetValue(edge.Destination.WarehouseId, out var di))
            {
                continue;
            }

            var cost = CostToLong(TransferRebalanceEngine.EdgeCostForMinCostFlow(edge, options));
            // Capacity limited by min(surplus, need) scaled
            var cap = Math.Min(ToScaled(edge.Source.Surplus), ToScaled(edge.Destination.Need));
            if (cap <= 0)
            {
                continue;
            }

            // Record index in forward edge via parallel array keyed by graph edge later
            AddEdge(1 + si, 1 + nSrc + di, cap, cost);
            // Map last added forward edge to candidate index
            var fwd = graph[1 + si][^1];
            fwd.CandidateIndex = e;
            flowOnCandidate[e] = 0;
        }

        var targetFlow = Math.Min(totalSupply, totalDemand);
        if (targetFlow <= 0)
        {
            return new List<TransferRebalanceEngine.Allocation>();
        }

        var potential = new long[nodeCount];
        var parentNode = new int[nodeCount];
        var parentEdge = new int[nodeCount];
        var dist = new long[nodeCount];

        var sent = 0;
        while (sent < targetFlow)
        {
            if (sw.ElapsedMilliseconds > timeoutMs)
            {
                return null;
            }

            Array.Fill(dist, long.MaxValue / 4);
            Array.Fill(parentNode, -1);
            dist[superSource] = 0;

            // Bellman-Ford (handles 0 costs; graph is small per SKU)
            var updated = true;
            for (var iter = 0; iter < nodeCount && updated; iter++)
            {
                updated = false;
                for (var u = 0; u < nodeCount; u++)
                {
                    if (dist[u] >= long.MaxValue / 8)
                    {
                        continue;
                    }

                    for (var ei = 0; ei < graph[u].Count; ei++)
                    {
                        var edge = graph[u][ei];
                        if (edge.Capacity <= 0)
                        {
                            continue;
                        }

                        var reduced = edge.Cost + potential[u] - potential[edge.To];
                        var nd = dist[u] + reduced;
                        if (nd < dist[edge.To])
                        {
                            dist[edge.To] = nd;
                            parentNode[edge.To] = u;
                            parentEdge[edge.To] = ei;
                            updated = true;
                        }
                    }
                }
            }

            if (parentNode[superSink] < 0)
            {
                break;
            }

            for (var i = 0; i < nodeCount; i++)
            {
                if (dist[i] < long.MaxValue / 8)
                {
                    potential[i] += dist[i];
                }
            }

            var add = targetFlow - sent;
            for (var v = superSink; v != superSource; v = parentNode[v])
            {
                var u = parentNode[v];
                var edge = graph[u][parentEdge[v]];
                add = Math.Min(add, edge.Capacity);
            }

            if (add <= 0)
            {
                break;
            }

            for (var v = superSink; v != superSource; v = parentNode[v])
            {
                var u = parentNode[v];
                var edge = graph[u][parentEdge[v]];
                edge.Capacity -= add;
                graph[edge.To][edge.Rev].Capacity += add;
                if (edge.CandidateIndex >= 0)
                {
                    flowOnCandidate[edge.CandidateIndex] += add;
                }
                else if (graph[edge.To][edge.Rev].CandidateIndex >= 0)
                {
                    // reverse edge of a candidate — reduce tracked flow
                    flowOnCandidate[graph[edge.To][edge.Rev].CandidateIndex] -= add;
                }
            }

            sent += add;
        }

        var allocations = new List<TransferRebalanceEngine.Allocation>();
        for (var i = 0; i < edges.Count; i++)
        {
            if (flowOnCandidate[i] <= 0)
            {
                continue;
            }

            var qty = FromScaled(flowOnCandidate[i]);
            if (qty > 0)
            {
                allocations.Add(new TransferRebalanceEngine.Allocation(edges[i], qty));
            }
        }

        return allocations;
    }

    private static int ToScaled(decimal value) =>
        (int)Math.Floor(value * Scale);

    private static decimal FromScaled(int value) =>
        value / (decimal)Scale;

    private static long CostToLong(decimal cost) =>
        (long)Math.Round(cost * 1000m, MidpointRounding.AwayFromZero);

    private sealed class McEdge
    {
        public McEdge(int to, int capacity, long cost, int rev)
        {
            To = to;
            Capacity = capacity;
            Cost = cost;
            Rev = rev;
            CandidateIndex = -1;
        }

        public int To { get; }
        public int Capacity { get; set; }
        public long Cost { get; }
        public int Rev { get; }
        public int CandidateIndex { get; set; }
    }
}
