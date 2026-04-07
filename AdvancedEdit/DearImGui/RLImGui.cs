// RlImGui.cs
// C# conversion of rlImGui (https://github.com/raylib-extras/rlImGui)
// Original: Copyright (c) 2024 Jeffery Myers (ZLIB License)
// Converted for use with Raylib-cs and Hexa.NET.ImGui

using System.Numerics;
using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework.DearImGui;

public static unsafe class RlImGui
{
    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private static ImGuiMouseCursor _currentMouseCursor = ImGuiMouseCursor.Count;
    private static readonly MouseCursor[] MouseCursorMap = new MouseCursor[(int)ImGuiMouseCursor.Count];

    private static ImGuiContextPtr _globalContext;

    private const int MaxRaylibKey = 349;
    private static readonly ImGuiKey[] RaylibKeyMap = new ImGuiKey[MaxRaylibKey];
    private static bool _keyMapInitialized;

    private static bool _lastFrameFocused;
    private static bool _lastControlPressed;
    private static bool _lastShiftPressed;
    private static bool _lastAltPressed;
    private static bool _lastSuperPressed;

    // -------------------------------------------------------------------------
    // Modifier helpers
    // -------------------------------------------------------------------------

    private static bool IsControlDown() =>
        Raylib.IsKeyDown(KeyboardKey.RightControl) || Raylib.IsKeyDown(KeyboardKey.LeftControl);

    private static bool IsShiftDown() =>
        Raylib.IsKeyDown(KeyboardKey.RightShift) || Raylib.IsKeyDown(KeyboardKey.LeftShift);

    private static bool IsAltDown() =>
        Raylib.IsKeyDown(KeyboardKey.RightAlt) || Raylib.IsKeyDown(KeyboardKey.LeftAlt);

    private static bool IsSuperDown() =>
        Raylib.IsKeyDown(KeyboardKey.RightSuper) || Raylib.IsKeyDown(KeyboardKey.LeftSuper);

    // -------------------------------------------------------------------------
    // Clipboard callbacks
    // -------------------------------------------------------------------------

    private delegate sbyte* GetClipTextCallback(IntPtr userData);

    private delegate void SetClipTextCallback(IntPtr userData, sbyte* text);

    private static GetClipTextCallback _getClipCallback = null!;
    private static SetClipTextCallback _setClipCallback = null!;

    // -------------------------------------------------------------------------
    // Keymap setup
    // -------------------------------------------------------------------------

    private static void SetupKeymap()
    {
        if (_keyMapInitialized) return;
        _keyMapInitialized = true;

        Array.Clear(RaylibKeyMap, 0, MaxRaylibKey);

        RaylibKeyMap[(int)KeyboardKey.Apostrophe] = ImGuiKey.Apostrophe;
        RaylibKeyMap[(int)KeyboardKey.Comma] = ImGuiKey.Comma;
        RaylibKeyMap[(int)KeyboardKey.Minus] = ImGuiKey.Minus;
        RaylibKeyMap[(int)KeyboardKey.Period] = ImGuiKey.Period;
        RaylibKeyMap[(int)KeyboardKey.Slash] = ImGuiKey.Slash;
        RaylibKeyMap[(int)KeyboardKey.Zero] = ImGuiKey.Key0;
        RaylibKeyMap[(int)KeyboardKey.One] = ImGuiKey.Key1;
        RaylibKeyMap[(int)KeyboardKey.Two] = ImGuiKey.Key2;
        RaylibKeyMap[(int)KeyboardKey.Three] = ImGuiKey.Key3;
        RaylibKeyMap[(int)KeyboardKey.Four] = ImGuiKey.Key4;
        RaylibKeyMap[(int)KeyboardKey.Five] = ImGuiKey.Key5;
        RaylibKeyMap[(int)KeyboardKey.Six] = ImGuiKey.Key6;
        RaylibKeyMap[(int)KeyboardKey.Seven] = ImGuiKey.Key7;
        RaylibKeyMap[(int)KeyboardKey.Eight] = ImGuiKey.Key8;
        RaylibKeyMap[(int)KeyboardKey.Nine] = ImGuiKey.Key9;
        RaylibKeyMap[(int)KeyboardKey.Semicolon] = ImGuiKey.Semicolon;
        RaylibKeyMap[(int)KeyboardKey.Equal] = ImGuiKey.Equal;
        RaylibKeyMap[(int)KeyboardKey.A] = ImGuiKey.A;
        RaylibKeyMap[(int)KeyboardKey.B] = ImGuiKey.B;
        RaylibKeyMap[(int)KeyboardKey.C] = ImGuiKey.C;
        RaylibKeyMap[(int)KeyboardKey.D] = ImGuiKey.D;
        RaylibKeyMap[(int)KeyboardKey.E] = ImGuiKey.E;
        RaylibKeyMap[(int)KeyboardKey.F] = ImGuiKey.F;
        RaylibKeyMap[(int)KeyboardKey.G] = ImGuiKey.G;
        RaylibKeyMap[(int)KeyboardKey.H] = ImGuiKey.H;
        RaylibKeyMap[(int)KeyboardKey.I] = ImGuiKey.I;
        RaylibKeyMap[(int)KeyboardKey.J] = ImGuiKey.J;
        RaylibKeyMap[(int)KeyboardKey.K] = ImGuiKey.K;
        RaylibKeyMap[(int)KeyboardKey.L] = ImGuiKey.L;
        RaylibKeyMap[(int)KeyboardKey.M] = ImGuiKey.M;
        RaylibKeyMap[(int)KeyboardKey.N] = ImGuiKey.N;
        RaylibKeyMap[(int)KeyboardKey.O] = ImGuiKey.O;
        RaylibKeyMap[(int)KeyboardKey.P] = ImGuiKey.P;
        RaylibKeyMap[(int)KeyboardKey.Q] = ImGuiKey.Q;
        RaylibKeyMap[(int)KeyboardKey.R] = ImGuiKey.R;
        RaylibKeyMap[(int)KeyboardKey.S] = ImGuiKey.S;
        RaylibKeyMap[(int)KeyboardKey.T] = ImGuiKey.T;
        RaylibKeyMap[(int)KeyboardKey.U] = ImGuiKey.U;
        RaylibKeyMap[(int)KeyboardKey.V] = ImGuiKey.V;
        RaylibKeyMap[(int)KeyboardKey.W] = ImGuiKey.W;
        RaylibKeyMap[(int)KeyboardKey.X] = ImGuiKey.X;
        RaylibKeyMap[(int)KeyboardKey.Y] = ImGuiKey.Y;
        RaylibKeyMap[(int)KeyboardKey.Z] = ImGuiKey.Z;
        RaylibKeyMap[(int)KeyboardKey.Space] = ImGuiKey.Space;
        RaylibKeyMap[(int)KeyboardKey.Escape] = ImGuiKey.Escape;
        RaylibKeyMap[(int)KeyboardKey.Enter] = ImGuiKey.Enter;
        RaylibKeyMap[(int)KeyboardKey.Tab] = ImGuiKey.Tab;
        RaylibKeyMap[(int)KeyboardKey.Backspace] = ImGuiKey.Backspace;
        RaylibKeyMap[(int)KeyboardKey.Insert] = ImGuiKey.Insert;
        RaylibKeyMap[(int)KeyboardKey.Delete] = ImGuiKey.Delete;
        RaylibKeyMap[(int)KeyboardKey.Right] = ImGuiKey.RightArrow;
        RaylibKeyMap[(int)KeyboardKey.Left] = ImGuiKey.LeftArrow;
        RaylibKeyMap[(int)KeyboardKey.Down] = ImGuiKey.DownArrow;
        RaylibKeyMap[(int)KeyboardKey.Up] = ImGuiKey.UpArrow;
        RaylibKeyMap[(int)KeyboardKey.PageUp] = ImGuiKey.PageUp;
        RaylibKeyMap[(int)KeyboardKey.PageDown] = ImGuiKey.PageDown;
        RaylibKeyMap[(int)KeyboardKey.Home] = ImGuiKey.Home;
        RaylibKeyMap[(int)KeyboardKey.End] = ImGuiKey.End;
        RaylibKeyMap[(int)KeyboardKey.CapsLock] = ImGuiKey.CapsLock;
        RaylibKeyMap[(int)KeyboardKey.ScrollLock] = ImGuiKey.ScrollLock;
        RaylibKeyMap[(int)KeyboardKey.NumLock] = ImGuiKey.NumLock;
        RaylibKeyMap[(int)KeyboardKey.PrintScreen] = ImGuiKey.PrintScreen;
        RaylibKeyMap[(int)KeyboardKey.Pause] = ImGuiKey.Pause;
        RaylibKeyMap[(int)KeyboardKey.F1] = ImGuiKey.F1;
        RaylibKeyMap[(int)KeyboardKey.F2] = ImGuiKey.F2;
        RaylibKeyMap[(int)KeyboardKey.F3] = ImGuiKey.F3;
        RaylibKeyMap[(int)KeyboardKey.F4] = ImGuiKey.F4;
        RaylibKeyMap[(int)KeyboardKey.F5] = ImGuiKey.F5;
        RaylibKeyMap[(int)KeyboardKey.F6] = ImGuiKey.F6;
        RaylibKeyMap[(int)KeyboardKey.F7] = ImGuiKey.F7;
        RaylibKeyMap[(int)KeyboardKey.F8] = ImGuiKey.F8;
        RaylibKeyMap[(int)KeyboardKey.F9] = ImGuiKey.F9;
        RaylibKeyMap[(int)KeyboardKey.F10] = ImGuiKey.F10;
        RaylibKeyMap[(int)KeyboardKey.F11] = ImGuiKey.F11;
        RaylibKeyMap[(int)KeyboardKey.F12] = ImGuiKey.F12;
        RaylibKeyMap[(int)KeyboardKey.LeftShift] = ImGuiKey.LeftShift;
        RaylibKeyMap[(int)KeyboardKey.LeftControl] = ImGuiKey.LeftCtrl;
        RaylibKeyMap[(int)KeyboardKey.LeftAlt] = ImGuiKey.LeftAlt;
        RaylibKeyMap[(int)KeyboardKey.LeftSuper] = ImGuiKey.LeftSuper;
        RaylibKeyMap[(int)KeyboardKey.RightShift] = ImGuiKey.RightShift;
        RaylibKeyMap[(int)KeyboardKey.RightControl] = ImGuiKey.RightCtrl;
        RaylibKeyMap[(int)KeyboardKey.RightAlt] = ImGuiKey.RightAlt;
        RaylibKeyMap[(int)KeyboardKey.RightSuper] = ImGuiKey.RightSuper;
        RaylibKeyMap[(int)KeyboardKey.KeyboardMenu] = ImGuiKey.Menu;
        RaylibKeyMap[(int)KeyboardKey.LeftBracket] = ImGuiKey.LeftBracket;
        RaylibKeyMap[(int)KeyboardKey.Backslash] = ImGuiKey.Backslash;
        RaylibKeyMap[(int)KeyboardKey.RightBracket] = ImGuiKey.RightBracket;
        RaylibKeyMap[(int)KeyboardKey.Grave] = ImGuiKey.GraveAccent;
        RaylibKeyMap[(int)KeyboardKey.Kp0] = ImGuiKey.Keypad0;
        RaylibKeyMap[(int)KeyboardKey.Kp1] = ImGuiKey.Keypad1;
        RaylibKeyMap[(int)KeyboardKey.Kp2] = ImGuiKey.Keypad2;
        RaylibKeyMap[(int)KeyboardKey.Kp3] = ImGuiKey.Keypad3;
        RaylibKeyMap[(int)KeyboardKey.Kp4] = ImGuiKey.Keypad4;
        RaylibKeyMap[(int)KeyboardKey.Kp5] = ImGuiKey.Keypad5;
        RaylibKeyMap[(int)KeyboardKey.Kp6] = ImGuiKey.Keypad6;
        RaylibKeyMap[(int)KeyboardKey.Kp7] = ImGuiKey.Keypad7;
        RaylibKeyMap[(int)KeyboardKey.Kp8] = ImGuiKey.Keypad8;
        RaylibKeyMap[(int)KeyboardKey.Kp9] = ImGuiKey.Keypad9;
        RaylibKeyMap[(int)KeyboardKey.KpDecimal] = ImGuiKey.KeypadDecimal;
        RaylibKeyMap[(int)KeyboardKey.KpDivide] = ImGuiKey.KeypadDivide;
        RaylibKeyMap[(int)KeyboardKey.KpMultiply] = ImGuiKey.KeypadMultiply;
        RaylibKeyMap[(int)KeyboardKey.KpSubtract] = ImGuiKey.KeypadSubtract;
        RaylibKeyMap[(int)KeyboardKey.KpAdd] = ImGuiKey.KeypadAdd;
        RaylibKeyMap[(int)KeyboardKey.KpEnter] = ImGuiKey.KeypadEnter;
        RaylibKeyMap[(int)KeyboardKey.KpEqual] = ImGuiKey.KeypadEqual;
    }

    // -------------------------------------------------------------------------
    // Mouse cursor setup
    // -------------------------------------------------------------------------

    private static void SetupMouseCursors()
    {
        MouseCursorMap[(int)ImGuiMouseCursor.Arrow] = MouseCursor.Arrow;
        MouseCursorMap[(int)ImGuiMouseCursor.TextInput] = MouseCursor.IBeam;
        MouseCursorMap[(int)ImGuiMouseCursor.Hand] = MouseCursor.PointingHand;
        MouseCursorMap[(int)ImGuiMouseCursor.ResizeAll] = MouseCursor.ResizeAll;
        MouseCursorMap[(int)ImGuiMouseCursor.ResizeEw] = MouseCursor.ResizeEw;
        MouseCursorMap[(int)ImGuiMouseCursor.ResizeNesw] = MouseCursor.ResizeNesw;
        MouseCursorMap[(int)ImGuiMouseCursor.ResizeNs] = MouseCursor.ResizeNs;
        MouseCursorMap[(int)ImGuiMouseCursor.ResizeNwse] = MouseCursor.ResizeNwse;
        MouseCursorMap[(int)ImGuiMouseCursor.NotAllowed] = MouseCursor.NotAllowed;
    }

    // -------------------------------------------------------------------------
    // Backend setup
    // -------------------------------------------------------------------------

    private static sbyte* GetClipboardText(IntPtr userData)
    {
        return Raylib.GetClipboardText();
    }

    private static void SetClipboardText(IntPtr userData, sbyte* text)
    {
        Raylib.SetClipboardText(text);
    }

    private static void SetupBackend()
    {
        var io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.HasGamepad
                           | ImGuiBackendFlags.HasSetMousePos
                           | ImGuiBackendFlags.RendererHasTextures;
        io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;

        io.MousePos = new Vector2(0, 0);

        var platformIO = ImGui.GetPlatformIO();

        _setClipCallback = SetClipboardText;
        platformIO.PlatformSetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(_setClipCallback).ToPointer();

        _getClipCallback = GetClipboardText;
        platformIO.PlatformGetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(_getClipCallback).ToPointer();
    }

    // -------------------------------------------------------------------------
    // Globals / context initialisation
    // -------------------------------------------------------------------------

    private static void SetupGlobals()
    {
        _lastFrameFocused = Raylib.IsWindowFocused();
        _lastControlPressed = false;
        _lastShiftPressed = false;
        _lastAltPressed = false;
        _lastSuperPressed = false;
    }

    // -------------------------------------------------------------------------
    // Public setup API
    // -------------------------------------------------------------------------

    /// <summary>Begin ImGui context initialization. Call before adding custom fonts.</summary>
    public static void BeginInitImGui()
    {
        SetupGlobals();

        if (_globalContext.IsNull)
            _globalContext = ImGui.CreateContext();

        SetupKeymap();

        var io = ImGui.GetIO();

        // Default font
        var defaultConfig = new ImFontConfig
        {
            FontDataOwnedByAtlas = 1,
            SizePixels = 13.0f,
            PixelSnapH = 1,
            GlyphMaxAdvanceX = float.MaxValue,
            RasterizerMultiply = 1,
            RasterizerDensity = 1,
        };

        io.Fonts.AddFontDefault(&defaultConfig);
    }

    /// <summary>Finish ImGui context initialization after fonts have been configured.</summary>
    public static void EndInitImGui()
    {
        ImGui.SetCurrentContext(_globalContext);
        SetupMouseCursors();
        SetupBackend();
    }

    /// <summary>Convenience one-call setup.</summary>
    public static void Setup()
    {
        BeginInitImGui();

        ImHelper.LoadAltLight();

        EndInitImGui();
    }

    // -------------------------------------------------------------------------
    // Per-frame API
    // -------------------------------------------------------------------------

    /// <summary>Begin a new ImGui frame using the current raylib frame time.</summary>
    public static void Begin()
    {
        ImGui.SetCurrentContext(_globalContext);
        BeginDelta(Raylib.GetFrameTime());
    }

    /// <summary>Begin a new ImGui frame with an explicit delta time.</summary>
    public static void BeginDelta(float deltaTime)
    {
        ImGui.SetCurrentContext(_globalContext);
        NewFrame(deltaTime);
        ProcessEvents();
        ImGui.NewFrame();
    }

    /// <summary>Render the ImGui frame. Call between raylib BeginDrawing/EndDrawing.</summary>
    public static void End()
    {
        ImGui.SetCurrentContext(_globalContext);
        ImGui.Render();
        RenderDrawData(ImGui.GetDrawData());
    }

    // -------------------------------------------------------------------------
    // Shutdown
    // -------------------------------------------------------------------------

    public static void Shutdown()
    {
        if (_globalContext.IsNull) return;

        ImGui.SetCurrentContext(_globalContext);
        ShutdownTextures();

        ImGui.DestroyContext(_globalContext);
        _globalContext = default;
    }

    // -------------------------------------------------------------------------
    // Internal: new-frame update
    // -------------------------------------------------------------------------

    private static void NewFrame(float deltaTime)
    {
        var io = ImGui.GetIO();

        io.DisplaySize.X = Raylib.GetScreenWidth();
        io.DisplaySize.Y = Raylib.GetScreenHeight();

        io.DeltaTime = deltaTime <= 0f ? 0.001f : deltaTime;

        // Update mouse cursor shape
        if ((io.BackendFlags & ImGuiBackendFlags.HasMouseCursors) != 0)
        {
            if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) == 0)
            {
                var imguiCursor = ImGui.GetMouseCursor();
                if (imguiCursor != _currentMouseCursor || io.MouseDrawCursor)
                {
                    _currentMouseCursor = imguiCursor;
                    if (io.MouseDrawCursor || imguiCursor == ImGuiMouseCursor.None)
                    {
                        Raylib.HideCursor();
                    }
                    else
                    {
                        Raylib.ShowCursor();
                        if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) == 0)
                        {
                            int cursorIdx = (int)imguiCursor;
                            Raylib.SetMouseCursor(
                                cursorIdx >= 0 && cursorIdx < (int)ImGuiMouseCursor.Count
                                    ? MouseCursorMap[cursorIdx]
                                    : MouseCursor.Default);
                        }
                    }
                }
            }
        }
    }

    // -------------------------------------------------------------------------
    // Internal: event processing
    // -------------------------------------------------------------------------

    private static void ProcessEvents()
    {
        var io = ImGui.GetIO();

        // Window focus
        bool focused = Raylib.IsWindowFocused();
        if (focused != _lastFrameFocused)
            io.AddFocusEvent(focused);
        _lastFrameFocused = focused;

        // Modifier keys
        bool ctrl = IsControlDown();
        if (ctrl != _lastControlPressed) io.AddKeyEvent(ImGuiKey.ModCtrl, ctrl);
        _lastControlPressed = ctrl;

        bool shift = IsShiftDown();
        if (shift != _lastShiftPressed) io.AddKeyEvent(ImGuiKey.ModShift, shift);
        _lastShiftPressed = shift;

        bool alt = IsAltDown();
        if (alt != _lastAltPressed) io.AddKeyEvent(ImGuiKey.ModAlt, alt);
        _lastAltPressed = alt;

        bool super = IsSuperDown();
        if (super != _lastSuperPressed) io.AddKeyEvent(ImGuiKey.ModSuper, super);
        _lastSuperPressed = super;

        // All mapped keys
        for (int keyIdx = 0; keyIdx < MaxRaylibKey; keyIdx++)
        {
            var imKey = RaylibKeyMap[keyIdx];
            if (imKey == 0) continue;

            if (Raylib.IsKeyReleased((KeyboardKey)keyIdx))
                io.AddKeyEvent(imKey, false);
            else if (Raylib.IsKeyPressed((KeyboardKey)keyIdx))
                io.AddKeyEvent(imKey, true);
        }

        // Text input
        if (io.WantCaptureKeyboard)
        {
            var pressed = Raylib.GetCharPressed();
            while (pressed != 0)
            {
                io.AddInputCharacter((uint)pressed);
                pressed = Raylib.GetCharPressed();
            }
        }

        // Mouse
        bool processMouse = focused;

        if (processMouse)
        {
            if (!io.WantSetMousePos)
                io.AddMousePosEvent(Raylib.GetMouseX(), Raylib.GetMouseY());

            void SetMouseButton(MouseButton rayBtn, int imBtn)
            {
                if (Raylib.IsMouseButtonPressed(rayBtn))
                    io.AddMouseButtonEvent(imBtn, true);
                else if (Raylib.IsMouseButtonReleased(rayBtn))
                    io.AddMouseButtonEvent(imBtn, false);
            }

            SetMouseButton(MouseButton.Left, (int)ImGuiMouseButton.Left);
            SetMouseButton(MouseButton.Right, (int)ImGuiMouseButton.Right);
            SetMouseButton(MouseButton.Middle, (int)ImGuiMouseButton.Middle);
            SetMouseButton(MouseButton.Forward, (int)ImGuiMouseButton.Middle + 1);
            SetMouseButton(MouseButton.Back, (int)ImGuiMouseButton.Middle + 2);

            var wheel = Raylib.GetMouseWheelMoveV();
            io.AddMouseWheelEvent(wheel.X, wheel.Y);
        }
        else
        {
            io.AddMousePosEvent(float.MinValue, float.MinValue);
        }

        // Gamepad
        if ((io.ConfigFlags & ImGuiConfigFlags.NavEnableGamepad) != 0 && Raylib.IsGamepadAvailable(0))
        {
            HandleGamepadButton(io, GamepadButton.LeftFaceUp, ImGuiKey.GamepadDpadUp);
            HandleGamepadButton(io, GamepadButton.LeftFaceRight, ImGuiKey.GamepadDpadRight);
            HandleGamepadButton(io, GamepadButton.LeftFaceDown, ImGuiKey.GamepadDpadDown);
            HandleGamepadButton(io, GamepadButton.LeftFaceLeft, ImGuiKey.GamepadDpadLeft);

            HandleGamepadButton(io, GamepadButton.RightFaceUp, ImGuiKey.GamepadFaceUp);
            HandleGamepadButton(io, GamepadButton.RightFaceRight, ImGuiKey.GamepadFaceLeft);
            HandleGamepadButton(io, GamepadButton.RightFaceDown, ImGuiKey.GamepadFaceDown);
            HandleGamepadButton(io, GamepadButton.RightFaceLeft, ImGuiKey.GamepadFaceRight);

            HandleGamepadButton(io, GamepadButton.LeftTrigger1, ImGuiKey.GamepadL1);
            HandleGamepadButton(io, GamepadButton.LeftTrigger2, ImGuiKey.GamepadL2);
            HandleGamepadButton(io, GamepadButton.RightTrigger1, ImGuiKey.GamepadR1);
            HandleGamepadButton(io, GamepadButton.RightTrigger2, ImGuiKey.GamepadR2);
            HandleGamepadButton(io, GamepadButton.LeftThumb, ImGuiKey.GamepadL3);
            HandleGamepadButton(io, GamepadButton.RightThumb, ImGuiKey.GamepadR3);

            HandleGamepadButton(io, GamepadButton.MiddleLeft, ImGuiKey.GamepadStart);
            HandleGamepadButton(io, GamepadButton.MiddleRight, ImGuiKey.GamepadBack);

            HandleGamepadStick(io, GamepadAxis.LeftX, ImGuiKey.GamepadLStickLeft, ImGuiKey.GamepadLStickRight);
            HandleGamepadStick(io, GamepadAxis.LeftY, ImGuiKey.GamepadLStickUp, ImGuiKey.GamepadLStickDown);
            HandleGamepadStick(io, GamepadAxis.RightX, ImGuiKey.GamepadRStickLeft, ImGuiKey.GamepadRStickRight);
            HandleGamepadStick(io, GamepadAxis.RightY, ImGuiKey.GamepadRStickUp, ImGuiKey.GamepadRStickDown);
        }
    }

    private static void HandleGamepadButton(ImGuiIOPtr io, GamepadButton button, ImGuiKey key)
    {
        if (Raylib.IsGamepadButtonPressed(0, button))
            io.AddKeyEvent(key, true);
        else if (Raylib.IsGamepadButtonReleased(0, button))
            io.AddKeyEvent(key, false);
    }

    private static void HandleGamepadStick(ImGuiIOPtr io, GamepadAxis axis, ImGuiKey negKey, ImGuiKey posKey)
    {
        const float deadZone = 0.20f;
        float v = Raylib.GetGamepadAxisMovement(0, axis);
        io.AddKeyAnalogEvent(negKey, v < -deadZone, v < -deadZone ? -v : 0f);
        io.AddKeyAnalogEvent(posKey, v > deadZone, v > deadZone ? v : 0f);
    }

    // -------------------------------------------------------------------------
    // Internal: scissor / rendering helpers
    // -------------------------------------------------------------------------

    private static void EnableScissor(float x, float y, float width, float height)
    {
        Rlgl.EnableScissorTest();
        var io = ImGui.GetIO();

        var scale = io.DisplayFramebufferScale;

        Rlgl.Scissor(
            (int)(x * scale.X),
            (int)((io.DisplaySize.Y - (y + height)) * scale.Y),
            (int)(width * scale.X),
            (int)(height * scale.Y));
    }

    private static void TriangleVert(in ImDrawVert vert)
    {
        byte r = (byte)(vert.Col >> 0);
        byte g = (byte)(vert.Col >> 8);
        byte b = (byte)(vert.Col >> 16);
        byte a = (byte)(vert.Col >> 24);

        Rlgl.Color4ub(r, g, b, a);
        Rlgl.TexCoord2f(vert.Uv.X, vert.Uv.Y);
        Rlgl.Vertex2f(vert.Pos.X, vert.Pos.Y);
    }

    private static void RenderTriangles(
        uint count,
        int indexStart,
        ImVector<ushort> indexBuffer,
        ImVector<ImDrawVert> vertBuffer,
        ulong textureId)
    {
        if (count < 3) return;

        Rlgl.Begin(0x0004); // RL_TRIANGLES
        Rlgl.SetTexture((uint)textureId);

        for (uint i = 0; i <= count - 3; i += 3)
        {
            var vA = vertBuffer[(int)indexBuffer[indexStart + (int)i]];
            var vB = vertBuffer[(int)indexBuffer[indexStart + (int)i + 1]];
            var vC = vertBuffer[(int)indexBuffer[indexStart + (int)i + 2]];

            TriangleVert(in vA);
            TriangleVert(in vB);
            TriangleVert(in vC);
        }

        Rlgl.End();
    }

    // -------------------------------------------------------------------------
    // Internal: texture management
    // -------------------------------------------------------------------------

    private static readonly Dictionary<nint, Texture2D> Textures = new();

    private static void UpdateTexture(ImTextureData* tex)
    {
        switch (tex->Status)
        {
            case ImTextureStatus.Ok:
            case ImTextureStatus.Destroyed:
            default:
                break;

            case ImTextureStatus.WantCreate:
            {
                var img = new Image
                {
                    Width = tex->Width,
                    Height = tex->Height,
                    Mipmaps = 1,
                    Format = tex->Format == ImTextureFormat.Alpha8
                        ? PixelFormat.UncompressedGrayscale
                        : PixelFormat.UncompressedR8G8B8A8,
                    Data = tex->GetPixels()
                };

                var texture = Raylib.LoadTextureFromImage(img);
                Textures[(nint)tex] = texture;
                tex->SetTexID((ulong)texture.Id);
                tex->Status = ImTextureStatus.Ok;
                break;
            }

            case ImTextureStatus.WantUpdates:
            {
                if (!Textures.TryGetValue((nint)tex, out var texture)) break;
                Raylib.UpdateTexture(texture, tex->GetPixels());
                tex->Status = ImTextureStatus.Ok;
                break;
            }

            case ImTextureStatus.WantDestroy:
            {
                if (Textures.Remove((nint)tex, out var texture))
                    Raylib.UnloadTexture(texture);

                tex->SetTexID(0ul);
                tex->Status = ImTextureStatus.Destroyed;
                break;
            }
        }
    }

    private static void ShutdownTextures()
    {
        var platformIO = ImGui.GetPlatformIO();
        for (int i = 0; i < platformIO.Textures.Size; i++)
        {
            var tex = platformIO.Textures[i];
            if (tex.Handle->Status != ImTextureStatus.Destroyed)
            {
                if (Textures.Remove((nint)tex.Handle, out var texture))
                    Raylib.UnloadTexture(texture);

                tex.Handle->Status = ImTextureStatus.Destroyed;
                tex.Handle->SetTexID(0ul);
            }
        }

        Textures.Clear();
    }

    // -------------------------------------------------------------------------
    // Internal: draw data rendering
    // -------------------------------------------------------------------------
    private delegate void Callback(ImDrawListPtr list, ImDrawCmd cmd);

    private static void RenderDrawData(ImDrawDataPtr drawData)
    {
        // Process any pending texture operations first.
        if (drawData.Handle->Textures != IntPtr.Zero.ToPointer())
        {
            for (int i = 0; i < drawData.Handle->Textures->Size; i++)
            {
                var tex = drawData.Handle->Textures->Data[i];
                if (tex.Handle->Status != ImTextureStatus.Ok)
                    UpdateTexture(tex);
            }
        }

        Rlgl.DrawRenderBatchActive();
        Rlgl.DisableBackfaceCulling();

        for (int l = 0; l < drawData.CmdListsCount; l++)
        {
            var cmdList = drawData.CmdLists[l];

            for (int c = 0; c < cmdList.CmdBuffer.Size; c++)
            {
                var cmd = cmdList.CmdBuffer[c];

                EnableScissor(
                    cmd.ClipRect.X - drawData.DisplayPos.X,
                    cmd.ClipRect.Y - drawData.DisplayPos.Y,
                    cmd.ClipRect.Z - (cmd.ClipRect.X - drawData.DisplayPos.X),
                    cmd.ClipRect.W - (cmd.ClipRect.Y - drawData.DisplayPos.Y));

                if (cmd.UserCallback != IntPtr.Zero.ToPointer())
                {
                    var cb = Marshal.GetDelegateForFunctionPointer<Callback>(new IntPtr(cmd.UserCallback));
                    cb(cmdList, cmd);
                    continue;
                }

                RenderTriangles(
                    cmd.ElemCount,
                    (int)cmd.IdxOffset,
                    cmdList.IdxBuffer,
                    cmdList.VtxBuffer,
                    (ulong)cmd.GetTexID());

                Rlgl.DrawRenderBatchActive();
            }
        }

        Rlgl.SetTexture(0);
        Rlgl.DisableScissorTest();
        Rlgl.EnableBackfaceCulling();
    }

    // -------------------------------------------------------------------------
    // Public image helpers
    // -------------------------------------------------------------------------

    public static void Image(in Texture2D image)
    {
        if (_globalContext.Handle != IntPtr.Zero.ToPointer())
            ImGui.SetCurrentContext(_globalContext);

        ImGui.Image(new ImTextureRef(texId: image.Id), new Vector2(image.Width, image.Height));
    }

    public static void ImageSize(in Texture2D image, int width, int height)
    {
        if (_globalContext.Handle != IntPtr.Zero.ToPointer())
            ImGui.SetCurrentContext(_globalContext);

        ImGui.Image(new ImTextureRef(texId: image.Id), new Vector2(width, height));
    }

    public static void ImageSizeV(in Texture2D image, Vector2 size)
    {
        if (_globalContext.Handle != IntPtr.Zero.ToPointer())
            ImGui.SetCurrentContext(_globalContext);

        ImGui.Image(new ImTextureRef(texId: image.Id), size);
    }

    public static bool ImageButton(string name, in Texture2D image)
    {
        if (_globalContext.Handle != IntPtr.Zero.ToPointer())
            ImGui.SetCurrentContext(_globalContext);

        return ImGui.ImageButton(name, new ImTextureRef(texId: image.Id), new Vector2(image.Width, image.Height));
    }

    public static bool ImageButtonSize(string name, in Texture2D image, Vector2 size)
    {
        if (_globalContext.Handle != IntPtr.Zero.ToPointer())
            ImGui.SetCurrentContext(_globalContext);

        return ImGui.ImageButton(name, new ImTextureRef(texId: image.Id), size);
    }

    public static void ImageRect(in Texture2D image, int destWidth, int destHeight, Rectangle sourceRect)
    {
        if (_globalContext.Handle != IntPtr.Zero.ToPointer())
            ImGui.SetCurrentContext(_globalContext);

        Vector2 uv0, uv1;

        if (sourceRect.Width < 0)
        {
            uv0.X = -sourceRect.X / image.Width;
            uv1.X = uv0.X - MathF.Abs(sourceRect.Width) / image.Width;
        }
        else
        {
            uv0.X = sourceRect.X / image.Width;
            uv1.X = uv0.X + sourceRect.Width / image.Width;
        }

        if (sourceRect.Height < 0)
        {
            uv0.Y = -sourceRect.Y / image.Height;
            uv1.Y = uv0.Y - MathF.Abs(sourceRect.Height) / image.Height;
        }
        else
        {
            uv0.Y = sourceRect.Y / image.Height;
            uv1.Y = uv0.Y + sourceRect.Height / image.Height;
        }

        ImGui.Image(new ImTextureRef(texId: image.Id), new Vector2(destWidth, destHeight), uv0, uv1);
    }

    public static void ImageRenderTexture(in RenderTexture2D renderTexture)
    {
        ImageRect(
            renderTexture.Texture,
            renderTexture.Texture.Width,
            renderTexture.Texture.Height,
            new Rectangle(0, 0, renderTexture.Texture.Width, -renderTexture.Texture.Height));
    }

    public static void ImageRenderTextureFit(in RenderTexture2D renderTexture, bool center = true)
    {
        if (_globalContext.Handle != IntPtr.Zero.ToPointer())
            ImGui.SetCurrentContext(_globalContext);

        var area = ImGui.GetContentRegionAvail();

        float scale = area.X / renderTexture.Texture.Width;
        float y = renderTexture.Texture.Height * scale;
        if (y > area.Y)
            scale = area.Y / renderTexture.Texture.Height;

        int sizeX = (int)(renderTexture.Texture.Width * scale);
        int sizeY = (int)(renderTexture.Texture.Height * scale);

        if (center)
        {
            ImGui.SetCursorPosX(0);
            ImGui.SetCursorPosX(area.X / 2f - sizeX / 2f);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (area.Y / 2f - sizeY / 2f));
        }

        ImageRect(
            renderTexture.Texture,
            sizeX, sizeY,
            new Rectangle(0, 0, renderTexture.Texture.Width, -renderTexture.Texture.Height));
    }
}