using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Aukronome.App.ViewModels;

namespace Aukronome.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var saved = _viewModel.LoadSessionSettings();
        RestoreWindowBoundsIfValid(saved.WindowLeft, saved.WindowTop, saved.WindowWidth, saved.WindowHeight);
        CompositionTarget.Rendering += OnCompositionTargetRendering;
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        CompositionTarget.Rendering -= OnCompositionTargetRendering;

        double? left = WindowState == WindowState.Normal ? Left : RestoreBounds.Left;
        double? top = WindowState == WindowState.Normal ? Top : RestoreBounds.Top;
        double? width = WindowState == WindowState.Normal ? Width : RestoreBounds.Width;
        double? height = WindowState == WindowState.Normal ? Height : RestoreBounds.Height;

        _viewModel.SaveSessionSettings(left, top, width, height);
        _viewModel.Dispose();
    }

    private void OnCompositionTargetRendering(object? sender, EventArgs e)
    {
        _viewModel.UpdateVisualSync();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Ctrl+S works everywhere in the window
        if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            _viewModel.SavePresetCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Do not hijack typing when a TextBox has focus
        if (Keyboard.FocusedElement is TextBox)
        {
            return;
        }

        bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

        switch (e.Key)
        {
            case Key.Space:
                _viewModel.TogglePlayCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Up:
                _viewModel.AdjustBpm(shift ? "10" : "1");
                e.Handled = true;
                break;

            case Key.Down:
                _viewModel.AdjustBpm(shift ? "-10" : "-1");
                e.Handled = true;
                break;

            case Key.T:
                if (Keyboard.Modifiers == ModifierKeys.None)
                {
                    _viewModel.TapTempoCommand.Execute(null);
                    e.Handled = true;
                }
                break;
        }
    }

    private void BpmTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Escape)
        {
            _viewModel.CommitBpmInputText();
            Keyboard.ClearFocus();
            Focus();
            e.Handled = true;
        }
    }

    private void BpmTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _viewModel.CommitBpmInputText();
    }

    private void NumeratorTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Escape)
        {
            _viewModel.CommitNumeratorInputText();
            Keyboard.ClearFocus();
            Focus();
            e.Handled = true;
        }
    }

    private void NumeratorTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _viewModel.CommitNumeratorInputText();
    }

    private void DenominatorTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Escape)
        {
            _viewModel.CommitDenominatorInputText();
            Keyboard.ClearFocus();
            Focus();
            e.Handled = true;
        }
    }

    private void DenominatorTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _viewModel.CommitDenominatorInputText();
    }

    private void TimeSignatureTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Escape)
        {
            _viewModel.CommitTimeSignatureInputText();
            Keyboard.ClearFocus();
            Focus();
            e.Handled = true;
        }
    }

    private void TimeSignatureTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _viewModel.CommitTimeSignatureInputText();
    }

    private void PresetNameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel.ConfirmPresetNameCommand.Execute(null);
            Keyboard.ClearFocus();
            Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            _viewModel.CancelPresetNameCommand.Execute(null);
            Keyboard.ClearFocus();
            Focus();
            e.Handled = true;
        }
    }

    private void RestoreWindowBoundsIfValid(double? left, double? top, double? width, double? height)
    {
        if (width is >= 500 && width <= SystemParameters.VirtualScreenWidth)
        {
            Width = width.Value;
        }

        if (height is >= 680 && height <= SystemParameters.VirtualScreenHeight)
        {
            Height = height.Value;
        }

        if (left.HasValue && top.HasValue)
        {
            double vLeft = SystemParameters.VirtualScreenLeft;
            double vTop = SystemParameters.VirtualScreenTop;
            double vRight = vLeft + SystemParameters.VirtualScreenWidth;
            double vBottom = vTop + SystemParameters.VirtualScreenHeight;

            if (left.Value >= vLeft - 50 && left.Value + 100 <= vRight &&
                top.Value >= vTop - 20 && top.Value + 100 <= vBottom)
            {
                Left = left.Value;
                Top = top.Value;
            }
        }
    }
}
