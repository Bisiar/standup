using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Standup.Application.Interfaces;
using Standup.Application.Models;

namespace Standup.Application.ViewModels;

public partial class DocumentsViewModel : ObservableObject
{
    private readonly IDocumentService _documentService;

    [ObservableProperty]
    private ObservableCollection<DocumentItem> _documents = new();

    [ObservableProperty]
    private DocumentItem? _selectedDocument;

    [ObservableProperty]
    private string _documentContent = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public DocumentsViewModel(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [RelayCommand]
    private async Task LoadDocumentsAsync()
    {
        IsLoading = true;
        try
        {
            var docs = await _documentService.GetDocumentsAsync();
            Documents.Clear();
            foreach (var doc in docs)
            {
                Documents.Add(doc);
            }

            if (Documents.Any())
            {
                await SelectDocumentAsync(Documents.First());
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SelectDocumentAsync(DocumentItem document)
    {
        SelectedDocument = document;
        DocumentContent = await _documentService.GetDocumentContentAsync(document.Path);
    }
}
