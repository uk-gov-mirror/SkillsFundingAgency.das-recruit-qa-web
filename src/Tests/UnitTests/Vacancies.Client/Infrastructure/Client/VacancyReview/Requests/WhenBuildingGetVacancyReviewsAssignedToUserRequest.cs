using System.Collections.Generic;
using System.Web;
using AutoFixture.NUnit4;
using Microsoft.AspNetCore.WebUtilities;
using Recruit.Vacancies.Client.Infrastructure.VacancyReview.Requests;
using NUnit.Framework;

namespace Recruit.Qa.Vacancies.Client.UnitTests.Vacancies.Client.Infrastructure.Client.VacancyReview.Requests;

public class WhenBuildingGetVacancyReviewsAssignedToUserRequest
{
    [Test, AutoData]
    public void Then_The_Request_Is_Built_Correctly(string userId, DateTime assignationExpiry, string status)
    {
        // arrange
        var expectedUrl = QueryHelpers.AddQueryString("users/VacancyReviews", new Dictionary<string, string>
        {
            ["assignationExpiry"] = $"{assignationExpiry:O}",
            ["status"] = status,
            ["userId"] = userId + "@%$£" + userId,
        });
        
        // act
        var actual = new GetVacancyReviewsAssignedToUserRequest(userId + "@%$£" + userId, assignationExpiry, status);

        // assert
        actual.GetUrl.Should().Be(expectedUrl);
    }
}