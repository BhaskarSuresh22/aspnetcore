// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.QuickGrid.Tests;

public class QuickGridColumnVirtualizeTest
{
    [Fact]
    public async Task ColumnVirtualize_PaginationConflict_ThrowsException()
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IJSRuntime>(NoOpJsRuntime.Instance)
            .AddSingleton<NavigationManager, TestNavigationManager>()
            .BuildServiceProvider();

        var testRenderer = new TestRenderer(serviceProvider);

        var testComponent = new ColumnVirtualizePaginationConflictComponent();

        var componentId = testRenderer.AssignRootComponentId(testComponent);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await testRenderer.RenderRootComponentAsync(componentId));

        Assert.Contains(nameof(QuickGrid<int>.Pagination), ex.Message);
        Assert.Contains(nameof(QuickGrid<int>.VirtualizeColumns), ex.Message);
    }

    private static RenderFragment BuildSingleColumnChildContent<TItem, TProp>(
        Expression<Func<TItem, TProp>> property)
        => builder =>
        {
            builder.OpenComponent<PropertyColumn<TItem, TProp>>(0);
            builder.AddAttribute(1, "Property", property);
            builder.CloseComponent();
        };

    private class Person
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private abstract class GridHostComponentBase : ComponentBase
    {
        public QuickGrid<Person> Grid { get; protected set; } = default!;
    }

    // Configures QuickGrid with both Pagination and VirtualizeColumns enabled — an invalid
    // combination that must throw during OnParametersSetAsync.
    private class ColumnVirtualizePaginationConflictComponent : GridHostComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<QuickGrid<Person>>(0);
            builder.AddAttribute(1, "Items", new List<Person>().AsQueryable());
            builder.AddAttribute(2, "Pagination", new PaginationState { ItemsPerPage = 10 });
            builder.AddAttribute(3, "VirtualizeColumns", true);
            builder.AddAttribute(4, "ColumnSize", 150f);
            builder.AddAttribute(5, "ChildContent",
                BuildSingleColumnChildContent<Person, int>(p => p.Id));
            builder.AddComponentReferenceCapture(6, c => Grid = (QuickGrid<Person>)c);
            builder.CloseComponent();
        }
    }

    private class TestNavigationManager : NavigationManager, IHostEnvironmentNavigationManager
    {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/");

        void IHostEnvironmentNavigationManager.Initialize(string baseUri, string uri) => Initialize(baseUri, uri);

        protected override void NavigateToCore(string uri, bool forceLoad) => Uri = uri;
    }

    /// <summary>
    /// A no-op IJSRuntime used for tests that do not expect to reach the JS-init phase
    /// (the throw happens earlier in OnParametersSetAsync, before _jsModule is imported).
    /// </summary>
    private sealed class NoOpJsRuntime : IJSRuntime
    {
        public static readonly NoOpJsRuntime Instance = new();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args = null)
            => throw new InvalidOperationException(
                $"Unexpected JS invocation in NoOpJsRuntime: {identifier}");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => throw new InvalidOperationException(
                $"Unexpected JS invocation in NoOpJsRuntime: {identifier}");
    }
}
