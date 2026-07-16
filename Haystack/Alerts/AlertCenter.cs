using Haystack.Data;

namespace Haystack.Alerts;

/// <summary>
/// The single compose-point for everything the alert bell shows. Each rule is
/// one private method returning its alerts; <see cref="GatherAll"/> concatenates
/// them and the bell renders whatever comes back, grouped by Alert.Group — so
/// adding a future rule (missed follow-ups, backup reminders, …) is one method
/// and one line here, with no UI changes.
/// </summary>
public static class AlertCenter
{
    public static List<Alert> GatherAll() =>
        [.. OverdueRelationships()];

    /// <summary>
    /// Relationship monitoring: companies and contacts with a "contact every N
    /// days" cadence whose window has lapsed (see IsOverdue on the models —
    /// activity on a company means activity on it or any of its contacts, and
    /// never-contacted records count from their creation date).
    /// </summary>
    private static IEnumerable<Alert> OverdueRelationships()
    {
        foreach (var company in new CompanyRepository().GetAll().Where(c => c.IsOverdue))
            yield return new Alert(
                "NEEDS CONTACT",
                company.Name,
                $"{company.EffectiveDaysSince} days since activity — expected every {company.ContactEveryDays}",
                AlertTarget.Company, company.Id);

        foreach (var contact in new ContactRepository().GetAll().Where(c => c.IsOverdue))
            yield return new Alert(
                "NEEDS CONTACT",
                string.IsNullOrWhiteSpace(contact.CompanyName)
                    ? contact.Name
                    : $"{contact.Name} ({contact.CompanyName})",
                $"{contact.EffectiveDaysSince} days since activity — expected every {contact.ContactEveryDays}",
                AlertTarget.Contact, contact.Id);
    }
}
