# Formatting

This repository uses `.editorconfig` as the single source of truth for formatting.

## What `.editorconfig` enforces here

- UTF-8
- LF line endings
- Final newline
- No trailing whitespace
- C#: tabs (indent size 4)

## How to apply it (practically)

- Make sure your editor reads `.editorconfig`.
- Reformat only files you touched; avoid repo-wide reformat PRs.

## VS Code

- EditorConfig is supported natively.
- Recommended: enable **Format on Save** for C#.
- Avoid overriding indentation in per-language settings (let `.editorconfig` drive it).

## JetBrains Rider

- Enable EditorConfig support: Settings → Editor → Code Style → Enable EditorConfig support.
- You can run Code Cleanup on touched files before committing.

## CI expectations

PRs should not introduce purely formatting-only changes.
If you need to reformat, keep it scoped and mention it in the PR description.
