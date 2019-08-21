﻿using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Autofac.Integration.AspNetCore.Multitenant.Test
{
    public sealed class HostedMultitenancyTests : IClassFixture<TestServerFixture>
    {
        private readonly TestServerFixture _testServerFixture;

        public HostedMultitenancyTests(TestServerFixture testServerFixture)
        {
            _testServerFixture = testServerFixture;
        }

        [Fact]
        public async Task CallRootEndpoint_HasTheCorrectDependenciesAndResponseIsBase()
        {
            var client = _testServerFixture.CreateClient();

            var response = await client.GetAsync("root-endpoint");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("base", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CallTenantEndpoint_WithNonExistantTenantReturns404()
        {
            var client = _testServerFixture.CreateClient();

            var response = await client.GetAsync("tenant-endpoint?tenant=tenant-does-not-exist");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("a", "a")]
        [InlineData("b", "b")]
        public async Task CallTenantEndpoint_HasTheCorrectDependenciesAndResponseIsTenantItself(string tenantQuery, string expectedTenantId)
        {
            var client = _testServerFixture.CreateClient();
            var response = await client.GetAsync($"tenant-endpoint?tenant={tenantQuery}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedTenantId, await response.Content.ReadAsStringAsync());
        }

        [Theory]
        [InlineData("", "base")]
        [InlineData("wrong-tenant", "base")]
        [InlineData("a", "a")]
        [InlineData("b", "b")]
        public async Task CallGenericEndpoint_HasTheCorrectDependenciesAndResponseIsTenantOrBase(string tenantQuery, string expectedTenantId)
        {
            var client = _testServerFixture.CreateClient();

            var response = await client.GetAsync($"supports-with-and-without-tenant?tenant={tenantQuery}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedTenantId, await response.Content.ReadAsStringAsync());
        }
    }
}