# C# Programming Guidelines

Please adhere to the following C# programming guidelines when writing or modifying code in this workspace:

- **No Comments**: Do not write any comments in the code. The code should be completely self-documenting through clear naming conventions and structure. Remove comments if modifying existing blocks.
- **Naming Conventions**: 
  - Use `PascalCase` for class names, method names, and public properties.
  - Use `camelCase` for local variables and method parameters.
  - Use `_camelCase` for private fields.
- **Braces**: Always use braces `{}` for `if`, `for`, `foreach`, and `while` statements, even if they contain only a single line of code. Place opening braces on a new line (Allman style).
- **Access Modifiers**: Always explicitly specify access modifiers (e.g., `private`, `public`, `protected`) instead of relying on defaults.
- **Var Keyword**: Use `var` for local variables when the type is obvious from the right side of the assignment.
- **String Interpolation**: Prefer string interpolation (`$"{variable}"`) over `string.Format` or string concatenation.
- **File Structure**: Keep one class per file unless they are small, closely related private nested classes.
- **No Public Variables**: Do not use public variables. Prefer private constants over public properties when applicable.
- **Serialized Fields**: Do not delete existing `[SerializeField]` attributes. You are allowed to create new serialized fields of your own when needed.
- **No Tooltips**: Do not use `[Tooltip]` attributes.
- **DOTween**: DOTween is available in the project and can be used for tweens, animations, and transitions.
- **Tale**: Tale is available in the project and can be used for story sequences, dialogs, transitions (e.g., Fade), and cinematic actions.
- **No Defensive Programming**: Do not include defensive programming checks (e.g., null checks, directory exists checks, security path checks). Assume the happy path.
- **No Debug Logs**: Do not write `Debug.Log`, `Debug.LogWarning`, or `Debug.LogError` statements in the code. Only keep the debug log that prints the remote controller listener port.
- **No Project Building**: Do not run build commands (e.g., `dotnet build` or MSBuild). Compiling and building the project is Unity's job.
- **No Git Commands**: Do not run any git commands (e.g., `git status`, `git diff`, `git log`). Never check git or use git to inspect history or verify changes.
- **No Prefab or Scene Inspection**: Do not check, inspect, view, or grep prefab (`.prefab`) and scene (`.unity`) assets when asked to implement code.

# Web Development Guidelines
- **Mobile-Friendly Landscape**: When working with HTML pages, the top priority is making the page mobile-friendly, and the UI layout should always be designed for landscape orientation.
- **Separation of Concerns**: Always follow proper web development guidelines by splitting CSS and JS into their own separate files rather than embedding them directly in HTML.
- **Scriptable Objects**: Scriptable Objects should be created and assigned via the inspector. Do not use Resources.Load for them.
- **UI Parity**: Any changes made to the debug battle menu UI must replicate to the web UI and vice versa.

