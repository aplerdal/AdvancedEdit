/*******************************************************************************************
 *
 *   raylib-extras [ImGui] example - Simple Integration
 *
 *	This is a simple ImGui Integration
 *	It is done using C++ but with C style code
 *	It can be done in C as well if you use the C ImGui wrapper
 *	https://github.com/cimgui/cimgui
 *
 *   Copyright (c) 2021 Jeffery Myers
 *
 ********************************************************************************************/

using System.Numerics;
using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework.DearImGui
{
    public static class ImGuiRenderer
    {
        internal static ImGuiContextPtr ImGuiContext = ImGuiContextPtr.Null;

        private static ImGuiMouseCursor _currentMouseCursor = ImGuiMouseCursor.Count;
        private static Dictionary<ImGuiMouseCursor, MouseCursor> _mouseCursorMap = new Dictionary<ImGuiMouseCursor, MouseCursor>();
        public static Texture2D _fontTexture;

        private static Dictionary<KeyboardKey, ImGuiKey> _raylibKeyMap = new Dictionary<KeyboardKey, ImGuiKey>();

        internal static bool LastFrameFocused = false;

        internal static bool LastControlPressed = false;
        internal static bool LastShiftPressed = false;
        internal static bool LastAltPressed = false;
        internal static bool LastSuperPressed = false;

        internal static bool IsControlDown()
        {
            return Raylib.IsKeyDown(KeyboardKey.RightControl) || Raylib.IsKeyDown(KeyboardKey.LeftControl);
        }

        internal static bool IsShiftDown()
        {
            return Raylib.IsKeyDown(KeyboardKey.RightShift) || Raylib.IsKeyDown(KeyboardKey.LeftShift);
        }

        internal static bool IsAltDown()
        {
            return Raylib.IsKeyDown(KeyboardKey.RightAlt) || Raylib.IsKeyDown(KeyboardKey.LeftAlt);
        }

        internal static bool IsSuperDown()
        {
            return Raylib.IsKeyDown(KeyboardKey.RightSuper) || Raylib.IsKeyDown(KeyboardKey.LeftSuper);
        }

        public delegate void SetupUserFontsCallback(ImGuiIOPtr imGuiIo);

        /// <summary>
        /// Callback for cases where the user wants to install additional fonts.
        /// </summary>
        public static SetupUserFontsCallback SetupUserFonts = null;

        /// <summary>
        /// Sets up ImGui, loads fonts and themes
        /// </summary>
        /// <param name="darkTheme">when true(default) the dark theme is used, when false the light theme is used</param>
        /// <param name="enableDocking">when true(not default) docking support will be enabled</param>
        public static void Setup(bool darkTheme = true, bool enableDocking = false)
        {
            BeginInitImGui();

            if (darkTheme)
                ImGui.StyleColorsDark();
            else
                ImGui.StyleColorsLight();

            if (enableDocking)
                ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            EndInitImGui();
        }

        /// <summary>
        /// Custom initialization. Not needed if you call Setup. Only needed if you want to add custom setup code.
        /// must be followed by EndInitImGui
        /// </summary>
        public static void BeginInitImGui()
        {
            _mouseCursorMap = new Dictionary<ImGuiMouseCursor, MouseCursor>();

            LastFrameFocused = Raylib.IsWindowFocused();
            LastControlPressed = false;
            LastShiftPressed = false;
            LastAltPressed = false;
            LastSuperPressed = false;

            _fontTexture.Id = 0;

            SetupKeymap();

            ImGuiContext = ImGui.CreateContext();
        }

        internal static void SetupKeymap()
        {
            if (_raylibKeyMap.Count > 0)
                return;

            // build up a map of raylib keys to ImGuiKeys
            _raylibKeyMap[KeyboardKey.Apostrophe] = ImGuiKey.Apostrophe;
            _raylibKeyMap[KeyboardKey.Comma] = ImGuiKey.Comma;
            _raylibKeyMap[KeyboardKey.Minus] = ImGuiKey.Minus;
            _raylibKeyMap[KeyboardKey.Period] = ImGuiKey.Period;
            _raylibKeyMap[KeyboardKey.Slash] = ImGuiKey.Slash;
            _raylibKeyMap[KeyboardKey.Zero] = ImGuiKey.Key0;
            _raylibKeyMap[KeyboardKey.One] = ImGuiKey.Key1;
            _raylibKeyMap[KeyboardKey.Two] = ImGuiKey.Key2;
            _raylibKeyMap[KeyboardKey.Three] = ImGuiKey.Key3;
            _raylibKeyMap[KeyboardKey.Four] = ImGuiKey.Key4;
            _raylibKeyMap[KeyboardKey.Five] = ImGuiKey.Key5;
            _raylibKeyMap[KeyboardKey.Six] = ImGuiKey.Key6;
            _raylibKeyMap[KeyboardKey.Seven] = ImGuiKey.Key7;
            _raylibKeyMap[KeyboardKey.Eight] = ImGuiKey.Key8;
            _raylibKeyMap[KeyboardKey.Nine] = ImGuiKey.Key9;
            _raylibKeyMap[KeyboardKey.Semicolon] = ImGuiKey.Semicolon;
            _raylibKeyMap[KeyboardKey.Equal] = ImGuiKey.Equal;
            _raylibKeyMap[KeyboardKey.A] = ImGuiKey.A;
            _raylibKeyMap[KeyboardKey.B] = ImGuiKey.B;
            _raylibKeyMap[KeyboardKey.C] = ImGuiKey.C;
            _raylibKeyMap[KeyboardKey.D] = ImGuiKey.D;
            _raylibKeyMap[KeyboardKey.E] = ImGuiKey.E;
            _raylibKeyMap[KeyboardKey.F] = ImGuiKey.F;
            _raylibKeyMap[KeyboardKey.G] = ImGuiKey.G;
            _raylibKeyMap[KeyboardKey.H] = ImGuiKey.H;
            _raylibKeyMap[KeyboardKey.I] = ImGuiKey.I;
            _raylibKeyMap[KeyboardKey.J] = ImGuiKey.J;
            _raylibKeyMap[KeyboardKey.K] = ImGuiKey.K;
            _raylibKeyMap[KeyboardKey.L] = ImGuiKey.L;
            _raylibKeyMap[KeyboardKey.M] = ImGuiKey.M;
            _raylibKeyMap[KeyboardKey.N] = ImGuiKey.N;
            _raylibKeyMap[KeyboardKey.O] = ImGuiKey.O;
            _raylibKeyMap[KeyboardKey.P] = ImGuiKey.P;
            _raylibKeyMap[KeyboardKey.Q] = ImGuiKey.Q;
            _raylibKeyMap[KeyboardKey.R] = ImGuiKey.R;
            _raylibKeyMap[KeyboardKey.S] = ImGuiKey.S;
            _raylibKeyMap[KeyboardKey.T] = ImGuiKey.T;
            _raylibKeyMap[KeyboardKey.U] = ImGuiKey.U;
            _raylibKeyMap[KeyboardKey.V] = ImGuiKey.V;
            _raylibKeyMap[KeyboardKey.W] = ImGuiKey.W;
            _raylibKeyMap[KeyboardKey.X] = ImGuiKey.X;
            _raylibKeyMap[KeyboardKey.Y] = ImGuiKey.Y;
            _raylibKeyMap[KeyboardKey.Z] = ImGuiKey.Z;
            _raylibKeyMap[KeyboardKey.Space] = ImGuiKey.Space;
            _raylibKeyMap[KeyboardKey.Escape] = ImGuiKey.Escape;
            _raylibKeyMap[KeyboardKey.Enter] = ImGuiKey.Enter;
            _raylibKeyMap[KeyboardKey.Tab] = ImGuiKey.Tab;
            _raylibKeyMap[KeyboardKey.Backspace] = ImGuiKey.Backspace;
            _raylibKeyMap[KeyboardKey.Insert] = ImGuiKey.Insert;
            _raylibKeyMap[KeyboardKey.Delete] = ImGuiKey.Delete;
            _raylibKeyMap[KeyboardKey.Right] = ImGuiKey.RightArrow;
            _raylibKeyMap[KeyboardKey.Left] = ImGuiKey.LeftArrow;
            _raylibKeyMap[KeyboardKey.Down] = ImGuiKey.DownArrow;
            _raylibKeyMap[KeyboardKey.Up] = ImGuiKey.UpArrow;
            _raylibKeyMap[KeyboardKey.PageUp] = ImGuiKey.PageUp;
            _raylibKeyMap[KeyboardKey.PageDown] = ImGuiKey.PageDown;
            _raylibKeyMap[KeyboardKey.Home] = ImGuiKey.Home;
            _raylibKeyMap[KeyboardKey.End] = ImGuiKey.End;
            _raylibKeyMap[KeyboardKey.CapsLock] = ImGuiKey.CapsLock;
            _raylibKeyMap[KeyboardKey.ScrollLock] = ImGuiKey.ScrollLock;
            _raylibKeyMap[KeyboardKey.NumLock] = ImGuiKey.NumLock;
            _raylibKeyMap[KeyboardKey.PrintScreen] = ImGuiKey.PrintScreen;
            _raylibKeyMap[KeyboardKey.Pause] = ImGuiKey.Pause;
            _raylibKeyMap[KeyboardKey.F1] = ImGuiKey.F1;
            _raylibKeyMap[KeyboardKey.F2] = ImGuiKey.F2;
            _raylibKeyMap[KeyboardKey.F3] = ImGuiKey.F3;
            _raylibKeyMap[KeyboardKey.F4] = ImGuiKey.F4;
            _raylibKeyMap[KeyboardKey.F5] = ImGuiKey.F5;
            _raylibKeyMap[KeyboardKey.F6] = ImGuiKey.F6;
            _raylibKeyMap[KeyboardKey.F7] = ImGuiKey.F7;
            _raylibKeyMap[KeyboardKey.F8] = ImGuiKey.F8;
            _raylibKeyMap[KeyboardKey.F9] = ImGuiKey.F9;
            _raylibKeyMap[KeyboardKey.F10] = ImGuiKey.F10;
            _raylibKeyMap[KeyboardKey.F11] = ImGuiKey.F11;
            _raylibKeyMap[KeyboardKey.F12] = ImGuiKey.F12;
            _raylibKeyMap[KeyboardKey.LeftShift] = ImGuiKey.LeftShift;
            _raylibKeyMap[KeyboardKey.LeftControl] = ImGuiKey.LeftCtrl;
            _raylibKeyMap[KeyboardKey.LeftAlt] = ImGuiKey.LeftAlt;
            _raylibKeyMap[KeyboardKey.LeftSuper] = ImGuiKey.LeftSuper;
            _raylibKeyMap[KeyboardKey.RightShift] = ImGuiKey.RightShift;
            _raylibKeyMap[KeyboardKey.RightControl] = ImGuiKey.RightCtrl;
            _raylibKeyMap[KeyboardKey.RightAlt] = ImGuiKey.RightAlt;
            _raylibKeyMap[KeyboardKey.RightSuper] = ImGuiKey.RightSuper;
            _raylibKeyMap[KeyboardKey.KeyboardMenu] = ImGuiKey.Menu;
            _raylibKeyMap[KeyboardKey.LeftBracket] = ImGuiKey.LeftBracket;
            _raylibKeyMap[KeyboardKey.Backslash] = ImGuiKey.Backslash;
            _raylibKeyMap[KeyboardKey.RightBracket] = ImGuiKey.RightBracket;
            _raylibKeyMap[KeyboardKey.Grave] = ImGuiKey.GraveAccent;
            _raylibKeyMap[KeyboardKey.Kp0] = ImGuiKey.Keypad0;
            _raylibKeyMap[KeyboardKey.Kp1] = ImGuiKey.Keypad1;
            _raylibKeyMap[KeyboardKey.Kp2] = ImGuiKey.Keypad2;
            _raylibKeyMap[KeyboardKey.Kp3] = ImGuiKey.Keypad3;
            _raylibKeyMap[KeyboardKey.Kp4] = ImGuiKey.Keypad4;
            _raylibKeyMap[KeyboardKey.Kp5] = ImGuiKey.Keypad5;
            _raylibKeyMap[KeyboardKey.Kp6] = ImGuiKey.Keypad6;
            _raylibKeyMap[KeyboardKey.Kp7] = ImGuiKey.Keypad7;
            _raylibKeyMap[KeyboardKey.Kp8] = ImGuiKey.Keypad8;
            _raylibKeyMap[KeyboardKey.Kp9] = ImGuiKey.Keypad9;
            _raylibKeyMap[KeyboardKey.KpDecimal] = ImGuiKey.KeypadDecimal;
            _raylibKeyMap[KeyboardKey.KpDivide] = ImGuiKey.KeypadDivide;
            _raylibKeyMap[KeyboardKey.KpMultiply] = ImGuiKey.KeypadMultiply;
            _raylibKeyMap[KeyboardKey.KpSubtract] = ImGuiKey.KeypadSubtract;
            _raylibKeyMap[KeyboardKey.KpAdd] = ImGuiKey.KeypadAdd;
            _raylibKeyMap[KeyboardKey.KpEnter] = ImGuiKey.KeypadEnter;
            _raylibKeyMap[KeyboardKey.KpEqual] = ImGuiKey.KeypadEqual;
        }

        private static void SetupMouseCursors()
        {
            _mouseCursorMap.Clear();
            _mouseCursorMap[ImGuiMouseCursor.Arrow] = MouseCursor.Arrow;
            _mouseCursorMap[ImGuiMouseCursor.TextInput] = MouseCursor.IBeam;
            _mouseCursorMap[ImGuiMouseCursor.Hand] = MouseCursor.PointingHand;
            _mouseCursorMap[ImGuiMouseCursor.ResizeAll] = MouseCursor.ResizeAll;
            _mouseCursorMap[ImGuiMouseCursor.ResizeEw] = MouseCursor.ResizeEw;
            _mouseCursorMap[ImGuiMouseCursor.ResizeNesw] = MouseCursor.ResizeNesw;
            _mouseCursorMap[ImGuiMouseCursor.ResizeNs] = MouseCursor.ResizeNs;
            _mouseCursorMap[ImGuiMouseCursor.ResizeNwse] = MouseCursor.ResizeNwse;
            _mouseCursorMap[ImGuiMouseCursor.NotAllowed] = MouseCursor.NotAllowed;
        }

        /// <summary>
        /// Forces the font texture atlas to be recomputed and re-cached
        /// </summary>
        public static unsafe void ReloadFonts()
        {
            ImGui.SetCurrentContext(ImGuiContext);
            ImGuiIOPtr io = ImGui.GetIO();

            int width = 0, height = 0, bytesPerPixel = 0;
            byte* pixels = null;
            io.Fonts.GetTexDataAsRGBA32(ref pixels, ref width, ref height, ref bytesPerPixel);

            Image image = new Image
            {
                Data = pixels,
                Width = width,
                Height = height,
                Mipmaps = 1,
                Format = PixelFormat.UncompressedR8G8B8A8,
            };

            if (Raylib.IsTextureValid(_fontTexture))
                Raylib.UnloadTexture(_fontTexture);

            _fontTexture = Raylib.LoadTextureFromImage(image);

            io.Fonts.SetTexID(new ImTextureID(_fontTexture.Id));
        }

        unsafe internal static sbyte* rlImGuiGetClipText(IntPtr userData)
        {
            return Raylib.GetClipboardText();
        }

        unsafe internal static void rlImGuiSetClipText(IntPtr userData, sbyte* text)
        {
            Raylib.SetClipboardText(text);
        }

        private unsafe delegate sbyte* GetClipTextCallback(IntPtr userData);

        private unsafe delegate void SetClipTextCallback(IntPtr userData, sbyte* text);

        private static GetClipTextCallback GetClipCallback = null!;
        private static SetClipTextCallback SetClipCallback = null!;

        public static bool LoadDefaultFont = true;

        /// <summary>
        /// End Custom initialization. Not needed if you call Setup. Only needed if you want to add custom setup code.
        /// must be proceeded by BeginInitImGui
        /// </summary>
        public static unsafe void EndInitImGui()
        {
            SetupMouseCursors();

            ImGui.SetCurrentContext(ImGuiContext);

            var fonts = ImGui.GetIO().Fonts;

            if (LoadDefaultFont)
                ImGui.GetIO().Fonts.AddFontDefault();

            ImGuiIOPtr io = ImGui.GetIO();

            ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();

            if (SetupUserFonts != null)
                SetupUserFonts(io);

            io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors | ImGuiBackendFlags.HasSetMousePos | ImGuiBackendFlags.HasGamepad;

            io.MousePos.X = 0;
            io.MousePos.Y = 0;

            // copy/paste callbacks
            unsafe
            {
                SetClipCallback = new SetClipTextCallback(rlImGuiSetClipText);
                platformIO.PlatformSetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(SetClipCallback).ToPointer();

                GetClipCallback = new GetClipTextCallback(rlImGuiGetClipText);
                platformIO.PlatformGetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(GetClipCallback).ToPointer();
            }

            platformIO.PlatformClipboardUserData = IntPtr.Zero.ToPointer();
            ReloadFonts();
        }

        private static void SetMouseEvent(ImGuiIOPtr io, MouseButton rayMouse, ImGuiMouseButton imGuiMouse)
        {
            if (Raylib.IsMouseButtonPressed(rayMouse))
                io.AddMouseButtonEvent((int)imGuiMouse, true);
            else if (Raylib.IsMouseButtonReleased(rayMouse))
                io.AddMouseButtonEvent((int)imGuiMouse, false);
        }

        private static void NewFrame(float dt = -1)
        {
            ImGuiIOPtr io = ImGui.GetIO();

            if (Raylib.IsWindowFullscreen())
            {
                int monitor = Raylib.GetCurrentMonitor();
                io.DisplaySize = new Vector2(Raylib.GetMonitorWidth(monitor), Raylib.GetMonitorHeight(monitor));
            }
            else
            {
                io.DisplaySize = new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
            }

            io.DisplayFramebufferScale = new Vector2(1, 1);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || Raylib.IsWindowState(ConfigFlags.HighDpiWindow))
                io.DisplayFramebufferScale = Raylib.GetWindowScaleDPI();

            io.DeltaTime = dt >= 0 ? dt : Raylib.GetFrameTime();

            if (io.WantSetMousePos)
            {
                Raylib.SetMousePosition((int)io.MousePos.X, (int)io.MousePos.Y);
            }
            else
            {
                io.AddMousePosEvent(Raylib.GetMouseX(), Raylib.GetMouseY());
            }

            SetMouseEvent(io, MouseButton.Left, ImGuiMouseButton.Left);
            SetMouseEvent(io, MouseButton.Right, ImGuiMouseButton.Right);
            SetMouseEvent(io, MouseButton.Middle, ImGuiMouseButton.Middle);
            SetMouseEvent(io, MouseButton.Forward, ImGuiMouseButton.Middle + 1);
            SetMouseEvent(io, MouseButton.Back, ImGuiMouseButton.Middle + 2);

            var wheelMove = Raylib.GetMouseWheelMoveV();
            io.AddMouseWheelEvent(wheelMove.X, wheelMove.Y);

            if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) == 0)
            {
                ImGuiMouseCursor imgui_cursor = ImGui.GetMouseCursor();
                if (imgui_cursor != _currentMouseCursor || io.MouseDrawCursor)
                {
                    _currentMouseCursor = imgui_cursor;
                    if (io.MouseDrawCursor || imgui_cursor == ImGuiMouseCursor.None)
                    {
                        Raylib.HideCursor();
                    }
                    else
                    {
                        Raylib.ShowCursor();

                        if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) == 0)
                        {
                            if (!_mouseCursorMap.ContainsKey(imgui_cursor))
                                Raylib.SetMouseCursor(MouseCursor.Default);
                            else
                                Raylib.SetMouseCursor(_mouseCursorMap[imgui_cursor]);
                        }
                    }
                }
            }
        }

        private static void FrameEvents()
        {
            ImGuiIOPtr io = ImGui.GetIO();

            bool focused = Raylib.IsWindowFocused();
            if (focused != LastFrameFocused)
                io.AddFocusEvent(focused);
            LastFrameFocused = focused;


            // handle the modifyer key events so that shortcuts work
            bool ctrlDown = IsControlDown();
            if (ctrlDown != LastControlPressed)
                io.AddKeyEvent(ImGuiKey.ModCtrl, ctrlDown);
            LastControlPressed = ctrlDown;

            bool shiftDown = IsShiftDown();
            if (shiftDown != LastShiftPressed)
                io.AddKeyEvent(ImGuiKey.ModShift, shiftDown);
            LastShiftPressed = shiftDown;

            bool altDown = IsAltDown();
            if (altDown != LastAltPressed)
                io.AddKeyEvent(ImGuiKey.ModAlt, altDown);
            LastAltPressed = altDown;

            bool superDown = IsSuperDown();
            if (superDown != LastSuperPressed)
                io.AddKeyEvent(ImGuiKey.ModSuper, superDown);
            LastSuperPressed = superDown;

            // get the pressed keys, they are in event order
            int keyId = Raylib.GetKeyPressed();
            while (keyId != 0)
            {
                KeyboardKey key = (KeyboardKey)keyId;
                if (_raylibKeyMap.ContainsKey(key))
                    io.AddKeyEvent(_raylibKeyMap[key], true);
                keyId = Raylib.GetKeyPressed();
            }

            // look for any keys that were down last frame and see if they were down and are released
            foreach (var keyItr in _raylibKeyMap)
            {
                if (Raylib.IsKeyReleased(keyItr.Key))
                    io.AddKeyEvent(keyItr.Value, false);
            }

            // add the text input in order
            var pressed = Raylib.GetCharPressed();
            while (pressed != 0)
            {
                io.AddInputCharacter((uint)pressed);
                pressed = Raylib.GetCharPressed();
            }

            // gamepads
            if ((io.ConfigFlags & ImGuiConfigFlags.NavEnableGamepad) != 0 && Raylib.IsGamepadAvailable(0))
            {
                HandleGamepadButtonEvent(io, GamepadButton.LeftFaceUp, ImGuiKey.GamepadDpadUp);
                HandleGamepadButtonEvent(io, GamepadButton.LeftFaceRight, ImGuiKey.GamepadDpadRight);
                HandleGamepadButtonEvent(io, GamepadButton.LeftFaceDown, ImGuiKey.GamepadDpadDown);
                HandleGamepadButtonEvent(io, GamepadButton.LeftFaceLeft, ImGuiKey.GamepadDpadLeft);

                HandleGamepadButtonEvent(io, GamepadButton.RightFaceUp, ImGuiKey.GamepadFaceUp);
                HandleGamepadButtonEvent(io, GamepadButton.RightFaceRight, ImGuiKey.GamepadFaceLeft);
                HandleGamepadButtonEvent(io, GamepadButton.RightFaceDown, ImGuiKey.GamepadFaceDown);
                HandleGamepadButtonEvent(io, GamepadButton.RightFaceLeft, ImGuiKey.GamepadFaceRight);

                HandleGamepadButtonEvent(io, GamepadButton.LeftTrigger1, ImGuiKey.GamepadL1);
                HandleGamepadButtonEvent(io, GamepadButton.LeftTrigger2, ImGuiKey.GamepadL2);
                HandleGamepadButtonEvent(io, GamepadButton.RightTrigger1, ImGuiKey.GamepadR1);
                HandleGamepadButtonEvent(io, GamepadButton.RightTrigger2, ImGuiKey.GamepadR2);
                HandleGamepadButtonEvent(io, GamepadButton.LeftThumb, ImGuiKey.GamepadL3);
                HandleGamepadButtonEvent(io, GamepadButton.RightThumb, ImGuiKey.GamepadR3);

                HandleGamepadButtonEvent(io, GamepadButton.MiddleLeft, ImGuiKey.GamepadStart);
                HandleGamepadButtonEvent(io, GamepadButton.MiddleRight, ImGuiKey.GamepadBack);

                // left stick
                HandleGamepadStickEvent(io, GamepadAxis.LeftX, ImGuiKey.GamepadLStickLeft, ImGuiKey.GamepadLStickRight);
                HandleGamepadStickEvent(io, GamepadAxis.LeftY, ImGuiKey.GamepadLStickUp, ImGuiKey.GamepadLStickDown);

                // right stick
                HandleGamepadStickEvent(io, GamepadAxis.RightX, ImGuiKey.GamepadRStickLeft, ImGuiKey.GamepadRStickRight);
                HandleGamepadStickEvent(io, GamepadAxis.RightY, ImGuiKey.GamepadRStickUp, ImGuiKey.GamepadRStickDown);
            }
        }


        private static void HandleGamepadButtonEvent(ImGuiIOPtr io, GamepadButton button, ImGuiKey key)
        {
            if (Raylib.IsGamepadButtonPressed(0, button))
                io.AddKeyEvent(key, true);
            else if (Raylib.IsGamepadButtonReleased(0, button))
                io.AddKeyEvent(key, false);
        }

        private static void HandleGamepadStickEvent(ImGuiIOPtr io, GamepadAxis axis, ImGuiKey negKey, ImGuiKey posKey)
        {
            const float deadZone = 0.20f;

            float axisValue = Raylib.GetGamepadAxisMovement(0, axis);

            io.AddKeyAnalogEvent(negKey, axisValue < -deadZone, axisValue < -deadZone ? -axisValue : 0);
            io.AddKeyAnalogEvent(posKey, axisValue > deadZone, axisValue > deadZone ? axisValue : 0);
        }

        /// <summary>
        /// Starts a new ImGui Frame
        /// </summary>
        /// <param name="dt">optional delta time, any value < 0 will use raylib GetFrameTime</param>
        public static void Begin(float dt = -1)
        {
            ImGui.SetCurrentContext(ImGuiContext);

            NewFrame(dt);
            FrameEvents();
            ImGui.NewFrame();
        }

        private static void EnableScissor(float x, float y, float width, float height)
        {
            Rlgl.EnableScissorTest();
            ImGuiIOPtr io = ImGui.GetIO();

            Vector2 scale = new Vector2(1.0f, 1.0f);
            if (Raylib.IsWindowState(ConfigFlags.HighDpiWindow) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                scale = io.DisplayFramebufferScale;

            Rlgl.Scissor((int)(x * scale.X),
                (int)((io.DisplaySize.Y - (int)(y + height)) * scale.Y),
                (int)(width * scale.X),
                (int)(height * scale.Y));
        }

        private static void TriangleVert(ImDrawVert idx_vert)
        {
            Vector4 color = ImGui.ColorConvertU32ToFloat4(idx_vert.Col);

            Rlgl.Color4f(color.X, color.Y, color.Z, color.W);
            Rlgl.TexCoord2f(idx_vert.Uv.X, idx_vert.Uv.Y);
            Rlgl.Vertex2f(idx_vert.Pos.X, idx_vert.Pos.Y);
        }

        private static void RenderTriangles(uint count, uint indexStart, ImVector<ushort> indexBuffer, ImVector<ImDrawVert> vertBuffer, ImTextureID texturePtr)
        {
            if (count < 3)
                return;

            uint textureId = 0;
            if (texturePtr != IntPtr.Zero)
                textureId = (uint)texturePtr.Handle;

            Rlgl.Begin(DrawMode.Triangles);
            Rlgl.SetTexture(textureId);

            for (int i = 0; i <= (count - 3); i += 3)
            {
                if (Rlgl.CheckRenderBatchLimit(3))
                {
                    Rlgl.Begin(DrawMode.Triangles);
                    Rlgl.SetTexture(textureId);
                }

                ushort indexA = indexBuffer[(int)indexStart + i];
                ushort indexB = indexBuffer[(int)indexStart + i + 1];
                ushort indexC = indexBuffer[(int)indexStart + i + 2];

                ImDrawVert vertexA = vertBuffer[indexA];
                ImDrawVert vertexB = vertBuffer[indexB];
                ImDrawVert vertexC = vertBuffer[indexC];

                TriangleVert(vertexA);
                TriangleVert(vertexB);
                TriangleVert(vertexC);
            }

            Rlgl.End();
        }

        private delegate void Callback(ImDrawListPtr list, ImDrawCmd cmd);

        private static unsafe void RenderData()
        {
            Rlgl.DrawRenderBatchActive();
            Rlgl.DisableBackfaceCulling();

            var data = ImGui.GetDrawData();

            for (int l = 0; l < data.CmdListsCount; l++)
            {
                ImDrawListPtr commandList = data.CmdLists[l];

                for (int cmdIndex = 0; cmdIndex < commandList.CmdBuffer.Size; cmdIndex++)
                {
                    var cmd = commandList.CmdBuffer[cmdIndex];

                    EnableScissor(cmd.ClipRect.X - data.DisplayPos.X, cmd.ClipRect.Y - data.DisplayPos.Y, cmd.ClipRect.Z - (cmd.ClipRect.X - data.DisplayPos.X), cmd.ClipRect.W - (cmd.ClipRect.Y - data.DisplayPos.Y));
                    if (cmd.UserCallback != IntPtr.Zero.ToPointer())
                    {
                        Callback cb = Marshal.GetDelegateForFunctionPointer<Callback>(new IntPtr(cmd.UserCallback));
                        cb(commandList, cmd);
                        continue;
                    }

                    RenderTriangles(cmd.ElemCount, cmd.IdxOffset, commandList.IdxBuffer, commandList.VtxBuffer, cmd.TextureId);

                    Rlgl.DrawRenderBatchActive();
                }
            }

            Rlgl.SetTexture(0);
            Rlgl.DisableScissorTest();
            Rlgl.EnableBackfaceCulling();
        }

        /// <summary>
        /// Ends an ImGui frame and submits all ImGui drawing to raylib for processing.
        /// </summary>
        public static void End()
        {
            ImGui.SetCurrentContext(ImGuiContext);
            ImGui.Render();
            RenderData();
        }

        /// <summary>
        /// Cleanup ImGui and unload font atlas
        /// </summary>
        public static void Shutdown()
        {
            Raylib.UnloadTexture(_fontTexture);
            ImGui.DestroyContext();
        }

        /// <summary>
        /// Draw a texture as an image in an ImGui Context
        /// Uses the current ImGui Cursor position and the full texture size.
        /// </summary>
        /// <param name="image">The raylib texture to draw</param>
        public static void Image(Texture2D image)
        {
            ImGui.Image(new ImTextureID(image.Id), new Vector2(image.Width, image.Height));
        }

        /// <summary>
        /// Draw a texture as an image in an ImGui Context at a specific size
        /// Uses the current ImGui Cursor position and the specified width and height
        /// The image will be scaled up or down to fit as needed
        /// </summary>
        /// <param name="image">The raylib texture to draw</param>
        /// <param name="width">The width of the drawn image</param>
        /// <param name="height">The height of the drawn image</param>
        public static void ImageSize(Texture2D image, int width, int height)
        {
            ImGui.Image(new ImTextureID(image.Id), new Vector2(width, height));
        }

        /// <summary>
        /// Draw a texture as an image in an ImGui Context at a specific size
        /// Uses the current ImGui Cursor position and the specified size
        /// The image will be scaled up or down to fit as needed
        /// </summary>
        /// <param name="image">The raylib texture to draw</param>
        /// <param name="size">The size of drawn image</param>
        public static void ImageSize(Texture2D image, Vector2 size)
        {
            ImGui.Image(new ImTextureID(image.Id), size);
        }

        /// <summary>
        /// Draw a portion texture as an image in an ImGui Context at a defined size
        /// Uses the current ImGui Cursor position and the specified size
        /// The image will be scaled up or down to fit as needed
        /// </summary>
        /// <param name="image">The raylib texture to draw</param>
        /// <param name="destWidth">The width of the drawn image</param>
        /// <param name="destHeight">The height of the drawn image</param>
        /// <param name="sourceRect">The portion of the texture to draw as an image. Negative values for the width and height will flip the image</param>
        public static void ImageRect(Texture2D image, float destWidth, float destHeight, Rectangle sourceRect)
        {
            Vector2 uv0 = new Vector2();
            Vector2 uv1 = new Vector2();

            if (sourceRect.Width < 0)
            {
                uv0.X = -((float)sourceRect.X / image.Width);
                uv1.X = (uv0.X - (float)(Math.Abs(sourceRect.Width) / image.Width));
            }
            else
            {
                uv0.X = (float)sourceRect.X / image.Width;
                uv1.X = uv0.X + (float)(sourceRect.Width / image.Width);
            }

            if (sourceRect.Height < 0)
            {
                uv0.Y = -((float)sourceRect.Y / image.Height);
                uv1.Y = (uv0.Y - (float)(Math.Abs(sourceRect.Height) / image.Height));
            }
            else
            {
                uv0.Y = (float)sourceRect.Y / image.Height;
                uv1.Y = uv0.Y + (float)(sourceRect.Height / image.Height);
            }

            ImGui.Image(new ImTextureID(image.Id), new Vector2(destWidth, destHeight), uv0, uv1);
        }

        /// <summary>
        /// Draws a render texture as an image an ImGui Context, automatically flipping the Y axis so it will show correctly on screen
        /// </summary>
        /// <param name="image">The render texture to draw</param>
        public static void ImageRenderTexture(RenderTexture2D image)
        {
            ImageRect(image.Texture, image.Texture.Width, image.Texture.Height, new Rectangle(0, 0, image.Texture.Width, -image.Texture.Height));
        }

        public static void ImageRenderTextureScale(RenderTexture2D image, float destWidth, float destHeight)
        {
            ImageRect(image.Texture, destWidth, destHeight, new Rectangle(0, 0, image.Texture.Width, -image.Texture.Height));
        }

        /// <summary>
        /// Draws a render texture as an image to the current ImGui Context, flipping the Y axis so it will show correctly on the screen
        /// The texture will be scaled to fit the content are available, centered if desired
        /// </summary>
        /// <param name="image">The render texture to draw</param>
        /// <param name="center">When true the texture will be centered in the content area. When false the image will be left and top justified</param>
        public static void ImageRenderTextureFit(RenderTexture2D image, bool center = true)
        {
            Vector2 area = ImGui.GetContentRegionAvail();

            float scale = area.X / image.Texture.Width;

            float y = image.Texture.Height * scale;
            if (y > area.Y)
            {
                scale = area.Y / image.Texture.Height;
            }

            int sizeX = (int)(image.Texture.Width * scale);
            int sizeY = (int)(image.Texture.Height * scale);

            if (center)
            {
                ImGui.SetCursorPosX(0);
                ImGui.SetCursorPosX(area.X / 2 - sizeX / 2);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (area.Y / 2 - sizeY / 2));
            }

            ImageRect(image.Texture, sizeX, sizeY, new Rectangle(0, 0, (image.Texture.Width), -(image.Texture.Height)));
        }

        /// <summary>
        /// Draws a texture as an image button in an ImGui context. Uses the current ImGui cursor position and the full size of the texture
        /// </summary>
        /// <param name="name">The display name and ImGui ID for the button</param>
        /// <param name="image">The texture to draw</param>
        /// <returns>True if the button was clicked</returns>
        public static bool ImageButton(string name, Texture2D image)
        {
            return ImageButtonSize(name, image, new Vector2(image.Width, image.Height));
        }

        /// <summary>
        /// Draws a texture as an image button in an ImGui context. Uses the current ImGui cursor position and the specified size.
        /// </summary>
        /// <param name="name">The display name and ImGui ID for the button</param>
        /// <param name="image">The texture to draw</param>
        /// <param name="size">The size of the button</param>
        /// <returns>True if the button was clicked</returns>
        public static bool ImageButtonSize(string name, Texture2D image, Vector2 size)
        {
            return ImGui.ImageButton(name, new ImTextureID(image.Id), size);
        }

        public static bool ImageButtonRect(string name, Texture2D image, Vector2 size, Rectangle sourceRect)
        {
            Vector2 uv0 = new Vector2();
            Vector2 uv1 = new Vector2();

            if (sourceRect.Width < 0)
            {
                uv0.X = -((float)sourceRect.X / image.Width);
                uv1.X = (uv0.X - (float)(Math.Abs(sourceRect.Width) / image.Width));
            }
            else
            {
                uv0.X = (float)sourceRect.X / image.Width;
                uv1.X = uv0.X + (float)(sourceRect.Width / image.Width);
            }

            if (sourceRect.Height < 0)
            {
                uv0.Y = -((float)sourceRect.Y / image.Height);
                uv1.Y = (uv0.Y - (float)(Math.Abs(sourceRect.Height) / image.Height));
            }
            else
            {
                uv0.Y = (float)sourceRect.Y / image.Height;
                uv1.Y = uv0.Y + (float)(sourceRect.Height / image.Height);
            }

            return ImGui.ImageButton(name, new ImTextureID(image.Id), size, uv0, uv1);
        }
    }
}