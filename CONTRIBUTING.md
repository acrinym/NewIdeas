# Contributing to Cycloside

Thank you for your interest in contributing to Cycloside! This project is **community-owned**, and your contributions matter.

---

## Ways to Contribute

### 1. Code
- Fix bugs (check GitHub issues)
- Implement features (check roadmap in `docs/plans/`)
- Improve performance
- Write tests

### 2. Themes & Plugins
- Create themes (see `docs/examples/theme-example.md`)
- Build plugins (see `Cycloside/SDK/README.md`)
- Share your creations on the marketplace

### 3. Documentation
- Fix typos, clarify instructions
- Write tutorials
- Translate docs to other languages
- Add code examples

### 4. Community
- Answer questions in issues/forums
- Report bugs with clear reproduction steps
- Test new releases and provide feedback
- Share your Cycloside setup (screenshots!)

### 5. Design
- Create icons, logos, graphics
- Design UI mockups
- Propose UX improvements
- Create promotional materials

---

## Getting Started

### Prerequisites
- .NET 8 SDK (`dotnet --version`)
- Git
- Code editor (VS Code, Rider, Visual Studio)

### Setup
```bash
git clone https://github.com/acrinym/NewIdeas.git
cd NewIdeas
dotnet build Cycloside/Cycloside.csproj
dotnet run --project Cycloside/Cycloside.csproj
```

### Project Structure
- `Cycloside/` - Main application code
- `Cycloside/Plugins/BuiltIn/` - Built-in plugins
- `Cycloside/Services/` - Core services (themes, plugins, effects)
- `Cycloside/Campfires/` - Design documents
- `docs/` - User-facing documentation
- `docs/plans/` - Development phase plans

---

## Code Contribution Process

### 1. Find or Create an Issue
- Check existing issues: https://github.com/acrinym/NewIdeas/issues
- If no issue exists, create one describing the bug/feature
- Wait for maintainer feedback before starting large changes

### 2. Fork and Branch
```bash
git clone https://github.com/YOUR-USERNAME/NewIdeas.git
cd NewIdeas
git checkout -b feature/your-feature-name
# or: git checkout -b fix/bug-description
```

### 3. Make Changes
- Follow existing code style (see below)
- Add tests if applicable
- Update docs if behavior changes
- Run linter: `dotnet format Cycloside/Cycloside.csproj`

### 4. Commit
```bash
git add .
git commit -m "feat: add feature description"
# or: git commit -m "fix: bug description"
```

**Commit message format:**
- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation only
- `refactor:` - Code refactor (no behavior change)
- `test:` - Add or fix tests
- `chore:` - Build, deps, tooling

### 5. Push and PR
```bash
git push origin feature/your-feature-name
```
Then open a pull request on GitHub with:
- **Title:** Clear summary (e.g., "Add gamepad navigation to Theater Mode")
- **Description:** What changed, why, how to test
- **Link to issue:** "Closes #123"

---

## Code Style

### C# Guidelines
- **Indentation:** 4 spaces (no tabs)
- **Naming:**
  - Classes/Methods: `PascalCase`
  - Variables/fields: `camelCase`
  - Private fields: `_camelCase` (underscore prefix)
  - Constants: `PascalCase`
- **Comments:** Explain *why*, not *what*
- **No placeholders:** Every method must be fully implemented (see `AGENTS.md`)

### Example
```csharp
public class ThemeManager
{
    private readonly ILogger _logger;
    private const int MaxThemeSize = 10 * 1024 * 1024; // 10 MB

    public async Task<Theme> LoadThemeAsync(string path)
    {
        // Validate theme file size before loading to prevent DoS
        var fileInfo = new FileInfo(path);
        if (fileInfo.Length > MaxThemeSize)
        {
            throw new InvalidOperationException("Theme file too large");
        }

        // Load and parse theme...
    }
}
```

### XAML/AXAML Guidelines
- Use meaningful names for controls (`ThemeListBox`, not `listBox1`)
- Follow Avalonia conventions
- Use data binding where possible

---

## Testing

### Run Tests
```bash
dotnet test
```

### Write Tests
- Unit tests for services/utilities
- Integration tests for plugin loading
- UI tests for critical workflows

**Test naming:** `MethodName_Scenario_ExpectedResult`

```csharp
[Fact]
public void LoadTheme_WithInvalidPath_ThrowsFileNotFoundException()
{
    // Arrange
    var manager = new ThemeManager();

    // Act & Assert
    Assert.Throws<FileNotFoundException>(() => manager.LoadTheme("invalid.axaml"));
}
```

---

## Pull Request Review

### What We Look For
- **Functionality:** Does it work as intended?
- **Code quality:** Is it readable, maintainable?
- **Tests:** Are there tests (when applicable)?
- **Docs:** Are docs updated (when applicable)?
- **No regressions:** Does it break existing features?

### Review Timeline
- Small PRs (< 100 lines): 2-3 days
- Medium PRs (100-500 lines): 5-7 days
- Large PRs (> 500 lines): 1-2 weeks

**Tip:** Smaller PRs get reviewed faster!

### If Your PR is Rejected
- Don't take it personally
- Read feedback carefully
- Ask clarifying questions
- Revise and resubmit

---

## Community Guidelines

### Code of Conduct
We follow the [Contributor Covenant v2.1](https://www.contributor-covenant.org/).

**TL;DR:**
- Be respectful
- Be inclusive
- Be constructive
- No harassment

**Violations:** Report to maintainers (see `GOVERNANCE.md`)

### Communication
- **GitHub Issues:** Bug reports, feature requests
- **Pull Requests:** Code review, technical discussion
- **Discussions:** General questions, brainstorming
- **Discord/Matrix:** [To be added] - Real-time chat

---

## Recognition

### Contributors
All contributors are listed in `CONTRIBUTORS.md` and appear in:
- Git history (`git log`)
- Release notes
- Project website (future)

### Maintainers
Active contributors may be invited to become maintainers:
- Commit access to repos
- Review and merge PRs
- Help shape roadmap

**How to become a maintainer:**
1. Contribute consistently (10+ merged PRs)
2. Demonstrate good judgment
3. Ask a current maintainer

---

## Legal

### License
By contributing, you agree:
- Your code is licensed under GPL v3
- You have the right to contribute (you own it or have permission)
- You grant a patent license for your contributions

**No separate CLA required.** Your PR is your agreement.

### Copyright
- Cycloside core: Copyright (C) 2024-2026 Justin and Contributors
- Your plugins/themes: You retain copyright (license them however you want)

---

## Questions?

- **"I'm new to open source, where do I start?"** Look for issues tagged `good first issue`
- **"Can I work on feature X?"** Yes! Create an issue first to discuss approach
- **"How long until my PR is reviewed?"** See "Review Timeline" above
- **"Can I contribute themes/plugins?"** Yes! No approval required, publish to your own feed
- **"I disagree with a design decision, what do I do?"** See `GOVERNANCE.md` conflict resolution

---

## Resources

- **Campfires:** Read `Cycloside/Campfires/` for project vision and philosophy
- **Phase Plans:** Read `docs/plans/` for roadmap and feature plans
- **Examples:** See `docs/examples/` for theme and plugin examples
- **Avalonia Docs:** https://docs.avaloniaui.net/
- **SkiaSharp Docs:** https://learn.microsoft.com/en-us/dotnet/api/skiasharp

---

**Thank you for contributing to Cycloside!**

**Together, we're resurrecting what corporations killed.**
