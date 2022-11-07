﻿using System;
using Typedown.Universal.Interfaces;
using Typedown.Universal.ViewModels;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Typedown.Universal.Controls.FloatControls
{
    public sealed partial class ToolTip : UserControl
    {
        private readonly Flyout flyout = new();

        private readonly AppViewModel viewModel;

        private readonly IMarkdownEditor markdownEditor;

        private readonly DispatcherTimer hideTimer = new();

        public ToolTip(AppViewModel viewModel, IMarkdownEditor markdownEditor)
        {
            this.viewModel = viewModel;
            this.markdownEditor = markdownEditor;
            hideTimer.Interval = TimeSpan.FromSeconds(3);
            hideTimer.Tick += OnHideTimerTick;
            flyout.Closed += OnFlyoutClosed;
            InitializeComponent();
        }

        public void Open(Rect rect, string text)
        {
            ContentTextBlock.Text = text;
            var options = new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Top, ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway };
            flyout.AreOpenCloseAnimationsEnabled = false;
            flyout.OverlayInputPassThroughElement = viewModel.XamlRoot.Content;
            flyout.FlyoutPresenterStyle = Resources["ToolTipFlyoutStyle"] as Style;
            flyout.Content = this;
            flyout.ShowAt(markdownEditor.GetDummyRectangle(rect), options);
            hideTimer.Start();
        }

        private void OnFlyoutClosed(object sender, object e)
        {
            hideTimer.Stop();
        }

        private void OnHideTimerTick(object sender, object e)
        {
            flyout.Hide();
            hideTimer.Stop();
        }

        public void Hide()
        {
            flyout.Hide();
        }
    }
}
