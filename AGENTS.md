# SecretSeal Engineering Rules

This file defines the working rules used in this project so the same style can be reused across repositories.

## 1) Project Identity

- Stack: ASP.NET Core Minimal API on `.NET 10` (`net10.0`) with C# nullable reference types enabled.
- App style: thin HTTP layer (`Program.cs`) + composition helpers (`StartupHelpers`) + layered class libraries.
- Frontend: static `wwwroot` pages using vanilla JavaScript modules (no framework).
- Quality mode: analyzers and code style are enforced in build; warnings are treated as errors.

## 2) Repository Structure and Layering

Use this structure and keep boundaries strict:

- `Abstractions`: contracts only (`I*` interfaces). No infrastructure logic.
- `Models`: domain/value objects (`Note`, `NoteId`) with invariants enforced in constructors/factories.
- `Transport`: API-facing DTOs, validation contracts/implementations, JSON converters.
- `Logic`: application use-cases and orchestrators (`INotesHandler`, cleaner flow).
- `Cryptography`: encryption/decryption implementation behind `ICryptoHelper`.
- `Storage`: EF Core context, entities, repositories, unit of work.
- `SecretSeal`: application host, DI wiring, routing, endpoints, hosted services, static files.
- `*.Tests`: one test project per production project.

Dependency direction must stay inward:

- `SecretSeal` can reference all app layers for composition.
- `Storage`, `Logic`, `Cryptography`, `Transport` may reference `Abstractions`/`Models` as needed.
- `Models` and `Abstractions` should remain low-level and stable.
- Never reference web host code from domain/library projects.

## 3) Runtime and Build Rules

For each project:

- `<TargetFramework>net10.0</TargetFramework>`
- `<Nullable>enable</Nullable>`
- `<ImplicitUsings>enable</ImplicitUsings>`
- `<NuGetAuditMode>all</NuGetAuditMode>`
- `<AnalysisMode>AllEnabledByDefault</AnalysisMode>`
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- `<CodeAnalysisTreatWarningsAsErrors>true</CodeAnalysisTreatWarningsAsErrors>`
- `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`
- `<GenerateDocumentationFile>True</GenerateDocumentationFile>`

Testing stack:

- `xUnit` + `FluentAssertions`.
- Integration tests use `Microsoft.AspNetCore.Mvc.Testing`.
- Use `coverlet.collector` for coverage collection.

## 4) C# Style Rules (from `.editorconfig`)

Formatting:

- `UTF-8`, `CRLF`, 4-space indentation, max line length `90`.
- Allman braces (open brace on new line).
- Prefer braces even for single-line blocks.
- `using` directives outside namespace; `System.*` first; grouped imports.

Naming:

- Interfaces start with `I`.
- Types and members use PascalCase.
- Private fields use `_camelCase`.
- `const` names use `ALL_CAPS`.
- Async methods end with `Async`.

Language preferences:

- Prefer `var` in most cases.
- Prefer file-scoped namespaces.
- Prefer null-propagation/coalescing and simplified boolean expressions.
- Avoid `this.` qualification for fields/methods/properties.
- Prefer pattern matching where it improves clarity.

Analyzer discipline:

- Style/naming/performance diagnostics are treated seriously (many as errors).
- Use targeted `#pragma` suppression only when justified and documented inline.

## 5) Coding Conventions in Practice

- Guard all external inputs with `ArgumentNullException.ThrowIfNull(...)`.
- Keep async flows fully async; pass `CancellationToken` through.
- In library/backend code, use `.ConfigureAwait(false)` on awaited calls.
- Keep classes `sealed` unless inheritance is intentional.
- Prefer immutable DTOs/records and small focused classes.
- Add XML docs for public APIs and important internals.
- Keep endpoint handlers thin; business logic belongs in `Logic` layer.

## 6) API and Domain Behavior Rules

- Endpoint style: Minimal APIs (`MapGet`, `MapPost`, `MapDelete`) with explicit return semantics.
- Error payload format: `{ error: "message" }`.
- One-time-read behavior is mandatory: note retrieval consumes/deletes note.
- Route ID type uses `ShortGuid` with custom route constraint + JSON converter.
- Validation happens before domain creation:
  - trim input,
  - reject empty/whitespace,
  - apply optional max length from config.
- Domain invariants:
  - `NoteId` cannot be empty GUID,
  - `Note` content cannot be null/empty/whitespace.

## 7) Configuration and DI Rules

- Use Options pattern with `.Bind(...).ValidateOnStart()`.
- Put option objects in dedicated `Configuration` folders.
- Validate config invariants (`DataAnnotations` + custom `Validate(...)` checks).
- Storage mode is configuration-driven (`InMemory` or `Database`).
- Register decorators via `Scrutor` (`INotesHandler` decorated by `CryptoNotesHandler`).
- Hosted/background work must run via `BackgroundService` and scoped dependencies.

## 8) Storage and Security Rules

- In database mode, use EF Core with explicit entity mapping and indexes.
- Keep repository logic in `Storage.Repositories`; coordinate commits via `IUnitOfWork`.
- Treat secrets as secrets:
  - do not hardcode production keys/passwords,
  - use environment variables/secret stores for sensitive config.
- Encryption key requirements:
  - 32-byte UTF-8 raw string, or
  - Base64/Base64url that decodes to 32 bytes.
- Client-side encryption contract (`id#k=...`) must remain compatible.

## 9) Frontend Rules (Static Web)

- Use ES modules and shared helpers (`shared.js`) to avoid duplication.
- No inline JavaScript in HTML; wire events in JS files.
- Keep UX deterministic for one-time secret flow (single-use open action).
- Keep styling tokenized via CSS variables in shared stylesheet.

## 10) Testing Rules

- Test naming pattern: `MethodWhenConditionExpectedBehavior`.
- Always use Arrange/Act/Assert sections with comments.
- Use `[Fact(DisplayName = "...")]` and `[Trait("Category", "Unit|Integration")]`.
- Cover:
  - happy path,
  - invalid input and null guards,
  - edge cases and regression behavior.
- New feature/fix requires matching tests in corresponding `*.Tests` project.

## 11) Definition of Done

Before merging, run:

```powershell
dotnet restore src/SecretSeal.slnx
dotnet build src/SecretSeal.slnx -c Release
dotnet test src/SecretSeal.slnx -c Release
```

A change is done only when:

- build is clean,
- tests pass,
- analyzer/style rules pass,
- architecture boundaries remain intact.

## 12) Reuse in Other Projects

To replicate this developer style in another repository:

- copy this `AGENTS.md`,
- copy `.editorconfig` rules,
- keep layered project split (`Abstractions`, `Models`, `Logic`, `Storage`, `Transport`, host, tests),
- enforce analyzer-as-error policy in all `.csproj` files,
- keep test and naming patterns identical.
