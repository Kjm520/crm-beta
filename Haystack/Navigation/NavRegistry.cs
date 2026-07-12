using System.Windows.Controls;
using Haystack.Views.Activity;
using Haystack.Views.Companies;
using Haystack.Views.Contacts;
using Haystack.Views.Dashboard;
using Haystack.Views.Leads;
using Haystack.Views.Workflow;

namespace Haystack.Navigation;

/// <summary>One left-panel tab: a stable key (for persistence), a label, and how to build its view.</summary>
public record NavItem(string Key, string Label, Func<UserControl> CreateView);

/// <summary>
/// Single source of truth for the left-panel tabs. The order here is the DEFAULT;
/// the user's saved order (app_settings "nav_order") overrides it.
/// </summary>
public static class NavRegistry
{
    public const string OrderSettingKey = "nav_order";

    public static readonly IReadOnlyList<NavItem> All = new List<NavItem>
    {
        new("dashboard", "Dashboard", () => new DashboardView()),
        new("companies", "Companies", () => new CompaniesView()),
        new("contacts",  "Contacts",  () => new ContactsView()),
        new("leads",     "Leads",     () => new LeadsView()),
        new("activity",  "Activity",  () => new ActivityView()),
        new("workflow",  "Workflow",  () => new WorkflowView()),
    };

    /// <summary>
    /// Returns the tabs in the user's saved order. Any tab not in the saved order
    /// (e.g. one added in a later version) keeps its default position at the end,
    /// so new tabs always appear even for existing users.
    /// </summary>
    public static IReadOnlyList<NavItem> InSavedOrder(string? savedCsv)
    {
        var order = (savedCsv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        // OrderBy is a stable sort, so unsaved items (index -1 -> int.MaxValue) keep
        // their relative registry order at the end.
        return All
            .OrderBy(item =>
            {
                var i = order.IndexOf(item.Key);
                return i < 0 ? int.MaxValue : i;
            })
            .ToList();
    }
}
