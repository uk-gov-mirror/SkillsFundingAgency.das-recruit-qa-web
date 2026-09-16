using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.WebUtilities;
using Recruit.Vacancies.Client.Infrastructure.OuterApi.Interfaces;

namespace Recruit.Vacancies.Client.Infrastructure.VacancyReview.Requests;

public class GetVacancyReviewsAssignedToUserRequest(string userId, DateTime assignationExpiry, string status) : IGetApiRequest
{
    public string GetUrl => QueryHelpers.AddQueryString("users/VacancyReviews", new Dictionary<string, string>
    {
        ["assignationExpiry"] = $"{assignationExpiry:O}",
        ["status"] = status,
        ["userId"] = userId,
    });
}