# StockLedger Retail

StockLedger Retail is a personal project focused on designing and building a retail inventory engine using a ledger-based approach.

The goal of this project is not only to improve software engineering skills, but also to deepen retail inventory domain knowledge through real-world business scenarios.

---

## Motivation

After working in retail-related systems, I realized that inventory management is one of the most critical and challenging domains in retail operations.

This project is an opportunity to:

- Practice system design and software architecture
- Explore inventory management concepts
- Build a long-term portfolio project
- Apply real-world retail business knowledge

---

## Core Concept

Instead of directly updating stock quantity, every inventory movement is recorded as a transaction.

Examples:

- Stock In
- Stock Out
- Stock Adjustment
- Warehouse Transfer (Future)

Current inventory is calculated and maintained from stock transactions.

---

## Version 1 Scope

### Product Management

Manage product master data:

- SKU
- Name
- Brand
- Category

### Warehouse Management

Manage warehouse information.

### Stock Ledger

Track every inventory movement.

### Current Stock

View current inventory quantity by product and warehouse.

### Inventory Dashboard

Basic inventory visibility and reporting.

---

## Planned Features

### Phase 1

- Product Management
- Warehouse Management
- Stock Ledger
- Inventory Adjustment

### Phase 2

- Warehouse Transfer
- Stock Counting
- Inventory Audit

### Phase 3

- Purchase Order
- Goods Receipt
- Inventory Analytics

---

## Architecture

The project will follow:

- Clean Architecture
- Domain-Driven Design (DDD)

---

## Technology Stack

### Backend

- .NET 9
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL

### Frontend

- Next.js
- TypeScript
- Tailwind CSS

### Infrastructure

- Docker

---

## Project Status

🚧 In Progress

Currently focusing on domain analysis and inventory business rules.
