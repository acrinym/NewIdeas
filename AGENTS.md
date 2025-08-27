# Repository Guidelines for Agents

This repository hosts the **Cycloside** project along with related documentation.  
When making changes, follow these binding rules:

---

## 🔒 Absolute Directives
- **NO PLACEHOLDERS EVER.**  
  - Do not generate stubs, dummy methods, TODO comments, empty XAML tags, or incomplete scaffolding. 
  - The TODO file(s) and documentation are the **ONLY** exception.  
  - Every output must be **fully runnable** and compile cleanly.  
  - **Exception:** a placeholder is allowed **only if Justin explicitly authorizes it**.  
    - The agent must *ask permission first* before inserting any placeholder.  

- **Output Format**  
  - No filler text — output code first.  
  - Explanations must appear as **inline comments inside the code**, never as prose outside.  

- **Completeness**  
   - If you cannot guarantee correctness, refine until you can — never emit broken code.  

---

## 🖼 Avalonia / SkiaSharp Specific Rules
- **XAML/CS Must Be Complete**  
  - Do not emit `<Control />` or unimplemented event hooks.  
  - All event handlers referenced in XAML must exist and be wired in C#.  
  - Bindings must point to real view models or properties — never dummies.  

- **UI Components**  
  - Always generate **working Avalonia UI components** with SkiaSharp rendering fully functional.  
  - Use established Avalonia idioms (e.g., `ReactiveUI`, `DataContext`) — never leave “to be implemented.”  
  - If UI requires graphics, provide working SkiaSharp draw calls with defaults that render visibly.  

- **Minimalism Over Gaps**  
  - If uncertain, generate a simple but runnable implementation and then ask about how to continue.  
  - Never leave unfinished UI fragments.  

---

## 🛠 Environment Setup
- Ensure the **.NET SDK 8** (`dotnet-sdk-8.0`) is installed before running builds or linters.  
- Verify with `dotnet --version` before proceeding.  

---

## 📐 Coding Style
- Use **4 spaces** for indentation in all C#.  
- Keep C# files under `Cycloside/` organized by the existing folder structure.  
- Follow Avalonia conventions when working with `Avalonia-master/`.  

---

## 📦 Programmatic Checks
- After modifying potential app-breaking C# code, run:  
  ```sh
  dotnet build Cycloside/Cycloside.csproj to check for build issues. 
