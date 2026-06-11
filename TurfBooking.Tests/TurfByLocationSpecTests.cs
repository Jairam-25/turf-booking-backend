using Domain.Entities;
using Domain.Specifications;
using FluentAssertions;

namespace TurfBooking.Tests.Specifications
{
    public class TurfByLocationSpecTests
    {
        [Theory]
        [InlineData("Tamil Nadu", 2)]
        [InlineData("Thanjavur", 1)]
        [InlineData("Chennai", 1)]
        [InlineData("Karnataka", 1)]
        [InlineData("Bangalore", 1)]
        [InlineData("Mumbai", 0)]
        public void TurfByLocationSpec_FiltersByLocationStateOrCity(string query, int expectedCount)
        {
            // Arrange
            var turfs = new List<Turf>
            {
                new Turf { Name = "Turf 1", Location = "37 Random Street", City = "Thanjavur", State = "Tamil Nadu" },
                new Turf { Name = "Turf 2", Location = "Chennai, Tamil Nadu 600001", City = "", State = "" },
                new Turf { Name = "Turf 3", Location = "Downtown", City = "Bangalore", State = "Karnataka" }
            }.AsQueryable();

            var spec = new TurfByLocationSpec(query);

            // Act
            var result = turfs.Where(spec.Criteria).ToList();

            // Assert
            result.Count.Should().Be(expectedCount);
        }
    }
}
