# EricksonLopez.Specification - v1.0 Launch Plan

## Overview
This document outlines the strategy for announcing the v1.0 release of `EricksonLopez.Specification`. The goal is to position this library as the premier, AOT-ready, high-performance specification pattern implementation for modern .NET applications.

## Target Audience
- .NET Developers (especially those using .NET 8/9/10).
- Teams using Dapper who want strongly-typed queries.
- Teams adopting NativeAOT or building Microservices/Serverless APIs.
- Domain-Driven Design (DDD) practitioners.

## Value Proposition (The "Why")
- **AOT-First:** Built from the ground up for Native AOT. Zero reflection emit, zero runtime code generation.
- **Dapper Integration:** The only specification library that natively translates expression trees directly to safe, parameterized SQL for Dapper.
- **DDD Purity:** Keeps domain layers clean. No database concepts (like `Include` or `AsNoTracking`) leak into the domain.
- **Analyzer Guardrails:** Ships with Roslyn analyzers (SPEC001-SPEC011) to enforce best practices at compile-time.

## Launch Channels

### 1. Reddit (`r/dotnet`, `r/csharp`)
**Title:** Show r/dotnet: I built an AOT-ready Specification library that translates directly to Dapper SQL.
**Content Strategy:**
- Focus on the technical challenges solved (AOT + Expression tree translation).
- Provide a clear before/after example using Dapper.
- Emphasize the Roslyn analyzers that prevent common footguns.

### 2. X (Twitter)
**Content Strategy:**
- Thread detailing the journey of building a high-performance expression translator.
- Snippets of code showing the Roslyn analyzers in action.
- Mention/Tag prominent .NET community members if relevant.

### 3. Dev.to / Medium / Personal Blog
**Article Title:** "Why I wrote yet another Specification Library for .NET (and how it works with AOT and Dapper)"
**Structure:**
- The problem with existing libraries (Ardalis, LinqKit) regarding AOT and Dapper.
- The design philosophy (DDD purity, immutable descriptors).
- Deep dive into how the Expression-to-SQL translation works safely.
- Benchmarks showcasing the low overhead.

### 4. GitHub
- Ensure the repository has proper tags (`dotnet`, `specification-pattern`, `dapper`, `aot`, `csharp`).
- Pin the repository to the profile.
- Add an architecture diagram to the README (generated via Mermaid).

## Launch Timeline

| Day | Action |
|---|---|
| **T-Minus 1** | Verify NuGet packages, XML docs, and GitHub release draft. |
| **Launch Day** | Publish packages to NuGet.org. Push GitHub Release. |
| **Launch + 2h**| Post to Reddit `r/dotnet`. |
| **Launch + 4h**| Post Twitter/X thread. |
| **Launch + 1 Day** | Publish deep-dive article on Blog/Dev.to. |
| **Launch + 1 Week**| Review issues, respond to feedback, publish v1.0.1 if necessary. |
