using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Haystack.Data;
using Haystack.Models;
using Haystack.ViewModels.Base;

namespace Haystack.ViewModels;

public partial class LeadDetailViewModel : ViewModelBase
{
    private readonly LeadRepository _leadRepo = new();
    private readonly LeadStatusRepository _statusRepo = new();
    private readonly CompanyRepository _companyRepo = new();

    [ObservableProperty]
    private Lead _lead = new();

    [ObservableProperty]
    private ObservableCollection<LeadStatus> _statuses = [];

    [ObservableProperty]
    private LeadStatus? _selectedStatus;

    public LeadDetailViewModel()
    {
        Statuses = new ObservableCollection<LeadStatus>(_statusRepo.GetAll());
        // New leads default to the first status (e.g. "New") for fast entry.
        SelectedStatus = Statuses.FirstOrDefault();
    }

    public void LoadLead(int leadId)
    {
        var l = _leadRepo.GetById(leadId);
        if (l is not null)
        {
            Lead = l;
            SelectedStatus = Statuses.FirstOrDefault(s => s.Id == l.StatusId);
        }
    }

    /// <summary>The reason the lead can't be saved yet, or null if it's valid.</summary>
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Lead.Name) && string.IsNullOrWhiteSpace(Lead.Company))
            return "Enter a name or a company.";
        return null;
    }

    /// <summary>Saves the lead. Returns false (without saving) if invalid.</summary>
    public bool SaveLead()
    {
        if (Validate() is not null) return false;

        Lead.StatusId = SelectedStatus?.Id;
        Lead.UpdatedAt = DateTime.UtcNow;
        if (Lead.Id == 0)
            Lead.Id = _leadRepo.Insert(Lead);
        else
            _leadRepo.Update(Lead);
        return true;
    }

    /// <summary>The reason this lead can't be converted, or null if it can.</summary>
    public string? ValidateConvert()
    {
        if (Validate() is { } problem) return problem;
        if (Lead.IsConverted) return "This lead has already been converted.";
        if (string.IsNullOrWhiteSpace(Lead.Company))
            return "A company name is required to convert this lead.";
        return null;
    }

    /// <summary>
    /// Converts the lead into a company, pre-filled from the lead's details.
    /// The lead is KEPT: it's linked to the new company and moved to the won status.
    /// Returns the new company's id.
    /// </summary>
    public int ConvertToCompany()
    {
        SaveLead();   // persist any edits before converting

        var company = new Company
        {
            Name   = Lead.Company!.Trim(),
            Phone  = Lead.Phone,
            Email  = Lead.Email,
            Notes  = Lead.Notes,
            Type   = ContactType.Customer,
            Status = ContactStatus.Active
        };
        company.Id = _companyRepo.Insert(company);

        var won = _statusRepo.GetWonStatus();
        _leadRepo.MarkConverted(Lead.Id, company.Id, won?.Id);

        return company.Id;
    }
}
