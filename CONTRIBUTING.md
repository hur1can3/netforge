# Contributing to NetForge

Thank you for investing your time in improving NetForge. This document describes how to set up your environment, propose changes, and align with the Fused Slice Architecture (FSA) rules (FSA-01..25).

## 1. Code of Conduct

Be respectful, constructive, and inclusive. Assume positive intent.

## 2. Architecture Guardrails

All contributions MUST respect the canonical rules in `fused_slice_architecture.md`:

- Critical: FSA-01,02,04,06,07,11,12,22
- High: FSA-03,05,08,09,10,15,16,17,18,19,20,21
- Recommended: FSA-13,14,23,24,25

Reference rule IDs in PR titles or descriptions where relevant, e.g.:

```text
feat(products): implement pricing recalculation (FSA-01, FSA-04)
```

## 3. Project Layout (Planned)

```text
/src
  /Core          (NetForge Core toolkit source distribution)
  /Domain        (Entities, ValueObjects, SmartEnums, Interfaces)
  /Features      ([FeatureName]Feature.cs files)
  /Infrastructure(DbContext, Repos, Specs Evaluator, Outbox, Clients, Decorators)
  /Presentation  (API Host, gRPC Host, AspNet Minimal API)
/tests
  /Domain        (Pure domain tests)
  /Features      (Mediator slice tests)
  /E2E           (Thin end-to-end contract tests)
```

## 4. Development Environment

- .NET 9 SDK (preview until RTM)
- Editor: VS Code or Rider (ensure C# Dev Kit if VS Code)
- Optional: Docker Desktop (future integration tests)

## 5. Git Workflow

1. Fork (if external) or branch from `main`.
2. Branch naming:
   - `feat/<area>-<short-desc>`
   - `fix/<area>-<issue>`
   - `chore/<area>-<task>`
3. Keep commits atomic; use Conventional Commit messages.
4. Rebase onto latest `main` before opening PR.

## 6. Commit Message Format

```text
<type>(<scope>): <summary>

[optional body]
[optional footer]
```

Types: `feat`, `fix`, `docs`, `refactor`, `perf`, `test`, `build`, `chore`.

Include rule IDs when enforcing or adding architectural changes.

## 7. Feature Implementation Checklist

Before submitting a feature PR:

- [ ] Single `[FeatureName]Feature.cs` file (FSA-01)
- [ ] Request/Response DTOs defined
- [ ] Validator nested (FSA-11)
- [ ] Handler returns `Result` / `Result<T>` (FSA-04)
- [ ] No direct `DbContext` usage (FSA-03)
- [ ] No `SaveChanges*` in handler/repo (FSA-06/FSA-12)
- [ ] Domain events added inside entities only (FSA-07)
- [ ] Specs used instead of raw queries (FSA-08)
- [ ] Logging + tracing structured (FSA-16)
- [ ] Idempotency considered for external side-effects (FSA-17)
- [ ] Paging enforced for collection queries (FSA-20)
- [ ] Concurrency token handled if mutable aggregate (FSA-21)
- [ ] Secrets not accessed directly (FSA-22)

## 8. Testing Expectations

Follow Test Pyramid (FSA-25):

- Domain tests: invariants, value object behavior
- Feature slice tests: mediator pipeline w/ in-memory infra doubles
- Minimal E2E: contract and wiring only

## 9. Performance & Observability

- Use structured logging templates
- Avoid premature caching (consider decorator: FSA-19)
- Always bound result sets (FSA-20)

## 10. Security & Configuration

- Use options binding in host layer (FSA-22)
- Avoid leaking secrets to logs

## 11. Versioning & Evolution

- Add new endpoints or DTO versions side-by-side (FSA-23)
- Avoid breaking existing contracts

## 12. Feature Flags

- Evaluate flags in host/behavior; pass boolean into request (FSA-24)

## 13. Submitting a PR

1. Ensure build & tests pass locally.
2. Update docs (`fused_slice_architecture.md` / `readme.md`) if rules or patterns change.
3. Provide a concise PR description referencing rule IDs.
4. Mark unchecked items with reasoning if deviating (must be justified & reviewed).

## 14. Adding or Modifying Rules

Propose in an issue or PR with:

- Proposed ID / or amendment of existing
- Priority justification
- Directive + Anti-Pattern + Rationale
- At least one Good / Bad code example

## 15. License

Contribution implies agreement with the MIT license in `LICENSE`.

---

Welcome aboard—build purposeful, cohesive slices! 🚀
