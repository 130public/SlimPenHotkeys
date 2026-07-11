# Publishing SlimPen Hotkeys to the Microsoft Store

A step‑by‑step checklist for packaging and submitting this app to the
Microsoft Store. Items are roughly in the order you should do them.
Check each box as you go.

> **App at a glance** (affects several steps below)
> - WinUI 3 / Windows App SDK 2.2.0, .NET 8, single‑project **MSIX** package.
> - Ships as a **tray utility** that installs a **global low‑level keyboard
>   hook** and synthesizes keystrokes (`SendInput`). This requires the
>   **`runFullTrust`** restricted capability and needs a short justification
>   during submission (see step 7).
> - Declares a **`windows.startupTask`** extension (launch at login).
> - Targets **x86, x64, and ARM64**.

---

## 1. Prerequisites (accounts & tooling)

- [ ] **Microsoft Partner Center developer account.** Register at
  <https://partner.microsoft.com/dashboard/registration>. There is a
  one‑time registration fee (individual or company). Company accounts
  require verification and take longer — start this early.
- [ ] **Visual Studio 2022** (17.10+) with these workloads/components:
  - *.NET Desktop Development*
  - *Windows application development* (includes the Windows App SDK /
    single‑project MSIX packaging tools)
  - Windows 11 SDK (10.0.26100 or the version this project targets).
- [ ] Confirm the project builds and publishes locally (already verified):
  ```powershell
  cd app
  dotnet publish -c Release -r win-x64 --self-contained true -p:Platform=x64
  ```

## 2. Reserve the app name

- [ ] In Partner Center → **Apps and games → New product → MSIX or PWA app**.
- [ ] Reserve a **display name** (e.g. `SlimPen Hotkeys`). This name must be
  unique across the Store and is what users will see. Reserving it unlocks
  the identity values you need in step 3.

## 3. Set the package identity (critical)

The manifest currently uses **placeholder identity** values that the Store
will reject:
```
Name="1AF2EFCC-6542-49DC-B8F9-84D6C11FB15D"
Publisher="CN=SlimPenHotkeys"
PublisherDisplayName="SlimPen Hotkeys"
```
These **must** be replaced with the values Partner Center assigns to your
reserved app.

- [ ] Easiest method — let Visual Studio do it: right‑click the
  **SlimPenHotkeys** project → **Publish → Associate App with the Store…**,
  sign in, pick your reserved name. VS rewrites `Package.appxmanifest`
  `Identity/Name`, `Publisher`, and `Properties/PublisherDisplayName` for you.
- [ ] Get the exact values from Partner Center under
  **Product → Product management → Product identity**:
  - `Package/Identity/Name` — the assigned package name.
  - `Package/Identity/Publisher` — your `CN=…` publisher string.
  - `Package/Properties/PublisherDisplayName` — your publisher display name.
- [ ] Also update `mp:PhoneIdentity PhoneProductId` to match the new
  `Identity/Name` GUID (or remove the `mp:PhoneIdentity` element — it is a
  legacy Windows Phone artifact and not required for a desktop MSIX).
- [ ] **Do not** manually sign the package. Store submissions are re‑signed
  by Microsoft; you only upload an **unsigned** `.msixupload` (see step 6).

## 4. Manifest & metadata review

- [x] **Removed the invalid `systemAIModels` capability** — it was a template
  leftover, unused in code, and would fail certification. (Done in this repo.)
- [ ] Keep **`runFullTrust`** — it is genuinely required for the global
  keyboard hook. Be ready to justify it (step 7).
- [ ] Set a more descriptive **`Description`** in `uap:VisualElements`
  (currently just `"SlimPen Hotkeys"`). This shows in some Windows surfaces.
- [ ] Bump **`Version`** for every submission. Store requires the revision
  (4th) field to be `0` (e.g. `1.0.0.0`, then `1.0.1.0`). Update both
  `Package.appxmanifest` `Identity/Version` and, ideally, `app.manifest`
  `assemblyIdentity version`.
- [ ] Decide whether **launch‑at‑login should default to on**. The manifest
  `StartupTask Enabled="true"` means it is registered enabled by default;
  Windows still shows the user a toggle in Task Manager/Startup apps.
  Consider `Enabled="false"` so users opt in (the in‑app "Launch at login"
  checkbox already manages this at runtime).

## 5. Store‑listing assets you must create

The repo has tile/logo PNGs (scale‑200 only) but the **Store listing** needs
additional images uploaded in Partner Center:

- [ ] **App screenshots** — at least **one** (1366×768 or larger, .png).
  Capture the settings window and the Test page.
- [ ] **Store logo** 300×300 (Partner Center listing image).
- [ ] Optional but recommended: 71×71, 150×150, 310×310, 620×300 promotional
  images for better placement.
- [ ] Consider generating **multiple asset scales** (100/125/150/200/400) with
  the VS **Manifest Designer → Assets → Generate** tool so tiles look crisp
  on all DPIs. Currently only `scale-200` variants exist.

## 6. Build the upload package

Use the packaging wizard (produces the `.msixupload` bundle for all archs):

- [ ] Right‑click the project → **Package and Publish → Create App Packages…**.
- [ ] Choose **"Microsoft Store using <your reserved name>"**.
- [ ] Select architectures **x86, x64, ARM64** (produce a bundle covering all
  three so every device is supported).
- [ ] Use the **Release** configuration. Leave signing to the Store.
- [ ] The wizard outputs a single `SlimPenHotkeys_<version>_x86_x64_arm64_bundle.msixupload`
  under `app\AppPackages\…`. That `.msixupload` is what you upload.
- [ ] (Optional) Run the **Windows App Certification Kit (WACK)** at the end of
  the wizard, or standalone, and fix any failures before submitting. This
  catches most certification issues locally.

> **Note:** The packaging wizard writes output under `app\AppPackages\`, which
> is already in `app/.gitignore`, so packages won't be committed. Only upload
> the multi‑arch `.msixupload` bundle — never a `_Debug_Test` sideload package.

## 7. Create the submission in Partner Center

- [ ] **Pricing and availability** — set price (Free), markets, and release
  schedule.
- [ ] **Properties**:
  - Category: *Utilities & tools* (or *Productivity*).
  - Declare that the app **starts at startup / runs in the background** if
    prompted.
- [ ] **Age ratings** — complete the IARC questionnaire (this app has no
  objectionable content → expect the lowest rating).
- [ ] **Packages** — upload the `.msixupload` from step 6.
- [ ] **Store listing** — description, screenshots (step 5), search terms
  (e.g. "Surface Pen", "remap", "hotkey", "Wispr"), and a support/website URL.
- [ ] **Notes for certification (important for this app):** explain the
  keyboard hook so a human reviewer doesn't flag it as keylogger‑like. Suggested:
  > "SlimPen Hotkeys installs a global low‑level keyboard hook to detect the
  > Surface Pen's button key events (F18/F19/F20) and remaps them to a
  > user‑configured keyboard shortcut via SendInput. It does not log, store,
  > or transmit keystrokes; the hook only inspects the configured trigger
  > keys. `runFullTrust` is required for the system‑wide hook. To reproduce:
  > open the app, assign a hotkey to Single Press, and press the pen's top
  > button — the Test page visualizes the trigger and the sent hotkey."

## 8. Certification gotchas specific to this app

- [ ] **`runFullTrust` justification** — restricted capability; expect manual
  review. The note in step 7 covers it.
- [ ] **Keyboard‑hook / input‑synthesis apps** sometimes get extra scrutiny.
  Keep the privacy story clear: no telemetry, no network, keystrokes never
  leave the device. State this in the listing/privacy policy.
- [ ] **Privacy policy URL** — required if the app is deemed to access personal
  data. Even though this app stores nothing off‑device, providing a short
  privacy statement URL (e.g. a GitHub page) avoids back‑and‑forth.
- [ ] **Trimming safety** — the Release/Store build is trimmed
  (`PublishTrimmed=true`). Settings serialization was switched to the
  **System.Text.Json source generator** so config load/save survives
  trimming. If you add new persisted types, add them to
  `SettingsJsonContext` in `app/Core/Settings.cs`, or settings will silently
  reset in the packaged build.
- [ ] **`dotnet build -c Release` alone fails** with NETSDK1102 because it is
  not self‑contained. That is expected — always build the package via the
  **Create App Packages** wizard or `dotnet publish … --self-contained true`
  (which the pubxml profiles under `app/Properties/PublishProfiles/` set up).

## 9. Submit & iterate

- [ ] Submit. Certification typically takes a few hours to a few days.
- [ ] If rejected, read the certification report, fix, **bump the version**,
  rebuild the package, and resubmit.
- [ ] After it goes live, verify install from the Store on a clean machine and
  confirm the pen mapping + launch‑at‑login work.

---

## Quick reference: what's already been handled in the repo

- Removed the invalid `systemAIModels` capability from `Package.appxmanifest`.
- Made settings JSON serialization trim‑safe (`SettingsJsonContext`) so the
  trimmed Store build persists configuration correctly.

## Quick reference: what you must still do manually

1. Register a Partner Center account and reserve the app name.
2. Associate the app with the Store (fixes the placeholder identity).
3. Add a real `Description`, review `Version`, decide the startup default.
4. Create screenshots and listing images.
5. Build the `x86|x64|arm64` `.msixupload` via the packaging wizard.
6. Fill in the submission (age rating, notes for cert, privacy policy) and submit.
