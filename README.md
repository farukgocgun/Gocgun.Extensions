# Gocgun.Extensions

> **Beta.** This package is in beta: the public API may change between releases.

BCL helpers. No ASP.NET Core types and no Turkey-only calendar.

- Text: diacritics, ASCII-safe file names, masking, length and email checks. Case changes take a `CultureInfo`.
- `CleanFileName` maps dotless `ı` because it has no Unicode decomposition, then keeps only ASCII letters, digits, `_`, and `-`. Empty input returns a random GUID. Directory separators and `..` do not survive.
- Dates: `ToUtc` and `FromUtc` take a `TimeZoneInfo`. Clock reads take a `TimeProvider`. Week bounds take the first day of the week and keep the input `DateTimeKind`.
- Streams: `ToByteArray` leaves the caller stream open unless `leaveOpen` is false.
- Claims: `GetGuidClaim` reads the claim type the caller passes.

Tests are unit tests. These helpers do not cross a process or network boundary, so an integration project would not exercise a different seam.

License: MIT (`LICENSE`).

## Publishing to nuget.org

Run `scripts/publish.sh` (or `scripts/publish.ps1` on Windows). It dispatches the manual `release.yml` workflow of each dependency first, in dependency order (Gocgun.Extensions), then of this package, waiting for each run and stopping on the first failure. Requires the GitHub CLI signed in with `gh auth login`; the `nuget` environment and the nuget.org Trusted Publishing policy must exist in each repository.
