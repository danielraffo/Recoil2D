using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// unity has two input systems and switching between them kept breaking my project
// so this just checks which one is turned on and uses that. everything else calls this.
public static class InputStuff
{
    public static Vector3 MouseScreenPos()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return Vector3.zero;
        Vector2 p = Mouse.current.position.ReadValue();
        return new Vector3(p.x, p.y, 0f);
#else
        return Input.mousePosition;
#endif
    }

    public static bool FireHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
        return Input.GetMouseButton(0);
#endif
    }

    // these three are just the menu keys
    public static bool SpacePressed() { return KeyWentDown(KeyCode.Space); }
    public static bool MPressed()     { return KeyWentDown(KeyCode.M); }
    public static bool EscPressed()   { return KeyWentDown(KeyCode.Escape); }

    static bool KeyWentDown(KeyCode k)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return false;
        if (k == KeyCode.Space)  return Keyboard.current.spaceKey.wasPressedThisFrame;
        if (k == KeyCode.M)      return Keyboard.current.mKey.wasPressedThisFrame;
        if (k == KeyCode.Escape) return Keyboard.current.escapeKey.wasPressedThisFrame;
        return false;
#else
        return Input.GetKeyDown(k);
#endif
    }
}
