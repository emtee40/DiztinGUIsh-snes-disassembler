using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Diz.Core.model;
using Diz.Gui.Avalonia.UserControls.UserControls;
using Diz.Gui.Avalonia.ViewModels;
using ReactiveUI;

namespace Diz.Gui.Avalonia.Views.Windows
{
    public class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        private Data data;

        public MainWindow() : this(null)
        {
        } 
        
        public MainWindow(Data data)
        {
#if DEBUG
        this.AttachDevTools();
#endif
            this.data = data;
            
            ViewModel = new MainWindowViewModel(data);

            this
                .WhenActivated(
                    disposableRegistration =>
                    {
                        this.Bind(ViewModel, 
                                vm => vm.LabelsViewModel,
                                view => view.LabelsListUserControl.ViewModel
                            )
                            .DisposeWith(disposableRegistration);
                    });

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public LabelsListUserControl LabelsListUserControl => this.FindControl<LabelsListUserControl>("LabelsListUserControl");
    }
}