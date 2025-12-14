using FluentAssertions;
using Standup.Application.Interfaces;
using Standup.Application.Models;
using Standup.Application.ViewModels;
using Xunit;

namespace Standup.Application.Tests.ViewModels;

public class DocumentsViewModelTests
{
    [Fact]
    public async Task LoadDocumentsAsync_LoadsDocumentsAndSelectsFirst()
    {
        // Arrange
        var documents = new[]
        {
            new DocumentItem("/docs/doc1.md", "Document 1", "Category A"),
            new DocumentItem("/docs/doc2.md", "Document 2", "Category B"),
        };
        var documentService = new TestDocumentService(documents);
        var viewModel = new DocumentsViewModel(documentService);

        // Act
        await viewModel.LoadDocumentsCommand.ExecuteAsync(null);

        // Assert
        viewModel.Documents.Should().HaveCount(2);
        viewModel.SelectedDocument.Should().Be(documents[0]);
        viewModel.DocumentContent.Should().Be("Content of /docs/doc1.md");
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadDocumentsAsync_WithNoDocuments_DoesNotSetSelectedDocument()
    {
        // Arrange
        var documentService = new TestDocumentService(Array.Empty<DocumentItem>());
        var viewModel = new DocumentsViewModel(documentService);

        // Act
        await viewModel.LoadDocumentsCommand.ExecuteAsync(null);

        // Assert
        viewModel.Documents.Should().BeEmpty();
        viewModel.SelectedDocument.Should().BeNull();
        viewModel.DocumentContent.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadDocumentsAsync_SetsIsLoadingDuringOperation()
    {
        // Arrange
        var documents = new[]
        {
            new DocumentItem("/docs/doc1.md", "Document 1", "Category A"),
        };
        var documentService = new TestDocumentService(documents, delayMs: 50);
        var viewModel = new DocumentsViewModel(documentService);

        // Act
        var loadTask = viewModel.LoadDocumentsCommand.ExecuteAsync(null);

        // Assert - IsLoading should be true during operation
        await Task.Delay(10); // Give it time to start
        viewModel.IsLoading.Should().BeTrue();

        await loadTask;
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadDocumentsAsync_ClearsPreviousDocuments()
    {
        // Arrange
        var initialDocs = new[]
        {
            new DocumentItem("/docs/doc1.md", "Document 1", "Category A"),
            new DocumentItem("/docs/doc2.md", "Document 2", "Category B"),
        };
        var newDocs = new[]
        {
            new DocumentItem("/docs/doc3.md", "Document 3", "Category C"),
        };

        var documentService = new TestDocumentService(initialDocs);
        var viewModel = new DocumentsViewModel(documentService);

        // Load initial documents
        await viewModel.LoadDocumentsCommand.ExecuteAsync(null);
        viewModel.Documents.Should().HaveCount(2);

        // Change the service to return different documents
        documentService.SetDocuments(newDocs);

        // Act
        await viewModel.LoadDocumentsCommand.ExecuteAsync(null);

        // Assert
        viewModel.Documents.Should().HaveCount(1);
        viewModel.Documents[0].Title.Should().Be("Document 3");
    }

    [Fact]
    public async Task SelectDocumentAsync_UpdatesSelectedDocumentAndContent()
    {
        // Arrange
        var documents = new[]
        {
            new DocumentItem("/docs/doc1.md", "Document 1", "Category A"),
            new DocumentItem("/docs/doc2.md", "Document 2", "Category B"),
        };
        var documentService = new TestDocumentService(documents);
        var viewModel = new DocumentsViewModel(documentService);
        await viewModel.LoadDocumentsCommand.ExecuteAsync(null);

        // Act
        await viewModel.SelectDocumentCommand.ExecuteAsync(documents[1]);

        // Assert
        viewModel.SelectedDocument.Should().Be(documents[1]);
        viewModel.DocumentContent.Should().Be("Content of /docs/doc2.md");
    }

    [Fact]
    public async Task SelectDocumentAsync_LoadsContentFromService()
    {
        // Arrange
        var document = new DocumentItem("/custom/path.md", "Custom Doc", "Custom");
        var documentService = new TestDocumentService(new[] { document });
        var viewModel = new DocumentsViewModel(documentService);

        // Act
        await viewModel.SelectDocumentCommand.ExecuteAsync(document);

        // Assert
        viewModel.DocumentContent.Should().Be("Content of /custom/path.md");
        documentService.LastLoadedPath.Should().Be("/custom/path.md");
    }

    [Fact]
    public void InitialState_IsEmpty()
    {
        // Arrange
        var documentService = new TestDocumentService(Array.Empty<DocumentItem>());

        // Act
        var viewModel = new DocumentsViewModel(documentService);

        // Assert
        viewModel.Documents.Should().BeEmpty();
        viewModel.SelectedDocument.Should().BeNull();
        viewModel.DocumentContent.Should().BeEmpty();
        viewModel.IsLoading.Should().BeFalse();
    }

    private class TestDocumentService : IDocumentService
    {
        private readonly int _delayMs;
        private IEnumerable<DocumentItem> _documents;

        public string? LastLoadedPath { get; private set; }

        public TestDocumentService(IEnumerable<DocumentItem> documents, int delayMs = 0)
        {
            _documents = documents;
            _delayMs = delayMs;
        }

        public void SetDocuments(IEnumerable<DocumentItem> documents)
        {
            _documents = documents;
        }

        public async Task<IEnumerable<DocumentItem>> GetDocumentsAsync()
        {
            if (_delayMs > 0)
            {
                await Task.Delay(_delayMs);
            }

            return _documents.ToList();
        }

        public async Task<string> GetDocumentContentAsync(string path)
        {
            LastLoadedPath = path;
            if (_delayMs > 0)
            {
                await Task.Delay(_delayMs);
            }

            return $"Content of {path}";
        }
    }
}
