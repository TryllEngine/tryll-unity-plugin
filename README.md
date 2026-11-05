# Tryll for Unity

Tryll runs language, speech-to-text and text-to-speech models on the player's own machine, and
connects them to your game through workflow-driven agents. This repository is the Unity package
`com.tryll.client` 0.10.0.

- **Unity:** 6000.3 or newer
- **Platform:** Windows x64 (the Tryll server is Windows-only today)
- **Docs:** https://docs.tryllengine.com/v0.10/

## Install

1. Install [Git](https://git-scm.com/downloads) and make sure it is on your PATH.
2. In Unity, open **Window > Package Manager**.
3. Click **+** > **Install package from git URL…**.
4. Paste this URL and click **Install**:

   ```
   https://github.com/TryllEngine/tryll-unity-plugin.git#v0.10.0
   ```

5. The **Welcome to Tryll** window opens. Accept the licence and click **Download server**. The
   server is downloaded once per version and shared by every project on the computer
   (`%LOCALAPPDATA%\Tryll\server\`). The editor starts the server only after you accept the
   licence.

No internet access during install? Download the server zip from the release page of this repository
and use **Install from zip…** in the same window.

## Update

Change the version at the end of the URL, for example `#v0.10.0` to the newer tag:
**Window > Package Manager > + > Install package from git URL…** with the new URL, or edit
`Packages/manifest.json`. The Welcome window then downloads the matching server.

Always update the package and the server together. The plugin and the server must be the same
version; a mismatch is refused with error 5004 (`ProtocolVersionMismatch`).

## Data collection

Telemetry is part of the Tryll Runtime Licence and is always on. This package sends one anonymous
event per server install, and the Tryll server sends its own telemetry. **Telemetry** in the
Welcome window opens the page that describes both: https://docs.tryllengine.com/v0.10/reference/telemetry.html.

## Help and community

**Window > Tryll > Help & Community** has links to the documentation and the community.
Found a bug? See https://docs.tryllengine.com/v0.10/how-to/report-a-bug.html, or open an issue at https://github.com/TryllEngine/tryll-unity-plugin/issues.

## How this repository is made

This repository is generated from Tryll's main repository by the release pipeline. Every release is
one commit and one tag. Changes made here directly are overwritten, so please report issues instead
of opening pull requests.

## Licence

See [LICENSE.md](LICENSE.md) and [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
