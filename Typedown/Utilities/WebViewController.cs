﻿using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Typedown.Universal.Interfaces;
using Typedown.Universal.Utilities;
using Windows.Devices.Input;
using Windows.System;
using Windows.UI.Input;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.Pointer;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Typedown.Utilities
{
    public class WebViewController: IWebViewController
    {
        public global::Windows.UI.Xaml.FrameworkElement Container { get; }

        private IntPtr ParentHWnd { get; }

        private float Scale { get; set; }

        private static Task<CoreWebView2Environment> coreWebView2EnvironmentTask;
        private static CoreWebView2Environment CoreWebView2Environment => coreWebView2EnvironmentTask.IsCompleted ? coreWebView2EnvironmentTask.Result : null;

        private Task<CoreWebView2CompositionController> coreWebView2CompositionControllerTask;
        private CoreWebView2CompositionController CoreWebView2CompositionController => coreWebView2CompositionControllerTask.IsCompleted ? coreWebView2CompositionControllerTask.Result : null;

        public CoreWebView2Controller CoreWebView2Controller { get; private set; }

        public CoreWebView2 CoreWebView2 => CoreWebView2Controller?.CoreWebView2;

        private global::Windows.UI.Composition.ContainerVisual WebViewVisual { get; set; }

        public event EventHandler CoreInitialized;

        public WebViewController(global::Windows.UI.Xaml.FrameworkElement container, IntPtr parentHWnd)
        {
            Container = container;
            ParentHWnd = parentHWnd;
            Initialize();
        }

        private async void Initialize()
        {
            await EnsureCreateCompositionController();
            await EnsureCreateController();
            InitializeEventHandler();
            UpdataScale();
            Observable.FromEventPattern(Container, nameof(Container.SizeChanged)).SubscribeWeak(_ => OnContainerSizeChanged());
            Observable.FromEventPattern(CoreWebView2CompositionController, nameof(CoreWebView2CompositionController.CursorChanged)).SubscribeWeak(_ => OnCursorChanged());
        }

        private static async Task EnsureCreateEnvironment()
        {
            if (coreWebView2EnvironmentTask == null)
            {
                var commandLineArgs = new List<string>() { "--disable-web-security" };
#if DEBUG
                commandLineArgs.Add("--remote-debugging-port=9222");
#endif
                var options = new CoreWebView2EnvironmentOptions(string.Join(" ", commandLineArgs));
                coreWebView2EnvironmentTask = CoreWebView2Environment.CreateAsync(null, null, options);
                await coreWebView2EnvironmentTask;
            }
            else
            {
                await coreWebView2EnvironmentTask;
            }
        }

        private async Task EnsureCreateCompositionController()
        {
            if (coreWebView2CompositionControllerTask == null)
            {
                var compositor = ElementCompositionPreview.GetElementVisual(Container).Compositor;
                WebViewVisual = compositor.CreateContainerVisual();
                WebViewVisual.RelativeSizeAdjustment = new(1, 1);
                ElementCompositionPreview.SetElementChildVisual(Container, WebViewVisual);
                await EnsureCreateEnvironment();
                coreWebView2CompositionControllerTask = CoreWebView2Environment.CreateCoreWebView2CompositionControllerAsync(ParentHWnd);
                await coreWebView2CompositionControllerTask;
                CoreWebView2CompositionController.RootVisualTarget = WebViewVisual;
            }
            else
            {
                await coreWebView2CompositionControllerTask;
            }
        }

        public async Task<bool> EnsureCreateController()
        {
            if (CoreWebView2Controller == null)
            {
                await coreWebView2CompositionControllerTask;
                var raw = typeof(CoreWebView2CompositionController).GetField("_rawNative", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(CoreWebView2CompositionController);
                CoreWebView2Controller = typeof(CoreWebView2Controller).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(object) }, null).Invoke(new object[] { raw }) as CoreWebView2Controller;
                CoreWebView2Controller.DefaultBackgroundColor = System.Drawing.Color.Transparent;
                CoreInitialized?.Invoke(this, EventArgs.Empty);
            }
            return CoreWebView2Controller != null;
        }

        private void OnContainerSizeChanged()
        {
            UpdateBounds();
        }

        private void InitializeEventHandler()
        {
            Observable.FromEventPattern(Container, nameof(Container.PointerMoved)).SubscribeWeak(x => OnPointerMoved(x.EventArgs as PointerRoutedEventArgs));
            Observable.FromEventPattern(Container, nameof(Container.PointerPressed)).SubscribeWeak(x => OnPointerPressed(x.EventArgs as PointerRoutedEventArgs));
            Observable.FromEventPattern(Container, nameof(Container.PointerReleased)).SubscribeWeak(x => OnPointerReleased(x.EventArgs as PointerRoutedEventArgs));
            Observable.FromEventPattern(Container, nameof(Container.PointerWheelChanged)).SubscribeWeak(x => OnPointerWheelChanged(x.EventArgs as PointerRoutedEventArgs));
            Observable.FromEventPattern(Container, nameof(Container.PointerExited)).SubscribeWeak(x => OnPointerExited(x.EventArgs as PointerRoutedEventArgs));
        }

        private void OnPointerMoved(PointerRoutedEventArgs args)
        {
            var deviceType = args.Pointer.PointerDeviceType;
            var message = deviceType == PointerDeviceType.Mouse ? 0x0200u : 0x0245u;
            OnXamlPointerMessage(message, args);
        }

        private bool hasMouseCapture;
        private bool hasPenCapture;
        private Dictionary<uint, bool> hasTouchCapture = new();
        private bool isLeftMouseButtonPressed;
        private bool isMiddleMouseButtonPressed;
        private bool isRightMouseButtonPressed;
        private bool isXButton1Pressed;
        private bool isXButton2Pressed;

        protected virtual void OnPointerPressed(PointerRoutedEventArgs args)
        {
            UpdateBounds();
            var deviceType = args.Pointer.PointerDeviceType;
            var pointerPoint = args.GetCurrentPoint(Container);
            uint message = 0;
            if (deviceType == PointerDeviceType.Mouse)
            {
                var properties = pointerPoint.Properties;
                hasMouseCapture = Container.CapturePointer(args.Pointer);
                if (properties.IsLeftButtonPressed)
                {
                    message = PInvoke.WM_LBUTTONDOWN;
                    isLeftMouseButtonPressed = true;
                }
                else if (properties.IsMiddleButtonPressed)
                {
                    message = PInvoke.WM_MBUTTONDOWN;
                    isMiddleMouseButtonPressed = true;
                }
                else if (properties.IsRightButtonPressed)
                {
                    message = PInvoke.WM_RBUTTONDOWN;
                    isRightMouseButtonPressed = true;
                }
                else if (properties.IsXButton1Pressed)
                {
                    message = PInvoke.WM_XBUTTONDOWN;
                    isXButton1Pressed = true;
                }
                else if (properties.IsXButton2Pressed)
                {
                    message = PInvoke.WM_XBUTTONDOWN;
                    isXButton2Pressed = true;
                }
            }
            else if (deviceType == PointerDeviceType.Touch)
            {
                message = PInvoke.WM_POINTERDOWN;
                hasTouchCapture.Add(pointerPoint.PointerId, Container.CapturePointer(args.Pointer));
            }
            else if (deviceType == PointerDeviceType.Pen)
            {
                message = PInvoke.WM_POINTERDOWN; // WM_POINTERDOWN
                hasPenCapture = Container.CapturePointer(args.Pointer);
            }
            if (message != 0)
                OnXamlPointerMessage(message, args);
        }

        protected virtual void OnPointerReleased(PointerRoutedEventArgs args)
        {
            var deviceType = args.Pointer.PointerDeviceType;
            var pointerPoint = args.GetCurrentPoint(Container);
            uint message = 0;
            if (deviceType == PointerDeviceType.Mouse)
            {
                if (isLeftMouseButtonPressed)
                {
                    message = PInvoke.WM_LBUTTONUP;
                    isLeftMouseButtonPressed = false;
                }
                else if (isMiddleMouseButtonPressed)
                {
                    message = PInvoke.WM_MBUTTONUP;
                    isMiddleMouseButtonPressed = false;
                }
                else if (isRightMouseButtonPressed)
                {
                    message = PInvoke.WM_RBUTTONUP;
                    isRightMouseButtonPressed = false;
                }
                else if (isXButton1Pressed)
                {
                    message = PInvoke.WM_XBUTTONUP;
                    isXButton1Pressed = false;
                }
                else if (isXButton2Pressed)
                {
                    message = PInvoke.WM_XBUTTONUP;
                    isXButton2Pressed = false;
                }
                if (hasMouseCapture)
                {
                    Container.ReleasePointerCapture(args.Pointer);
                    hasMouseCapture = false;
                }
            }
            else
            {
                if (hasTouchCapture.Keys.Contains(pointerPoint.PointerId))
                {
                    Container.ReleasePointerCapture(args.Pointer);
                    hasTouchCapture.Remove(pointerPoint.PointerId);
                }
                if (hasPenCapture)
                {
                    Container.ReleasePointerCapture(args.Pointer);
                    hasPenCapture = false;
                }
                message = PInvoke.WM_POINTERUP; // WM_POINTERUP
            }
            if (message != 0)
                OnXamlPointerMessage(message, args);
        }

        protected virtual void OnPointerWheelChanged(PointerRoutedEventArgs args)
        {
            var deviceType = args.Pointer.PointerDeviceType;
            var message = deviceType == PointerDeviceType.Mouse ? 0x020Au : 0x024Eu;
            OnXamlPointerMessage(message, args);
        }

        protected virtual void OnPointerExited(PointerRoutedEventArgs args)
        {
            global::Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerCursor = new global::Windows.UI.Core.CoreCursor(0, 0);
            if (args.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                OnXamlPointerMessage(PInvoke.WM_MOUSELEAVE, args);
                if (!hasMouseCapture) ResetMouseInputState();
            }
            else
            {
                OnXamlPointerMessage(PInvoke.WM_POINTERLEAVE, args); // WM_POINTERLEAVE
            }
        }

        public void UpdateBounds()
        {
            if (CoreWebView2Controller != null)
                CoreWebView2Controller.Bounds = new(new(0, 0), new((int)(Container.ActualWidth * Scale), (int)(Container.ActualHeight * Scale)));
        }

        public void UpdataScale()
        {
            Scale = PInvoke.GetDpiForWindow(new(ParentHWnd)) / 96f;
            if (WebViewVisual != null)
                WebViewVisual.Scale = new(new(1 / Scale), 1);
            UpdateBounds();
        }

        private void ResetMouseInputState()
        {
            isLeftMouseButtonPressed = false;
            isMiddleMouseButtonPressed = false;
            isRightMouseButtonPressed = false;
            isXButton1Pressed = false;
            isXButton2Pressed = false;
        }

        private void OnXamlPointerMessage(uint message, PointerRoutedEventArgs args)
        {
            if (CoreWebView2Controller == null)
                return;
            args.Handled = true;
            var logicalPointerPoint = args.GetCurrentPoint(Container);
            var logicalPoint = logicalPointerPoint.Position;
            var physicalPoint = new System.Drawing.Point((int)(logicalPoint.X * Scale), (int)(logicalPoint.Y * Scale));
            var deviceType = args.Pointer.PointerDeviceType;
            if (deviceType == PointerDeviceType.Mouse)
            {
                if (message == PInvoke.WM_MOUSELEAVE)
                {
                    CoreWebView2CompositionController.SendMouseInput((CoreWebView2MouseEventKind)message, 0, 0, new(0, 0));
                }
                else
                {
                    uint mouse_data = 0;
                    if (message == PInvoke.WM_MOUSEWHEEL || message == PInvoke.WM_MOUSEHWHEEL)
                    {
                        mouse_data = (uint)logicalPointerPoint.Properties.MouseWheelDelta;
                    }
                    if (message == PInvoke.WM_XBUTTONDOWN || message == PInvoke.WM_XBUTTONUP || message == PInvoke.WM_XBUTTONDBLCLK)
                    {
                        var pointerUpdateKind = logicalPointerPoint.Properties.PointerUpdateKind;
                        if (pointerUpdateKind == PointerUpdateKind.XButton1Pressed || pointerUpdateKind == PointerUpdateKind.XButton1Released)
                            mouse_data |= 0x0001;
                        if (pointerUpdateKind == PointerUpdateKind.XButton2Pressed || pointerUpdateKind == PointerUpdateKind.XButton2Released)
                            mouse_data |= 0x0002;
                    }
                    CoreWebView2CompositionController.SendMouseInput((CoreWebView2MouseEventKind)message, GetKeyModifiers(args), mouse_data, physicalPoint);
                }
            }
            else
            {
                var inputPt = args.GetCurrentPoint(Container);
                var outputPt = CoreWebView2Environment.CreateCoreWebView2PointerInfo();
                FillPointerInfo(inputPt, outputPt, args);
                CoreWebView2CompositionController.SendPointerInput((CoreWebView2PointerEventKind)message, outputPt);
            }
        }

        private static readonly Dictionary<HCURSOR, global::Windows.UI.Core.CoreCursorType> coreCursorTypeDic = new()
        {
            {PInvoke.LoadCursor(HINSTANCE.Null, PInvoke.IDC_ARROW),  global::Windows.UI.Core.CoreCursorType.Arrow},
            {PInvoke.LoadCursor(HINSTANCE.Null, PInvoke.IDC_CROSS),  global::Windows.UI.Core.CoreCursorType.Cross},
            {PInvoke.LoadCursor(HINSTANCE.Null, PInvoke.IDC_HAND),  global::Windows.UI.Core.CoreCursorType.Hand},
            {PInvoke.LoadCursor(HINSTANCE.Null, PInvoke.IDC_HELP),  global::Windows.UI.Core.CoreCursorType.Help},
            {PInvoke.LoadCursor(HINSTANCE.Null, PInvoke.IDC_IBEAM),  global::Windows.UI.Core.CoreCursorType.IBeam},
            {PInvoke.LoadCursor(HINSTANCE.Null, PInvoke.IDC_SIZEALL),  global::Windows.UI.Core.CoreCursorType.SizeAll},
            {PInvoke.LoadCursor(HINSTANCE.Null, PInvoke.IDC_SIZENESW),  global::Windows.UI.Core.CoreCursorType.SizeNortheastSouthwest},
            {PInvoke.LoadCursor(HINSTANCE.Null, PInvoke.IDC_SIZENS),  global::Windows.UI.Core.CoreCursorType.SizeNorthSouth},
            {PInvoke.LoadCursor(HINSTANCE.Null, PInvoke.IDC_SIZENWSE),  global::Windows.UI.Core.CoreCursorType.SizeNorthwestSoutheast},
            {PInvoke.LoadCursor(HINSTANCE.Null, PInvoke.IDC_SIZEWE),  global::Windows.UI.Core.CoreCursorType.SizeWestEast},
            {PInvoke.LoadCursor(HINSTANCE.Null, PInvoke.IDC_NO),  global::Windows.UI.Core.CoreCursorType.UniversalNo},
            {PInvoke.LoadCursor(HINSTANCE.Null, PInvoke.IDC_UPARROW),  global::Windows.UI.Core.CoreCursorType.UpArrow},
            {PInvoke.LoadCursor(HINSTANCE.Null, PInvoke.IDC_WAIT),  global::Windows.UI.Core.CoreCursorType.Wait},
            {PInvoke.LoadCursor(HINSTANCE.Null, PInvoke.IDC_PIN),  global::Windows.UI.Core.CoreCursorType.Pin},
            {PInvoke.LoadCursor(HINSTANCE.Null, PInvoke.IDC_PERSON),  global::Windows.UI.Core.CoreCursorType.Person},
        };

        private void OnCursorChanged()
        {
            if (coreCursorTypeDic.TryGetValue(new(CoreWebView2CompositionController.Cursor), out var cursor))
                global::Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerCursor = new global::Windows.UI.Core.CoreCursor(cursor, 0);
            else
                global::Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerCursor = new global::Windows.UI.Core.CoreCursor(0, 0);
        }

        private CoreWebView2MouseEventVirtualKeys GetKeyModifiers(PointerRoutedEventArgs args)
        {
            var properties = args.GetCurrentPoint(Container).Properties;
            var modifiers = CoreWebView2MouseEventVirtualKeys.None;
            if (args.KeyModifiers.HasFlag(VirtualKeyModifiers.Control))
                modifiers |= CoreWebView2MouseEventVirtualKeys.Control;
            if (args.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift))
                modifiers |= CoreWebView2MouseEventVirtualKeys.Shift;
            if (properties.IsLeftButtonPressed)
                modifiers |= CoreWebView2MouseEventVirtualKeys.LeftButton;
            if (properties.IsRightButtonPressed)
                modifiers |= CoreWebView2MouseEventVirtualKeys.RightButton;
            if (properties.IsMiddleButtonPressed)
                modifiers |= CoreWebView2MouseEventVirtualKeys.MiddleButton;
            if (properties.IsXButton1Pressed)
                modifiers |= CoreWebView2MouseEventVirtualKeys.XButton1;
            if (properties.IsXButton2Pressed)
                modifiers |= CoreWebView2MouseEventVirtualKeys.XButton2;
            return modifiers;
        }

        private void FillPointerPenInfo(PointerPoint inputPt, CoreWebView2PointerInfo outputPt)
        {
            
            var inputProperties = inputPt.Properties;
            var outputPt_penFlags = PInvoke.PEN_FLAG_NONE;
            if (inputProperties.IsBarrelButtonPressed)
                outputPt_penFlags |= PInvoke.PEN_FLAG_BARREL;
            if (inputProperties.IsInverted)
                outputPt_penFlags |= PInvoke.PEN_FLAG_INVERTED;
            if (inputProperties.IsEraser)
                outputPt_penFlags |= PInvoke.PEN_FLAG_ERASER;
            outputPt.PenFlags = (uint)outputPt_penFlags;
            outputPt.PenMask = (uint)(PInvoke.PEN_MASK_PRESSURE | PInvoke.PEN_MASK_ROTATION | PInvoke.PEN_MASK_TILT_X | PInvoke.PEN_MASK_TILT_Y);
            outputPt.PenPressure = (uint)(inputProperties.Pressure * 1024);
            outputPt.PenRotation = (uint)inputProperties.Twist;
            outputPt.PenTiltX = (int)inputProperties.XTilt;
            outputPt.PenTiltY = (int)inputProperties.YTilt;
        }

        private void FillPointerTouchInfo(PointerPoint inputPt, CoreWebView2PointerInfo outputPt)
        {
            var inputProperties = inputPt.Properties;
            outputPt.TouchFlags = 0;
            outputPt.TouchMask = (uint)(PInvoke.TOUCH_MASK_CONTACTAREA | PInvoke.TOUCH_MASK_ORIENTATION | PInvoke.TOUCH_MASK_PRESSURE);
            var width = inputProperties.ContactRect.Width * Scale;
            var height = inputProperties.ContactRect.Height * Scale;
            var leftVal = inputProperties.ContactRect.X * Scale;
            var topVal = inputProperties.ContactRect.Y * Scale;
            outputPt.TouchContact = new System.Drawing.Rectangle((int)leftVal, (int)topVal, (int)width, (int)height);
            var widthRaw = inputProperties.ContactRectRaw.Width * Scale;
            var heightRaw = inputProperties.ContactRectRaw.Height * Scale;
            var leftValRaw = inputProperties.ContactRectRaw.X * Scale;
            var topValRaw = inputProperties.ContactRectRaw.Y * Scale;
            outputPt.TouchContactRaw = new System.Drawing.Rectangle((int)leftValRaw, (int)topValRaw, (int)widthRaw, (int)heightRaw);
            outputPt.TouchOrientation = (uint)inputProperties.Orientation;
            outputPt.TouchPressure = (uint)(inputProperties.Pressure * 1024);
        }

        private void FillPointerInfo(PointerPoint inputPt, CoreWebView2PointerInfo outputPt, PointerRoutedEventArgs args)
        {
            PointerPointProperties inputProperties = inputPt.Properties;
            var deviceType = inputPt.PointerDevice.PointerDeviceType;
            if (deviceType == PointerDeviceType.Pen)
                outputPt.PointerKind = (uint)POINTER_INPUT_TYPE.PT_MOUSE;
            else if (deviceType == PointerDeviceType.Touch)
                outputPt.PointerKind = (uint)POINTER_INPUT_TYPE.PT_MOUSE;
            outputPt.PointerId = args.Pointer.PointerId;
            outputPt.FrameId = inputPt.FrameId;
            var outputPt_pointerFlags = POINTER_FLAGS.POINTER_FLAG_NONE;
            if (inputProperties.IsInRange)
                outputPt_pointerFlags |= POINTER_FLAGS.POINTER_FLAG_INRANGE;
            if (deviceType == PointerDeviceType.Touch)
            {
                FillPointerTouchInfo(inputPt, outputPt);
                if (inputPt.IsInContact)
                {
                    outputPt_pointerFlags |= POINTER_FLAGS.POINTER_FLAG_INCONTACT;
                    outputPt_pointerFlags |= POINTER_FLAGS.POINTER_FLAG_FIRSTBUTTON;
                }
                if (inputProperties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
                {
                    outputPt_pointerFlags |= POINTER_FLAGS.POINTER_FLAG_NEW;
                }
            }
            if (deviceType == PointerDeviceType.Pen)
            {
                FillPointerPenInfo(inputPt, outputPt);
                if (inputPt.IsInContact)
                {
                    outputPt_pointerFlags |= POINTER_FLAGS.POINTER_FLAG_INCONTACT;
                    if (!inputProperties.IsBarrelButtonPressed)
                        outputPt_pointerFlags |= POINTER_FLAGS.POINTER_FLAG_FIRSTBUTTON;
                    else
                        outputPt_pointerFlags |= POINTER_FLAGS.POINTER_FLAG_SECONDBUTTON;
                }
            }
            
            if (inputProperties.IsPrimary)
                outputPt_pointerFlags |= POINTER_FLAGS.POINTER_FLAG_PRIMARY;
            if (inputProperties.TouchConfidence)
                outputPt_pointerFlags |= POINTER_FLAGS.POINTER_FLAG_CONFIDENCE;
            if (inputProperties.IsCanceled)
                outputPt_pointerFlags |= POINTER_FLAGS.POINTER_FLAG_CANCELED;
            if (inputProperties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
                outputPt_pointerFlags |= POINTER_FLAGS.POINTER_FLAG_DOWN;
            if (inputProperties.PointerUpdateKind == PointerUpdateKind.Other)
                outputPt_pointerFlags |= POINTER_FLAGS.POINTER_FLAG_UPDATE;
            if (inputProperties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
                outputPt_pointerFlags |= POINTER_FLAGS.POINTER_FLAG_UP;
            outputPt.PointerFlags = (uint)outputPt_pointerFlags;
            var outputPt_pointerPixelLocation = new System.Drawing.Point((int)(inputPt.Position.X * Scale), (int)(inputPt.Position.Y * Scale));
            outputPt.PixelLocation = outputPt_pointerPixelLocation;
            var outputPt_pointerRawPixelLocation = new System.Drawing.Point((int)(inputPt.RawPosition.X * Scale), (int)(inputPt.RawPosition.Y * Scale));
            outputPt.PixelLocationRaw = outputPt_pointerRawPixelLocation;
            var outputPoint_pointerTime = inputPt.Timestamp / 1000;
            outputPt.Time = (uint)outputPoint_pointerTime;
            var outputPoint_pointerHistoryCount = (uint)args.GetIntermediatePoints(Container).Count;
            outputPt.HistoryCount = outputPoint_pointerHistoryCount;
            if (PInvoke.QueryPerformanceFrequency(out var lpFrequency))
            {
                var scale = 1000000ul;
                var frequency = (ulong)lpFrequency;
                var outputPoint_pointerPerformanceCount = (inputPt.Timestamp * frequency) / scale;
                outputPt.PerformanceCount = outputPoint_pointerPerformanceCount;
            }
            outputPt.ButtonChangeKind = (int)inputProperties.PointerUpdateKind;
        }
    }
}
