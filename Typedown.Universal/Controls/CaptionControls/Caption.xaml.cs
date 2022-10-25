﻿using System;
using System.Linq;
using System.Reactive;
using Typedown.Universal.Utilities;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Typedown.Universal.Controls
{
    public sealed partial class Caption : UserControl
    {
        public static DependencyProperty BackCommandProperty = DependencyProperty.Register(nameof(BackCommand), typeof(Command<Unit>), typeof(Caption), null);

        public Command<Unit> BackCommand { get => (Command<Unit>)GetValue(BackCommandProperty); set => SetValue(BackCommandProperty, value); }

        public Caption()
        {
            InitializeComponent();
            if (Environment.OSVersion.Version.Build >= 22000)
                CaptionWrapper.Children.Remove(CaptionWrapper.Children.OfType<CaptionButtons>().First());
        }
    }
}
