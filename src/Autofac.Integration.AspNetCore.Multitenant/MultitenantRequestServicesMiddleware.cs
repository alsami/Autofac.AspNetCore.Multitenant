using System;
using System.Threading.Tasks;
using Autofac;
using Autofac.Integration.AspNetCore.Multitenant;
using Autofac.Multitenant;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// Middleware that forces the request lifetime scope to be created from the multitenant container
    /// directly to avoid inadvertent incorrect tenant identification.
    /// </summary>
    internal class MultitenantRequestServicesMiddleware
    {
        private readonly IHttpContextAccessor _contextAccessor;

        private readonly Func<IContainer, MultitenantContainer> _multitenantContainerAccessor;

        private readonly RequestDelegate _next;

        private readonly IContainer _container;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultitenantRequestServicesMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next step in the request pipeline.</param>
        /// <param name="multitenantContainerAccessor">A function that will access the multitenant container from which request lifetimes should be generated.</param>
        /// <param name="container">The root container build using <see cref="ContainerBuilder"/>.</param>
        /// <param name="contextAccessor">The <see cref="IHttpContextAccessor"/> to set up with the current request context.</param>
        public MultitenantRequestServicesMiddleware(RequestDelegate next, Func<IContainer, MultitenantContainer> multitenantContainerAccessor, IContainer container, IHttpContextAccessor contextAccessor)
        {
            this._next = next;
            this._multitenantContainerAccessor = multitenantContainerAccessor;
            this._container = container;
            this._contextAccessor = contextAccessor;
        }

        /// <summary>
        /// Invokes the middleware using the specified context.
        /// </summary>
        /// <param name="context">
        /// The request context to process through the middleware.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> to await for completion of the operation.
        /// </returns>
        public async Task Invoke(HttpContext context)
        {
            // If there isn't already an HttpContext set on the context
            // accessor for this async/thread operation, set it. This allows
            // tenant identification to use it.
            if (this._contextAccessor.HttpContext == null)
            {
                this._contextAccessor.HttpContext = context;
            }

            var multitenantContainer =
                this._multitenantContainerAccessor(_container);

            if (multitenantContainer == null)
            {
                throw new InvalidOperationException(Properties.Resources.NoMultitenantContainerAvailable);
            }

            IServiceProvidersFeature existingFeature = null;
            try
            {
                var autofacFeature =
                    RequestServicesFeatureFactory.CreateFeature(context, multitenantContainer.Resolve<IServiceScopeFactory>());

                if (autofacFeature is IDisposable disp)
                {
                    context.Response.RegisterForDispose(disp);
                }

                existingFeature = context.Features.Get<IServiceProvidersFeature>();
                context.Features.Set(autofacFeature);
                await this._next.Invoke(context);
            }
            finally
            {
                // In ASP.NET Core 1.x the existing feature will disposed as part of
                // a using statement; in ASP.NET Core 2.x and ASP.NET Core 3.x, it is registered directly
                // with the response for disposal. In either case, we don't have to
                // do that. We do put back any existing feature, though, since
                // at this point there may have been some default tenant or base
                // container level stuff resolved and after this middleware it needs
                // to be what it was before.
                context.Features.Set(existingFeature);
            }
        }
    }
}