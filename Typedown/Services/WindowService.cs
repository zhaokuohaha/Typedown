﻿using System;
using Typedown.Controls;
using Typedown.Universal.Interfaces;
using Windows.UI.Xaml;
using Typedown.Utilities;
using System.Reactive.Subjects;
using Typedown.Windows;

namespace Typedown.Services
{
    public class WindowService : IWindowService
    {
        public Subject<nint> WindowStateChanged { get; } = new();

        public void RaiseWindowStateChanged(nint hWnd) => WindowStateChanged.OnNext(hWnd);

        public nint GetWindow(UIElement element) => AppWindow.GetWindow(element)?.Handle ?? default;
    }
}
