﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using Autofac.Multitenant;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
#if NETCOREAPP3_0
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
#endif
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Autofac.Integration.AspNetCore.Multitenant.Test
{
    public sealed class HostedMultitenancyTests : IClassFixture<TestServerFixture>
    {
        private readonly TestServerFixture _testServerFixture;

        public HostedMultitenancyTests(TestServerFixture testServerFixture)
        {
            this._testServerFixture = testServerFixture;
        }

        [Fact]
        public async Task CallRootEndpoint_HasTheCorrectDependenciesAndResponseIsBase()
        {
            var client = this._testServerFixture.GetApplicationClient();

            var response = await client.GetAsync("root-endpoint");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("base", await response.Content.ReadAsStringAsync());
        }

        [Theory]
        [InlineData("a", "a")]
        [InlineData("b", "b")]
        public async Task CallTenantEndpoint_HasTheCorrectDependenciesAndResponseIsTenantItself(string tenantQuery, string expectedTenantId)
        {
            var client = this._testServerFixture.GetApplicationClient();

            var response = await client.GetAsync($"tenant-endpoint?tenant={tenantQuery}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedTenantId, await response.Content.ReadAsStringAsync());
        }

        [Theory]
        [InlineData("tenant-does-not-exist")]
        public async Task CallTenantEndpoint_WithNonExistantTenantReturns404(string tenantQuery)
        {
            var client = this._testServerFixture.GetApplicationClient();

            var response = await client.GetAsync($"tenant-endpoint?tenant={tenantQuery}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("", "base")]
        [InlineData("wrong-tenant", "base")]
        [InlineData("a", "a")]
        [InlineData("b", "b")]
        public async Task CallGenericEndpoint_HasTheCorrectDependenciesAndResponseIsTenantOrBase(string tenantQuery, string expectedTenantId)
        {
            var client = this._testServerFixture.GetApplicationClient();

            var response = await client.GetAsync($"supports-with-and-without-tenant?tenant={tenantQuery}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedTenantId, await response.Content.ReadAsStringAsync());
        }
    }
}