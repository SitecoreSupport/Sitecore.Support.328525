using Sitecore.Commerce.UX.CustomerOrderManager;
using Sitecore.Commerce.UX.CustomerOrderManager.Repositories;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq;
using Sitecore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Sitecore.Support.Commerce.UX.CustomerOrderManager.Repositories
{
  public class SupportSearchRepository : ISearchRepository
  {
    private const string OrdersIndexName = "commerce_orders_index";
    private const string CustomersIndexName = "commerce_userprofiles_index_master";

    public List<object> GetSearchResults(string itemType, string searchTerm, string parentId, string sortDirection, string sortProperty, int pageIndex, int pageSize, string environment, out int totalItemCount, List<string> requestedProperties)
    {
      Assert.IsNotNullOrEmpty(itemType, "The parameter itemType cannot be empty.");

      if (searchTerm != null)
      {
        searchTerm = searchTerm.Trim();
      }

      totalItemCount = 0;

      if (itemType.Equals("customer", StringComparison.OrdinalIgnoreCase))
      {
        return this.GetCustomerSearchResults(
            searchTerm,
            sortDirection,
            sortProperty,
            pageIndex,
            pageSize,
            environment,
            out totalItemCount,
            requestedProperties);
      }

      if (itemType.Equals("order", StringComparison.OrdinalIgnoreCase))
      {
        return this.GetOrderSearchResults(
            searchTerm,
            sortDirection,
            sortProperty,
            pageIndex,
            pageSize,
            environment,
            out totalItemCount,
            requestedProperties);
      }

      throw new InvalidOperationException("itemType not recognized");
    }

    private List<object> GetOrderSearchResults(string searchTerm, string sortDirection, string sortProperty, int pageIndex, int pageSize, string artifactStoreId, out int totalItemCount, List<string> requestedProperties)
    {
      ISearchIndex ordersIndex = ContentSearchManager.GetIndex(OrdersIndexName);

      using (var context = ordersIndex.CreateSearchContext())
      {
        var searchQuery = context.GetQueryable<OrderSearchResultItem>();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
          searchQuery = searchQuery.Where(
              r => (r.OrderConfirmationId.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) || r.Email.Equals(searchTerm, StringComparison.OrdinalIgnoreCase))
              && r.ArtifactStoreId.Equals(artifactStoreId, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
          searchQuery = searchQuery.Where(r => r.ArtifactStoreId.Equals(artifactStoreId, StringComparison.OrdinalIgnoreCase));
        }

        totalItemCount = searchQuery.Count();

        var isSortingSpecified = !string.IsNullOrWhiteSpace(sortProperty);
        var isPagingSpecified = pageSize > 0 && pageIndex >= 0;

        if (isSortingSpecified)
        {
          if (sortDirection == "Asc")
          {
            searchQuery = searchQuery.OrderBy(order => order[sortProperty]);
          }
          else
          {
            searchQuery = searchQuery.OrderByDescending(order => order[sortProperty]);
          }
        }

        if (isPagingSpecified)
        {
          searchQuery = searchQuery.Skip(pageIndex * pageSize).Take(pageSize);
        }

        var searchResults = searchQuery.GetResults();

        var resultsItems = searchResults.Select(r => this.CreateOrderResult(r.Document, requestedProperties));

        return resultsItems.ToList<object>();
      }
    }

    private List<object> GetCustomerSearchResults(string searchTerm, string sortDirection, string sortProperty, int pageIndex, int pageSize, string environment, out int totalItemCount, List<string> requestedProperties)
    {
      ISearchIndex customersIndex = ContentSearchManager.GetIndex(CustomersIndexName);

      using (var context = customersIndex.CreateSearchContext())
      {
        var searchQuery = context.GetQueryable<CustomerSearchResultItem>();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
          if (searchTerm.Contains("*"))
          {
            if (searchTerm.EndsWith("*", StringComparison.OrdinalIgnoreCase))
            {
              searchQuery = searchQuery.Where(
                  r => (r.UserId.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                  r.Email.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                  r.FirstName.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                  r.LastName.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                  r.Content.Contains(searchTerm)));
            }
            else if (searchTerm.StartsWith("*", StringComparison.OrdinalIgnoreCase))
            {
              searchQuery = searchQuery.Where(
                  r => (r.UserId.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                  r.Email.EndsWith(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                  r.FirstName.EndsWith(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                  r.LastName.EndsWith(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                  r.Content.Contains(searchTerm)));
            }
          }
          else
          {
            searchQuery = searchQuery.Where(
                r => (r.UserId.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                r.Email.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                r.FirstName.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                r.LastName.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                r.Content.Equals(searchTerm)));
          }
        }

        totalItemCount = searchQuery.Count();

        var isSortingSpecified = !string.IsNullOrWhiteSpace(sortProperty);
        var isPagingSpecified = pageSize > 0 && pageIndex >= 0;

        if (isSortingSpecified)
        {
          if (sortDirection == "Asc")
          {
            searchQuery = searchQuery.OrderBy(customer => customer[sortProperty]);
          }
          else
          {
            searchQuery = searchQuery.OrderByDescending(customer => customer[sortProperty]);
          }
        }

        if (isPagingSpecified)
        {
          searchQuery = searchQuery.Skip(pageIndex * pageSize).Take(pageSize);
        }

        var searchResults = searchQuery.GetResults();

        var resultsItems = searchResults.Select(r => this.CreateCustomerResult(r.Document, requestedProperties));

        return resultsItems.ToList<object>();
      }
    }

    private object CreateCustomerResult(CustomerSearchResultItem resultItem, List<string> requestedFields)
    {
      // Always include UserID, LastName, FirstName and email
      var result = new Dictionary<string, object>();
      result.Add("Id", resultItem.UserId);
      result.Add("first_name", resultItem.FirstName);
      result.Add("last_name", resultItem.LastName);
      result.Add("email_address", resultItem.Email);
      if (!resultItem.ExternalId.Contains("CommerceUsers"))
      {
        result.Add("ItemId", resultItem.ExternalId);
      }
      else
      {
        result.Add("ItemId", resultItem.UserId);
      }

      result.Add("Template", "Customer");

      // TODO: Get the last order date from the customer's orders
      result.Add("LastOrderDate", DateUtil.ToIsoDate(DateTime.Now));
      result.Add("customertargeturl", "/sitecore/client/Applications/CustomerOrderManager/Customer?target=" + resultItem.UserId);

      requestedFields.ForEach(f =>
      {
        if (!result.ContainsKey(f.ToLower(CultureInfo.InvariantCulture)) && resultItem.Fields.ContainsKey(f))
        {
          var fieldValue = resultItem.Fields[f];

          if (fieldValue.GetType() == typeof(DateTime))
          {
            result.Add(f, DateUtil.ToIsoDate((DateTime)fieldValue));
          }
          else
          {
            result.Add(f, resultItem.Fields[f]);
          }
        }
      });

      return result;
    }

    private object CreateOrderResult(OrderSearchResultItem resultItem, List<string> requestedFields)
    {
      // Always include order id, order confirmation, order date, and orderTargetUrl
      var result = new Dictionary<string, object>();

      result.Add("orderid", resultItem.OrderId);
      result.Add("orderconfirmationid", resultItem.OrderConfirmationId);
      result.Add("ordertargeturl", "/sitecore/client/Applications/CustomerOrderManager/Order?target=" + resultItem.OrderId);
      result.Add("orderplaceddate", DateUtil.ToIsoDate(resultItem.OrderDate));

      requestedFields.ForEach(f =>
      {
        if (!result.ContainsKey(f.ToLower(CultureInfo.InvariantCulture)) && resultItem.Fields.ContainsKey(f))
        {
          var fieldValue = resultItem.Fields[f];

          DateTime date;
          if (DateTime.TryParse(fieldValue.ToString(), out date))
          {
            result.Add(f, DateUtil.ToIsoDate(date)); result.Add(f, DateUtil.ToIsoDate(date));
          }
          else
          {
            result.Add(f, resultItem.Fields[f]);
          }
        }
      });

      return result;
    }
  }
}
