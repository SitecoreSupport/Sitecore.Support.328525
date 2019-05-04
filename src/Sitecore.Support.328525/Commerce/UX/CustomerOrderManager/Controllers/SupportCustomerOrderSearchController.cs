using Sitecore.Commerce.UX.CustomerOrderManager.Repositories;
using Sitecore.Diagnostics;
using Sitecore.Web;
using System.Collections.Generic;
using System.Web.Mvc;
using Sitecore.Commerce.UX.Shared.Controllers;
using System;
using Sitecore.Support.Commerce.UX.CustomerOrderManager.Repositories;

namespace Sitecore.Support.Commerce.UX.CustomerOrderManager.Controllers
{
  public class SupportCustomerOrderSearchController : BaseController
  {
    private ISearchRepository repository;

    public SupportCustomerOrderSearchController(ISearchRepository repository)
    {
      Assert.IsNotNull(repository, "repository");
      this.repository = repository;
    }

    public SupportCustomerOrderSearchController()
        : this(new SupportSearchRepository())
    {
      // TODO: IOC/DI
    }

    protected ISearchRepository Repository
    {
      get
      {
        return this.repository;
      }
    }

    public JsonResult GetSearchResults(string itemType, string searchTerm, string parentId = null)
    {
      var sortDirection = this.GetRequestSortDirection();
      var sortProperty = this.GetRequestSortProperty();
      var pageIndex = this.GetRequestPageIndex();
      var pageSize = this.GetRequestPageSize();
      var requestedProperties = this.GetRequestedFields();
      var language = GetRequestedLanguage();
      var currency = GetRequestedCurrency();
      var environment = GetRequestedEnvironment();

      var totalItemCount = 0;
      var results = this.Repository.GetSearchResults(
          itemType,
          searchTerm,
          parentId,
          sortDirection,
          sortProperty,
          pageIndex,
          pageSize,
          environment,
          out totalItemCount,
          requestedProperties);

      var queryResponse = new { Items = results.ToArray(), TotalItemCount = totalItemCount };
      return Json(queryResponse);
    }

    private string GetRequestedEnvironment()
    {
      var headerString = WebUtil.GetFormValue("Headers");
      if (!string.IsNullOrWhiteSpace(headerString))
      {
        var headers = headerString.Split('|');
        foreach (var item in headers)
        {
          var headerValues = item.Split(':');
          if (headerValues[0] == "Environment")
          {
            return headerValues[1];
          }
        }
      }

      return string.Empty;
    }

    private string GetRequestedCurrency()
    {
      var currency = WebUtil.GetFormValue("Currency");
      if (!string.IsNullOrWhiteSpace(currency))
      {
        return currency;
      }

      return string.Empty;
    }

    private string GetRequestedLanguage()
    {
      var language = WebUtil.GetFormValue("Language");
      if (!string.IsNullOrWhiteSpace(language))
      {
        return language;
      }

      return string.Empty;
    }

    private List<string> GetRequestedFields()
    {
      var delimitedFields = WebUtil.GetFormValue("fields");
      var requestedFields = new List<string>();

      if (!string.IsNullOrWhiteSpace(delimitedFields))
      {
        var fields = delimitedFields.Split('|');
        foreach (var field in fields)
        {
          if (!string.IsNullOrWhiteSpace(field))
          {
            requestedFields.Add(field);
          }
        }
      }

      return requestedFields;
    }

    private string GetRequestSortDirection()
    {
      var sorting = WebUtil.GetFormValue("Sorting");
      if (!string.IsNullOrWhiteSpace(sorting))
      {
        // By SPEAK convention, the sort direction is
        // the first character of the Sorting variable
        var sortDirection = sorting[0];
        if (sortDirection == 'a')
        {
          return "Asc";
        }

        if (sortDirection == 'd')
        {
          return "Desc";
        }
      }

      return string.Empty;
    }

    private string GetRequestSortProperty()
    {
      var sorting = WebUtil.GetFormValue("Sorting");
      if (!string.IsNullOrWhiteSpace(sorting) && sorting.Length > 1)
      {
        // By SPEAK convention, the sort property is
        // prefixed with the sort direction, so ignore the first character
        var sortProperty = sorting.Substring(1);
        return sortProperty;
      }

      return string.Empty;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "System.Int32.TryParse(System.String,System.Int32@)", Justification = "Out parameter is set to default value in case conversion fails.")]
    private int GetRequestPageIndex()
    {
      int pageIndex = 0;
      var pageIndexString = WebUtil.GetFormValue("PageIndex");
      if (!string.IsNullOrWhiteSpace(pageIndexString))
      {
        var parseSucceeded = int.TryParse(pageIndexString, out pageIndex);
      }

      return pageIndex;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "System.Int32.TryParse(System.String,System.Int32@)", Justification = "Out parameter is set to default value in case conversion fails.")]
    private int GetRequestPageSize()
    {
      int pageSize = 0;
      var pageSizeString = WebUtil.GetFormValue("PageSize");
      if (!string.IsNullOrWhiteSpace(pageSizeString))
      {
        int.TryParse(pageSizeString, out pageSize);
      }

      return pageSize;
    }
  }
}
