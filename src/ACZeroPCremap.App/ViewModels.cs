using ACZeroPCremap.Core.Localization;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ACZeroPCremap.Core;
using Microsoft.Win32;

namespace ACZeroPCremap.App;

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}

public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => execute();
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}

public sealed class MainViewModel : ObservableObject
{
    private readonly Window owner;
    private ConfigSession? session;
    private readonly LanguagePreferences languagePreferences;
    public IReadOnlyList<AppLanguage> Languages => LanguagePreferences.Languages;
    public string SelectedLanguage
    {
        get => Text.Culture.Name;
        set
        {
            if (value is null || value == Text.Culture.Name || !Languages.Any(l => l.Code == value)) return;
            Text.Culture = LanguagePreferences.CultureFor(value);
            var saved = true;
            try { languagePreferences.Save(value); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { saved = false; }
            foreach (var section in Sections) section.Relocalize();
            Status = Text.Get(saved ? "Language.Changed" : "Language.SaveFailed");
            Refresh();
            Changed();
        }
    }
    public ObservableCollection<SectionViewModel> Sections { get; }
    public string FilePath => session?.FilePath ?? Text.Get("NoFile");
    public bool HasFile => session is not null;
    public string DirtyLabel => session?.IsDirty == true ? Text.Get("UnsavedBadge") : "";
    public string WindowTitle => "ACZeroPCremap" + (session?.IsDirty == true ? " *" : "");
    public string Status { get; private set; } = Text.Get("ChooseFile");
    public ICommand OpenCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DiscardCommand { get; }
    public ICommand ReloadCommand { get; }
    public ICommand RestoreCommand { get; }

    public MainViewModel(Window owner, LanguagePreferences? languagePreferences = null)
    {
        this.owner = owner;
        this.languagePreferences = languagePreferences ?? new(LanguagePreferences.DefaultPath);
        Sections = [new(this, "KeyboardFlight"), new(this, "KeyboardUI")];
        OpenCommand = new RelayCommand(OpenFile);
        SaveCommand = new RelayCommand(() => Save(), () => session?.IsDirty == true);
        DiscardCommand = new RelayCommand(Discard, () => session?.IsDirty == true);
        ReloadCommand = new RelayCommand(Reload, () => session is not null);
        RestoreCommand = new RelayCommand(Restore, () => session is not null);
        Refresh();
    }

    public void OpenInitial(string? path)
    {
        if (path is null) return;
        Run(() => { session = ConfigSession.Open(path); Status = Text.Get("OpenedInitial"); });
    }

    private void OpenFile()
    {
        var dialog = new OpenFileDialog { Filter = Text.Get("IniFilter"), Title = Text.Get("OpenTitle") };
        if (dialog.ShowDialog(owner) != true) return;
        Run(() =>
        {
            _ = ConfigSession.Open(dialog.FileName);
            if (!CanLeave()) return;
            // The selected file may be the current file just saved by CanLeave().
            session = ConfigSession.Open(dialog.FileName);
            Status = Text.Get("Opened");
        });
    }

    public bool CanLeave()
    {
        if (session?.IsDirty != true) return true;
        var choice = MessageDialog.Show(owner, Text.Get("UnsavedPrompt"),
            "ACZeroPCremap", MessageBoxButton.YesNoCancel, savePrompt: true);
        return choice switch { MessageBoxResult.Yes => Save(), MessageBoxResult.No => true, _ => false };
    }

    private bool Save() => Run(() =>
    {
        if (session is null) return;
        var backup = session.Save();
        Status = backup is null ? Text.Get("Unchanged") : Text.Format("Saved", backup);
    });

    private void Discard()
    {
        if (MessageDialog.Show(owner, Text.Get("DiscardPrompt"), Text.Get("Discard"),
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        Run(() => { session!.DiscardChanges(); Status = Text.Get("Discarded"); });
    }

    private void Reload()
    {
        if (session is null) return;
        if (session.IsDirty && MessageDialog.Show(owner, Text.Get("ReloadPrompt"),
                Text.Get("Reload"), MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        Run(() => { session = ConfigSession.Open(session.FilePath); Status = Text.Get("Reloaded"); });
    }

    private void Restore()
    {
        if (session is null) return;
        var dialog = new OpenFileDialog
        {
            Filter = Text.Get("BackupFilter"), Title = Text.Get("SelectBackup"),
            InitialDirectory = Path.GetDirectoryName(session.FilePath)
        };
        if (dialog.ShowDialog(owner) != true) return;
        Run(() =>
        {
            if (!CanLeave()) return;
            var date = File.GetLastWriteTime(dialog.FileName).ToString("g", Text.Culture);
            if (MessageDialog.Show(owner, Text.Format("RestorePrompt", dialog.FileName, date),
                    Text.Get("RestoreTitle"), MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (session.IsDirty) session.DiscardChanges();
            var backup = session.Restore(dialog.FileName);
            Status = Text.Format("Restored", backup);
        });
    }

    internal void Assign(string section, string key, BindingEntry? entry)
    {
        var dialog = new BindingDialog(section, key, entry?.Value, session!.Document) { Owner = owner };
        if (dialog.ShowDialog() != true || dialog.Result is null) return;
        if (!ConfirmSharedBinding(section, key, dialog.Result.Value, entry)) return;
        Run(() =>
        {
            if (entry is null) session!.Document.Add(section, key, dialog.Result.Value);
            else session!.Document.Replace(section, entry.Id, dialog.Result.Value);
            Status = Text.Get("Assigned");
        });
    }

    private bool ConfirmSharedBinding(string section, string key, string value, BindingEntry? entry)
    {
        if (entry is not null && Bindings.InputIdentity(section, entry.Value) == Bindings.InputIdentity(section, value)) return true;
        var warning = session!.Document.SharedInputWarning(section, key, value);
        return warning.Length == 0 || MessageDialog.Show(owner, warning + "\n\n" + Text.Get("Binding.SharedConfirm"),
            Text.Get("Binding.SharedTitle"), MessageBoxButton.YesNo) == MessageBoxResult.Yes;
    }

    internal void SetFlightMouseEnabled(bool enabled) => Run(() =>
    {
        session!.Document.SetFlightMouseEnabled(enabled);
        Status = Text.Get(enabled ? "MouseFlight.Enabled" : "MouseFlight.Disabled");
    });

    internal void Remove(string section, BindingEntry entry)
    {
        if (MessageDialog.Show(owner, Text.Format("RemovePrompt", entry.DisplayName, Bindings.ActionName(section, entry.Key)),
                Text.Get("RemoveTitle"), MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        Run(() =>
        {
            session!.Document.Remove(section, entry.Id);
            Status = Text.Get("Removed");
        });
    }

    private bool Run(Action action)
    {
        try { action(); Refresh(); return true; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            // Framework exception messages use the Windows language. Show our own
            // localized explanation; preserve detailed messages for app/domain errors.
            Status = ex switch
            {
                FileNotFoundException or DirectoryNotFoundException => Text.Get("Error.FileMissing"),
                UnauthorizedAccessException => Text.Get("Error.AccessDenied"),
                ExternalChangeException or InvalidDataException or InvalidOperationException => ex.Message,
                IOException => Text.Get("Error.FileAccess"),
                _ => Text.Get("Error.Unexpected")
            };
            Refresh();
            MessageDialog.Show(owner, Status + (session is null ? "" : Text.Format("FileContext", session.FilePath)),
                Text.Get("OperationFailed"));
            return false;
        }
    }

    private void Refresh()
    {
        foreach (var section in Sections) section.Update(session?.Document);
        Changed(nameof(HasFile)); Changed(nameof(FilePath)); Changed(nameof(DirtyLabel)); Changed(nameof(WindowTitle)); Changed(nameof(Status));
        CommandManager.InvalidateRequerySuggested();
    }
}

public sealed class SectionViewModel(MainViewModel main, string section) : ObservableObject
{
    private readonly string currentSection = section;
    private bool useMouseInFlight;
    public bool ShowMouseOption => currentSection.Equals("KeyboardFlight", StringComparison.OrdinalIgnoreCase);
    public bool CanToggleMouse { get; private set; }
    public string MouseHelp => Text.Get(CanToggleMouse ? "MouseFlight.Help" : "MouseFlight.Unavailable");
    public bool UseMouseInFlight
    {
        get => useMouseInFlight;
        set
        {
            if (value == useMouseInFlight || !CanToggleMouse) return;
            main.SetFlightMouseEnabled(value);
        }
    }
    private string searchText = "";
    private string selectedGroupId = "All";
    public string Title => Text.Get(currentSection.Equals("KeyboardUI", StringComparison.OrdinalIgnoreCase) ? "MenuTab" : "FlightTab");
    public string Note => Bindings.SectionNote(currentSection);
    public string ShortNote => Text.Get(currentSection.Equals("KeyboardUI", StringComparison.OrdinalIgnoreCase) ? "Menu.ShortNote" : "Flight.ShortNote");
    public string Problem { get; private set; } = "";
    public string ConflictText { get; private set; } = "";
    public IReadOnlyList<ActionViewModel> Actions { get; private set; } = [];
    public IReadOnlyList<ActionGroupViewModel> Groups { get; private set; } = [];
    public IReadOnlyList<GroupOption> GroupOptions { get; } = CreateGroupOptions(section);
    internal void Relocalize()
    {
        foreach (var group in GroupOptions) group.Relocalize();
        Changed(nameof(SelectedGroupId));
        Changed(nameof(Title)); Changed(nameof(Note)); Changed(nameof(ShortNote));
    }
    public bool HasNoResults => Actions.Count > 0 && Groups.Count == 0;
    public string ResultSummary => Text.Format("Search.Results", Groups.Sum(g => g.Actions.Count), Actions.Count);
    public ICommand ClearSearchCommand => new RelayCommand(() => SearchText = "", () => SearchText.Length > 0);
    public ICommand ResetFiltersCommand => new RelayCommand(() => { SearchText = ""; SelectedGroupId = "All"; });

    public string SearchText
    {
        get => searchText;
        set
        {
            if (searchText == value) return;
            searchText = value ?? "";
            Changed();
            ApplyFilters();
        }
    }

    public string SelectedGroupId
    {
        get => selectedGroupId;
        set
        {
            if (value is null || value == selectedGroupId) return;
            selectedGroupId = value;
            Changed();
            ApplyFilters();
        }
    }

    internal void Update(IniDocument? document)
    {
        var section = currentSection;
        useMouseInFlight = ShowMouseOption && document?.HasFlightMouseBindings == true;
        CanToggleMouse = ShowMouseOption && document is not null && document.SectionProblem(section) is null &&
            (document.HasFlightMouseBindings || document.HasDisabledFlightMouseBindings);
        Changed(nameof(UseMouseInFlight)); Changed(nameof(CanToggleMouse)); Changed(nameof(MouseHelp));
        Problem = document is null ? Text.Get("OpenConfig") : document.SectionProblem(section) ?? "";
        var entries = document?.Entries(section) ?? [];
        var keys = Bindings.ActionKeys.Concat(entries.Select(e => e.Key).Where(k => !Bindings.IsKnownAction(k))).Distinct(StringComparer.OrdinalIgnoreCase);
        Actions = document is null || document.SectionCount(section) == 0 ? [] : keys.Select(key => new ActionViewModel(main, section, key,
            entries.Where(e => e.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).ToArray(), Problem.Length == 0)).ToArray();
        var conflicts = document?.Conflicts(section) ?? [];
        ConflictText = conflicts.Count == 0 ? "" : Text.Get("SharedBindings") + string.Join("; ", conflicts.Select(c => $"{c.InputName} → {string.Join(", ", c.Actions.Select(a => Bindings.ActionName(section, a)))}"));
        Changed(nameof(Problem)); Changed(nameof(ConflictText)); Changed(nameof(Actions));
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var terms = SearchText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Groups = GroupOptions.Where(g => g.Id != "All")
            .Select(g => new ActionGroupViewModel(g.Name, Actions.Where(action =>
                action.GroupId == g.Id && (SelectedGroupId == "All" || SelectedGroupId == g.Id) &&
                terms.All(term => action.SearchContent.Contains(term, StringComparison.OrdinalIgnoreCase))).ToArray(),
                currentSection.Equals("KeyboardUI", StringComparison.OrdinalIgnoreCase)
                    ? g.Id == "Unused" ? Text.Get("Menu.UnusedHint")
                        : g.Id is not ("Navigation" or "Selection") ? Text.Get("Menu.ReplayGroupNote") : ""
                    : ""))
            .Where(g => g.Actions.Count > 0).ToArray();
        Changed(nameof(Groups)); Changed(nameof(HasNoResults)); Changed(nameof(ResultSummary));
        CommandManager.InvalidateRequerySuggested();
    }

    private static IReadOnlyList<GroupOption> CreateGroupOptions(string section)
    {
        string[] ids = section.Equals("KeyboardUI", StringComparison.OrdinalIgnoreCase)
            ? ["All", "Navigation", "Selection", "Camera", "Additional", "Unused", "Other"]
            : ["All", "Flight", "Weapons", "Camera", "Wingman", "Other"];
        return ids.Select(id => new GroupOption(id, Text.Get("Group." + id))).ToArray();
    }
}

public sealed class GroupOption(string id, string name) : ObservableObject
{
    public string Id { get; } = id;
    public string Name { get; private set; } = name;
    internal void Relocalize() { Name = Text.Get("Group." + Id); Changed(nameof(Name)); }
}
public sealed record ActionGroupViewModel(string Name, IReadOnlyList<ActionViewModel> Actions, string Description = "")
{
    public string CountText => Actions.Count == 1 ? Text.Get("Group.CountOne") : Text.Format("Group.Count", Actions.Count);
}

public sealed class ActionViewModel
{
    public string Key { get; }
    public string Name { get; }
    public string Description { get; }
    public string GroupId { get; }
    public string SearchContent { get; }
    public IReadOnlyList<BindingRowViewModel> Rows { get; }
    public string EmptyText { get; }
    public ICommand AddCommand { get; }

    internal ActionViewModel(MainViewModel main, string section, string key, IReadOnlyList<BindingEntry> entries, bool enabled)
    {
        Key = key;
        Name = Bindings.ActionName(section, key);
        Description = Bindings.ActionDescription(section, key);
        GroupId = GetGroup(section, key);
        Rows = entries.Select(e => new BindingRowViewModel(main, section, e, enabled)).ToArray();
        // Match visible names and file values; keep every alternative on a matching action.
        SearchContent = string.Join(" ", new[] { Name, Description, Key, Text.Get("Group." + GroupId) }
            .Concat(entries.SelectMany(e => new[] { e.DisplayName, e.Value })));
        EmptyText = entries.Any(e => e.Editable) ? "" : Text.Get("NoSimpleBindings");
        AddCommand = new RelayCommand(() => main.Assign(section, key, null), () => enabled && Bindings.IsKnownAction(key));
    }

    private static string GetGroup(string section, string key)
    {
        if (!Bindings.IsKnownAction(key)) return "Other";
        var code = key.ToUpperInvariant();
        if (section.Equals("KeyboardUI", StringComparison.OrdinalIgnoreCase)) return code switch
        {
            "HATL" or "HATR" or "HATU" or "HATD" or "LX-" or "LX+" or "LY-" or "LY+" => "Navigation",
            "A" or "B" or "MENU" => "Selection",
            "RX-" or "RX+" or "RY-" or "RY+" => "Camera",
            "LT" or "RT" or "LSC" or "RSC" => "Unused",
            _ => "Additional"
        };
        return code switch
        {
            "HATL" or "HATR" or "HATU" or "HATD" => "Wingman",
            "A" or "B" or "X" or "Y" or "LSC" or "VIEW" => "Weapons",
            "RX-" or "RX+" or "RY-" or "RY+" or "RSC" => "Camera",
            _ => "Flight"
        };
    }
}

public sealed class BindingRowViewModel
{
    public string DisplayName { get; }
    public string Detail { get; }
    public string EditHint { get; }
    public bool IsEditable { get; }
    public ICommand EditCommand { get; }
    public ICommand RemoveCommand { get; }

    internal BindingRowViewModel(MainViewModel main, string section, BindingEntry entry, bool enabled)
    {
        DisplayName = entry.DisplayName;
        IsEditable = entry.Editable;
        Detail = entry.Editable ? entry.Value : Text.Get("ReadOnlyPrefix") + entry.Value;
        EditHint = Text.Get("Binding.EditHint") + "\n" + entry.Value;
        EditCommand = new RelayCommand(() => main.Assign(section, entry.Key, entry), () => enabled && entry.Editable);
        RemoveCommand = new RelayCommand(() => main.Remove(section, entry), () => enabled && entry.Editable);
    }
}
