# Vendored: MackySoft.SerializeReferenceExtensions

**Version:** 1.7.0 (released 2026-01-18)
**Source:** https://github.com/mackysoft/Unity-SerializeReferenceExtensions/tree/1.7.0/Assets/MackySoft/MackySoft.SerializeReferenceExtensions
**License:** MIT (`LICENSE` in this folder)

Provides the `[SubclassSelector]` Inspector drawer for `[SerializeReference]` fields.
Used by `TryllGraphDescription.Params` so workflow-asset authors can pick the concrete
`Tryll*Params` subclass per node.

## Layout

Upstream's `Runtime/` and `Editor/` are split into our existing Runtime/Editor
ThirdParty folders. The code is copied verbatim, including upstream `.meta` files —
preserving GUIDs means the upstream Editor asmdef's `GUID:49b49…` reference to the
Runtime asmdef continues to resolve without rewrites.

Local changes (code and namespaces are unchanged):

- The folder is `MackySoft.SRE`, not `MackySoft.SerializeReferenceExtensions`, and the
  three `TypeSearch/` subfolders are flattened into `TypeSearch/`. Unity installs a Git
  package under `<project>/Library/PackageCache/com.tryll.client@<hash>/`, and the long
  upstream paths crossed Windows' 260-character limit there. The Unity export fails when a
  package path is longer than `[unity.export] max_path_length` (`ci/release.toml`).
- The assemblies are `Tryll.MackySoft.SRE` and `Tryll.MackySoft.SRE.Editor`, with
  `autoReferenced: false`. A project that also installs the upstream package gets no
  duplicate-assembly error, and its own scripts see only the upstream types (the attributes
  are in the global namespace, so an auto-referenced copy would clash with CS0433). Tryll's
  asmdefs reference these by name. With both packages loaded, the managed-reference
  context menu shows its items twice; that is cosmetic.

| Folder | Files | Notes |
|---|---|---|
| `Runtime/ThirdParty/MackySoft.SRE/` | `AddTypeMenuAttribute.cs`, `HideInTypeMenuAttribute.cs`, `SubclassSelectorAttribute.cs`, `Tryll.MackySoft.SRE.asmdef`, `LICENSE`, `VENDOR.md` | Runtime-eligible attributes |
| `Editor/ThirdParty/MackySoft.SRE/` | `SubclassSelectorDrawer.cs`, `AdvancedTypePopup.cs`, `ManagedReferenceContextualPropertyMenu.cs`, `ManagedReferenceUtility.cs`, `PropertyDrawerCache.cs`, `SerializedPropertyExtensions.cs`, `TypeMenuUtility.cs`, `Tryll.MackySoft.SRE.Editor.asmdef`, `TypeSearch/*` (10 files, flattened from upstream `TypeSearch/` and its three subfolders) | Editor-only drawer + advanced type popup |

`Tryll.Client.asmdef` references `Tryll.MackySoft.SRE`; `Tryll.Client.Editor.asmdef` references `Tryll.MackySoft.SRE.Editor`.

## Why bundle, not declare as a UPM dependency?

This is the temporary solution. The longer-term plan is to declare it as a UPM dependency
at the publish stage. The tradeoff is documented in
`docs/research/clients/unity-plugin-external-deps-bundle-vs-package.md`.

## Updating

When bumping to a new upstream tag:

1. Pick the new tag at https://github.com/mackysoft/Unity-SerializeReferenceExtensions/tags
2. Download `Assets/MackySoft/MackySoft.SerializeReferenceExtensions/Runtime/**` and `Editor/**` (the same file set listed in the table above, including `.meta` files).
3. Replace the contents of `Runtime/ThirdParty/MackySoft.SRE/` (excluding `VENDOR.md`) and `Editor/ThirdParty/MackySoft.SRE/`. Then redo the local changes above: flatten `TypeSearch/`, and rename both asmdef files and their `name` fields (keep the asmdef `.meta` GUIDs).
4. Replace `LICENSE` with the repo-root `LICENSE` from that tag.
5. Update the **Version** line at the top of this file.
6. Re-open the Unity project to let it re-import.
