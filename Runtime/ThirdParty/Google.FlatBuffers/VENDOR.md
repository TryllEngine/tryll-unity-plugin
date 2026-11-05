# Vendored: Google.FlatBuffers C# Runtime

**Version:** v24.3.25  
**Source:** https://github.com/google/flatbuffers/tree/v24.3.25/net/FlatBuffers  
**License:** Apache 2.0 (`LICENSE.txt`)

## Files

Copied verbatim from upstream `net/FlatBuffers/` at tag `v24.3.25`:

| File | Notes |
|---|---|
| `ByteBuffer.cs` | Core buffer read/write |
| `ByteBufferUtil.cs` | Size-prefix helpers |
| `FlatBufferBuilder.cs` | Table/vector/string construction |
| `FlatBufferConstants.cs` | Constants + compile-time version guard (`FLATBUFFERS_24_3_25`) |
| `FlatBufferVerify.cs` | Optional buffer verifier |
| `IFlatbufferObject.cs` | Base interface for generated types |
| `Offset.cs` | `Offset<T>`, `StringOffset`, `VectorOffset` |
| `Struct.cs` | Base for generated struct types |
| `Table.cs` | Base for generated table types |
| `LICENSE.txt` | Apache 2.0 from repo root |

## Version lock

This version **must match** the `GIT_TAG` for the `flatbuffers` FetchContent dependency in
`server/CMakeLists.txt`. Check with:

```
grep "GIT_TAG" server/CMakeLists.txt
```

Currently: `GIT_TAG v24.3.25`

The `flatc` compiler and the C# runtime must be the same version because
`FlatBufferConstants.FLATBUFFERS_24_3_25()` is called by generated code as a
compile-time version check. A mismatch causes a deliberate build error.

## Updating

When bumping `GIT_TAG` in `CMakeLists.txt`:

1. Download the new source ZIP from https://github.com/google/flatbuffers/releases
2. Replace the `.cs` files in this folder with `net/FlatBuffers/*.cs` from that ZIP.
3. Replace `LICENSE.txt` with the `LICENSE` file from that ZIP's repo root.
4. Update the **Version** line at the top of this file and the table above.
5. Re-run `cmake --build --preset debug` to regenerate `Runtime/Generated/` with the matching `flatc`.
