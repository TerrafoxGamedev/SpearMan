---
name: filesystem-reimport
description: |-
  Re-scan the Godot project's res:// filesystem and/or refresh specific files, then wait for the import to settle before returning. The Godot analog of Unity's AssetDatabase.Refresh. Two modes:
    - Pass 'files' (a list of res:// paths) to refresh exactly those files — use this after editing a file's bytes outside the editor, or after adding a new file.
    - Omit 'files' (or pass an empty list) to trigger a full EditorFileSystem.Scan — use this after adding/removing many files on disk so Godot picks up the change.
  With 'files', each file is routed by type: an already-imported asset (it has a '.import' sidecar) is reimported via EditorFileSystem.ReimportFiles; a NEW file with an importer extension (e.g. a .png that has never been imported) is first registered with the editor filesystem and then imported; Godot's NATIVE formats (.tscn/.tres/.gd/.cs/.gdshader) have no importer and are refreshed with EditorFileSystem.UpdateFile instead — queueing them for import would make the editor log "importer for type '' not…
metadata:
  generated-by: mcp-plugin-dotnet
---

# FileSystem / Reimport

Re-scan the Godot project's res:// filesystem and/or refresh specific files, then wait for the import to settle before returning. The Godot analog of Unity's AssetDatabase.Refresh. Two modes:
  - Pass 'files' (a list of res:// paths) to refresh exactly those files — use this after editing a file's bytes outside the editor, or after adding a new file.
  - Omit 'files' (or pass an empty list) to trigger a full EditorFileSystem.Scan — use this after adding/removing many files on disk so Godot picks up the change.
With 'files', each file is routed by type: an already-imported asset (it has a '.import' sidecar) is reimported via EditorFileSystem.ReimportFiles; a NEW file with an importer extension (e.g. a .png that has never been imported) is first registered with the editor filesystem and then imported; Godot's NATIVE formats (.tscn/.tres/.gd/.cs/.gdshader) have no importer and are refreshed with EditorFileSystem.UpdateFile instead — queueing them for import would make the editor log "importer for type '' not found".
Every import is VERIFIED afterwards (a valid '.import' sidecar must exist). The returned status lists each file that was NOT imported, with the reason, before anything else — never treat a result that starts with 'NOT imported' as success.
The call blocks until the work completes (bounded), so a subsequent resource-find/get-data sees the settled state. Returns a short status string.

## How to Call

### HTTP API (Direct Tool Execution)

Execute this tool directly via the MCP Plugin HTTP API:

```bash
curl -X POST http://127.0.0.1:8791/api/tools/filesystem-reimport \
  -H "Content-Type: application/json" \
  -d '{
  "files": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use `-d @args.json`.
>
> Or pipe via stdin:
> ```bash
> curl -X POST http://127.0.0.1:8791/api/tools/filesystem-reimport -H "Content-Type: application/json" -d @- <<'EOF'
> {"param": "value"}
> EOF
> ```

#### With Authorization (if required)

```bash
curl -X POST http://127.0.0.1:8791/api/tools/filesystem-reimport \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
  "files": "string_value"
}'
```

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `files` | `any` | No | Optional list of res:// file paths to refresh. An already-imported asset (it has a '.import' sidecar) is reimported; a new file with an importer extension (e.g. a fresh .png) is registered and imported for the first time; Godot's native formats (.tscn/.tres/.gd/.cs/...) have no importer and are refreshed via EditorFileSystem.UpdateFile. When omitted/empty, a full filesystem scan is run instead. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "files": {
      "$ref": "#/$defs/System.Collections.Generic.List(System.String)",
      "description": "Optional list of res:// file paths to refresh. An already-imported asset (it has a '.import' sidecar) is reimported; a new file with an importer extension (e.g. a fresh .png) is registered and imported for the first time; Godot's native formats (.tscn/.tres/.gd/.cs/...) have no importer and are refreshed via EditorFileSystem.UpdateFile. When omitted/empty, a full filesystem scan is run instead."
    }
  },
  "$defs": {
    "System.Collections.Generic.List(System.String)": {
      "type": "array",
      "items": {
        "type": "string"
      }
    }
  }
}
```

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "type": "string"
    }
  },
  "required": [
    "result"
  ]
}
```

