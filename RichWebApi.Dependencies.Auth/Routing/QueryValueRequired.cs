using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Routing;

namespace RichWebApi.Routing;

public class QueryValueRequiredAttribute(string queryParameter, string value) : ActionMethodSelectorAttribute
{
	public override bool IsValidForRequest(RouteContext context, ActionDescriptor action)
	{
		return context.HttpContext.Request.Query.TryGetValue(queryParameter, out var values) 
		       && values.Any(v => string.Equals(v, value, StringComparison.OrdinalIgnoreCase));
	}
}