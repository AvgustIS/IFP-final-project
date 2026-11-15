# QA checklist — 15 November 2026

Completed automated and smoke checks (details in QA_REPORT_RU.md):

- [x] Debug and Release compilation; Release -warnaserror
- [x] 69 .NET tests pass
- [x] Six Playwright customer/admin workflows pass in Edge desktop/mobile and Firefox desktop
- [x] Home, catalog, product images and details
- [x] Search, sort, category, stock-only and reset
- [x] Cart add/increment/decrement/remove; refresh and sign-in persistence
- [x] Registration, duplicate email, wrong and correct password, logout
- [x] CSRF rejection; anonymous/customer/admin route restrictions
- [x] Checkout delivery/payment validation, DB prices, stock and cart clearing
- [x] Order history isolation; another user's success URL rejected
- [x] SQLite reopen preserves orders and stock
- [x] Product/category CRUD; cannot delete a category with products
- [x] Pending → Processing → Completed (service test)
- [x] Processing → Cancelled and inventory restoration (service and browser)
- [x] Concurrent last-unit checkout and concurrent cancellation (separate SQLite connections)
- [x] Order snapshots survive product deletion
- [x] Existing catalog is not reseeded after product deletion
- [x] NuGet vulnerability audit, including transitive packages
- [x] Release publish and Production-mode startup smoke check
- [x] Desktop/mobile screenshots and basic no-horizontal-overflow assertions
- [ ] Physical phone/tablet tests
- [ ] macOS/Linux runtime tests and Safari
- [ ] Screen reader / full accessibility audit
- [ ] Load benchmark and CPU/memory profiling
- [ ] Measured line/branch coverage
- [ ] Fill participants' full names in the presentation and perform the oral defense

A mobile viewport is emulation, not physical-device testing. Checked boxes describe the exact automated/smoke scope; they do not imply every possible browser, input or production failure was tested.
