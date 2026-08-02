using RemnantOverseer.Models.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RemnantOverseer.Services;
using RemnantOverseer.Utilities;
using System.Collections.ObjectModel;
using RemnantOverseer.Models;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace RemnantOverseer.ViewModels;
public partial class MissingItemsViewModel : ViewModelBase
{
    private readonly SaveDataService _saveDataService;
    private MappedMissingItems _mappedMissingItems = new();
    private readonly Subject<string?> _filterTextSubject = new Subject<string?>();

    private int _selectedCharacterIndex = -1;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string? _filterText = null;

    [ObservableProperty]
    private ObservableCollection<MissingItemGroup> _filteredItemGroups = [];

    [ObservableProperty]
    private bool _isGlobalExpandOn = true;

    [ObservableProperty]
    private bool _isGroupedByWorld;

    [ObservableProperty]
    private bool _isNerudFilterChecked;

    [ObservableProperty]
    private bool _isYaeshaFilterChecked;

    [ObservableProperty]
    private bool _isLosomnFilterChecked;

    public MissingItemsViewModel(SaveDataService saveDataService)
    {
        _saveDataService = saveDataService;
        _filterTextSubject
          .Throttle(TimeSpan.FromMilliseconds(400))
          .Subscribe(OnFilterTextChangedDebounced);

        // Set the flag until after onLoaded is executed
        IsLoading = true;
    }

    public void OnViewLoaded()
    {
        if (IsInitialized) { return; }

        Task.Run(async () => { await ReadSave(true); IsActive = true; IsInitialized = true; });
    }

    [RelayCommand]
    private void ExpandTreeNodes()
    {
        IsGlobalExpandOn = !IsGlobalExpandOn;
    }

    [RelayCommand]
    private void NerudFilterToggled()
    {
        if (IsNerudFilterChecked)
        {
            IsYaeshaFilterChecked = false;
            IsLosomnFilterChecked = false;
        }

        ApplyFilter();
    }

    [RelayCommand]
    private void YaeshaFilterToggled()
    {
        if (IsYaeshaFilterChecked)
        {
            IsNerudFilterChecked = false;
            IsLosomnFilterChecked = false;
        }

        ApplyFilter();
    }

    [RelayCommand]
    private void LosomnFilterToggled()
    {
        if (IsLosomnFilterChecked)
        {
            IsNerudFilterChecked = false;
            IsYaeshaFilterChecked = false;
        }

        ApplyFilter();
    }

    partial void OnFilterTextChanged(string? value)
    {
        _filterTextSubject.OnNext(value);
    }

    partial void OnIsGroupedByWorldChanged(bool value)
    {
        ApplyFilter();
    }

    private void OnFilterTextChangedDebounced(string? value)
    {
        ApplyFilter(value);
    }

    [RelayCommand]
    public void ResetFilters()
    {
        var hasWorldFilter = IsNerudFilterChecked
            || IsYaeshaFilterChecked
            || IsLosomnFilterChecked;
        ResetWorldFilterToggles();

        if (FilterText is null && hasWorldFilter)
        {
            ApplyFilter();
        }

        FilterText = null;
    }

    private async Task ReadSave(bool resetActiveCahracter = false)
    {
        IsLoading = true;

        var dataset = await _saveDataService.GetSaveData();
        if (dataset == null)
        {
            IsLoading = false;
            return;
        }

        if (resetActiveCahracter)
        {
            _selectedCharacterIndex = DatasetMapper.GetActiveCharacterIndex(dataset);
            // Call private field to avoid filtering on every assignment
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
            _filterText = null;
            _isNerudFilterChecked = false;
            _isYaeshaFilterChecked = false;
            _isLosomnFilterChecked = false;
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
            OnPropertyChanged(nameof(FilterText));
            OnPropertyChanged(nameof(IsNerudFilterChecked));
            OnPropertyChanged(nameof(IsYaeshaFilterChecked));
            OnPropertyChanged(nameof(IsLosomnFilterChecked));
        }

        _mappedMissingItems = DatasetMapper.MapMissingItems(dataset.Characters[_selectedCharacterIndex].Profile.MissingItems);

        ApplyFilter();

        IsLoading = false;
    }

    private async Task CharacterUpdatedHandler(int characerIndex)
    {
        _selectedCharacterIndex = characerIndex;
        await ReadSave(false);
    }

    private void ApplyFilter()
    {
        ApplyFilter(FilterText);
    }

    private void ApplyFilter(string? value)
    {
        var items = _mappedMissingItems.ItemCategoryList
            .Where(category => category.Type != Models.Enums.ItemTypes.Unknown)
            .SelectMany(category => category.Items);

        if (IsNerudFilterChecked)
        {
            items = items.Where(item => item.CanonicalWorldName == "N'Erud");
        }
        else if (IsYaeshaFilterChecked)
        {
            items = items.Where(item => item.CanonicalWorldName == "Yaesha");
        }
        else if (IsLosomnFilterChecked)
        {
            items = items.Where(item => item.CanonicalWorldName == "Losomn");
        }

        if (!string.IsNullOrEmpty(value))
        {
            items = items.Where(item =>
                item.Name.Contains(value, StringComparison.OrdinalIgnoreCase)
                || item.WorldName.Contains(value, StringComparison.OrdinalIgnoreCase)
                || item.AcquisitionSourceName.Contains(value, StringComparison.OrdinalIgnoreCase));
        }

        var filteredItems = items.ToList();
        var groups = IsGroupedByWorld
            ? GroupItemsByWorld(filteredItems)
            : GroupItemsByType(filteredItems);

        FilteredItemGroups = new(groups);
    }

    private static List<MissingItemGroup> GroupItemsByType(List<Item> items)
    {
        return items
            .GroupBy(item => item.Type)
            .OrderBy(group => group.Key)
            .Select(group => new MissingItemGroup
            {
                Type = group.Key,
                IsItemTypeGroup = true,
                Items = group.ToList()
            })
            .ToList();
    }

    private static List<MissingItemGroup> GroupItemsByWorld(List<Item> items)
    {
        return items
            .GroupBy(item => item.CanonicalWorldName)
            .OrderBy(group => GetWorldSortOrder(group.Key))
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new MissingItemGroup
            {
                CanonicalWorldName = group.Key,
                Items = group.OrderBy(item => item.Type).ThenBy(item => item.Name).ToList()
            })
            .ToList();
    }

    private static int GetWorldSortOrder(string canonicalWorldName)
    {
        return canonicalWorldName switch
        {
            "Ward 13" => 0,
            "Losomn" => 1,
            "N'Erud" => 2,
            "Yaesha" => 3,
            "The Labyrinth" => 4,
            "Root Earth" => 5,
            _ => int.MaxValue
        };
    }

    private void ResetWorldFilterToggles()
    {
        IsNerudFilterChecked = false;
        IsYaeshaFilterChecked = false;
        IsLosomnFilterChecked = false;
    }

    #region Messages
    protected override void OnActivated()
    {
        Messenger.Register<MissingItemsViewModel, CharacterSelectChangedMessage>(this, (r, m) => {
            IsLoading = true; // Look into it later, sometimes task starts just a moment too late and the old stuff still can be seen
            Task.Run(async () => await CharacterUpdatedHandler(m.Value));
        });

        Messenger.Register<MissingItemsViewModel, SaveFileChangedMessage>(this, (r, m) => {
            IsLoading = true;
            Task.Run(async () => await ReadSave(m.CharacterCountChanged));
        });

        Messenger.Register<MissingItemsViewModel, CultureChangedMessage>(this, (r, m) => {
            r.ApplyFilter();
        });
    }
    #endregion Messages
}
