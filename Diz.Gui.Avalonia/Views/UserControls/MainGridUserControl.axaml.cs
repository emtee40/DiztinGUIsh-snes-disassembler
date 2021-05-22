﻿using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Diz.Gui.Avalonia.ViewModels;
using ReactiveUI;

namespace Diz.Gui.Avalonia.Views.UserControls
{
    public class MainGridUserControl : ReactiveUserControl<ByteEntriesViewModel>
    {
        public DataGrid MainGrid => this.FindControl<DataGrid>("MainGrid");
        
        public MainGridUserControl()
        {
            ViewModel = new ByteEntriesViewModel();

            this.WhenActivated(disposables =>
            {
                this.OneWayBind(ViewModel,
                    viewmodel => viewmodel.ByteEntries,
                    view => view.MainGrid.Items
                ).DisposeWith(disposables);

                this.WhenAnyValue(
                    x => x.MainGrid.SelectedItem)
                    .BindTo(this, x => x.ViewModel.SelectedItem);

                this.Bind(ViewModel, vm => vm.SelectedItem, v => v.MainGrid.SelectedItem);
                
                // this.BindCommand(ViewModel, vm=>vm.)
                
                // this.BindCommand(
                //     ViewModel,
                //     vm => vm.SetSelectedItem,
                //     v => v.MainGrid,
                //     nameof(MainGrid.CellEditEnded));
                
                // this.BindCommand(
                //     ViewModel,
                //     vm => vm.SetSelectedItem,
                //     v => v.MainGrid.SelectedItem,
                //     nameof(MainGrid.CellEditEnded));
                
//                this.BindCommand(ViewModel,
  //                  vm => vm.ByteEntries,
    //                view=>view.MainGrid.Items[0]
            });

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}