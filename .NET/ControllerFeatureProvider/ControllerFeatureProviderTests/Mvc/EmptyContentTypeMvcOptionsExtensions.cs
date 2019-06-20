using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace ControllerFeatureProviderTests.Mvc
{
    public static class EmptyContentTypeMvcOptionsExtensions
    {
        public static void AddEmptyContentTypeFormatter(this MvcOptions options)
        {
            _ = options ?? throw new ArgumentNullException(nameof(options));

            JsonInputFormatter formatter = (JsonInputFormatter)options.InputFormatters.First(f => f.GetType() == typeof(JsonInputFormatter));
            options.InputFormatters.Add(new EmptyContentTypeJsonInputFormatter(formatter));
        }
    }
}