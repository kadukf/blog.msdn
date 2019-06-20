using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace ControllerFeatureProviderTests.Mvc
{
    public class EmptyContentTypeJsonInputFormatter : InputFormatter
    {
        private readonly JsonInputFormatter _inner;

        public EmptyContentTypeJsonInputFormatter(JsonInputFormatter inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public override bool CanRead(InputFormatterContext context)
        {
            _ = context ?? throw new ArgumentNullException(nameof(context));

            string contentType = context.HttpContext.Request.ContentType;
            if (string.IsNullOrEmpty(contentType))
            {
                return true;
            }

            return base.CanRead(context);
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            return await _inner.ReadRequestBodyAsync(context).ConfigureAwait(false);
        }
    }

}