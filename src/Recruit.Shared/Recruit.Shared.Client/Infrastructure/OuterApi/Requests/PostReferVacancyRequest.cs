using Recruit.Vacancies.Client.Infrastructure.OuterApi.Interfaces;

namespace Recruit.Vacancies.Client.Infrastructure.OuterApi.Requests;

public class PostReferVacancyRequest(long vacancyReference) : IPostApiRequest
{
    public string PostUrl => $"vacancies/refer/{vacancyReference}";
    public object Data { get; set; } = null;
}