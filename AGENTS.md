# Project Notes

- Project Settings uses `activeInputHandler: 1` (`Input System Package (New)` only). Do not use legacy `UnityEngine.Input` APIs such as `Input.GetKeyDown`, `Input.GetAxis`, or `KeyCode` in gameplay scripts.
- Use `UnityEngine.InputSystem` instead: `InputActionReference` for XR/controller actions and `Keyboard.current` for editor keyboard fallbacks.
- If an asmdef-scoped script uses Input System types, add the `Unity.InputSystem` assembly reference to that asmdef.
