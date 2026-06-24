import { execSync, spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { setTimeout as sleep } from "node:timers/promises";
import { fileURLToPath } from "node:url";

const PORT = Number(process.env.PORT || 3000);
const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const NEXT_DIR = path.join(ROOT, ".next");

function log(message) {
  console.log(`[dev] ${message}`);
}

function killPort(port) {
  if (process.platform === "win32") {
    try {
      const output = execSync("netstat -ano -p tcp", {
        encoding: "utf8",
        stdio: ["ignore", "pipe", "ignore"],
      });
      const pids = new Set();

      for (const line of output.split(/\r?\n/)) {
        if (!line.includes("LISTENING")) {
          continue;
        }
        const match = line.trim().match(/:(\d+)\s+[^\s]+\s+LISTENING\s+(\d+)$/i);
        if (!match) {
          continue;
        }
        const [, foundPort, pid] = match;
        if (Number(foundPort) === port && pid !== "0") {
          pids.add(pid);
        }
      }

      for (const pid of pids) {
        try {
          execSync(`taskkill /F /PID ${pid}`, { stdio: "ignore" });
          log(`Stopped process ${pid} on port ${port}`);
        } catch {
          // Process may already be gone.
        }
      }
    } catch {
      // No listeners on this port.
    }
    return;
  }

  try {
    const pids = execSync(`lsof -ti tcp:${port}`, { encoding: "utf8" })
      .split(/\s+/)
      .map((value) => value.trim())
      .filter(Boolean);

    for (const pid of pids) {
      try {
        process.kill(Number(pid), "SIGTERM");
        log(`Stopped process ${pid} on port ${port}`);
      } catch {
        // Ignore stale pid.
      }
    }
  } catch {
    // Port is free.
  }
}

function cleanNextDir() {
  fs.rmSync(NEXT_DIR, { recursive: true, force: true });
  log("Removed .next cache");
}

function startDevServer() {
  const useTurbo = process.env.NEXT_DISABLE_TURBO !== "1";
  const args = ["next", "dev", "--port", String(PORT)];
  if (useTurbo) {
    args.push("--turbo");
  }

  log(`Starting ${args.join(" ")}`);

  const child = spawn(process.platform === "win32" ? "npx.cmd" : "npx", args, {
    cwd: ROOT,
    stdio: "inherit",
    env: process.env,
  });

  child.on("exit", (code, signal) => {
    if (signal) {
      process.kill(process.pid, signal);
      return;
    }
    process.exit(code ?? 0);
  });
}

async function main() {
  process.chdir(ROOT);

  log(`Preparing dev server on port ${PORT}`);
  killPort(PORT);
  await sleep(400);
  cleanNextDir();
  startDevServer();
}

main().catch((error) => {
  console.error("[dev] Failed to start:", error);
  process.exit(1);
});
