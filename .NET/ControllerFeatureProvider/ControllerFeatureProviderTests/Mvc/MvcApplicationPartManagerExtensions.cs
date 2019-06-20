using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace ControllerFeatureProviderTests.Mvc
{
    public static class MvcApplicationPartManagerExtensions
    {
        public static IMvcBuilder WithController<T>(this IMvcBuilder builder)
        {
            return builder.ConfigureApplicationPartManager(m =>
            {
                m.FeatureProviders.Add(new CustomControllerFeatureProvider(typeof(T).GetTypeInfo()));
            });
        }

        private class CustomControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
        {
            private readonly TypeInfo _controllerType;

            public CustomControllerFeatureProvider(TypeInfo controllerType)
            {
                _controllerType = controllerType ?? throw new ArgumentNullException(nameof(controllerType));
            }

            public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
            {
                feature.Controllers.Add(_controllerType);
            }
        }
    }
}