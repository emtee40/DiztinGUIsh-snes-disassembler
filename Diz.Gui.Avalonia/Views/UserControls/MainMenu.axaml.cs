﻿using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Diz.Gui.Avalonia.Views.UserControls
{
    public class MainMenu : UserControl
    {
        public MainMenu()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}