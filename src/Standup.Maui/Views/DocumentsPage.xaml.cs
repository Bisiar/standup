using Standup.Application.ViewModels;

namespace Standup.Maui.Views;

public partial class DocumentsPage : ContentPage
{
    public DocumentsPage(DocumentsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is DocumentsViewModel vm)
        {
            await vm.LoadDocumentsCommand.ExecuteAsync(null);
        }
    }
}
