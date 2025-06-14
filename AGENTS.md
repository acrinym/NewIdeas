# Repository Guidelines for Agents

This repository hosts the **Cycloside** project along with related documentation.
When making changes, please follow these rules:

## Coding Style
- Use 4 spaces for C# indentation.
- Keep C# files under `Cycloside/` organized by existing structure.

## Commit Messages
- Summarize the purpose of the change clearly.
- Mention related issues or features when relevant (e.g. `Fixes #42`).

## Programmatic Checks
- After modifying C# code, run `dotnet build Cycloside/Cycloside.csproj` to ensure the project compiles.
- If the build fails, include the failure output in your PR notes.

## Other Notes
- Avoid editing large binary archives or zipped sources.
- Documentation lives in the `docs/` folder. Keep Markdown simple and readable.

