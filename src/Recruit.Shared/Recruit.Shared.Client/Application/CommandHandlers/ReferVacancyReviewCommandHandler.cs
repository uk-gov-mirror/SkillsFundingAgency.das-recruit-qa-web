using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Recruit.Vacancies.Client.Application.Commands;
using Recruit.Vacancies.Client.Application.Providers;
using Recruit.Vacancies.Client.Domain.Entities;
using Recruit.Vacancies.Client.Domain.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Recruit.Vacancies.Client.Infrastructure.OuterApi.Interfaces;
using Recruit.Vacancies.Client.Infrastructure.OuterApi.Requests;
using Recruit.Vacancies.Client.Infrastructure.Services;

namespace Recruit.Vacancies.Client.Application.CommandHandlers;

public class ReferVacancyReviewCommandHandler(
    ILogger<ReferVacancyReviewCommandHandler> logger,
    IVacancyReviewRepository vacancyReviewRepositoryRunner,
    IVacancyReviewQuery vacancyReviewQuery,
    IValidator<VacancyReview> vacancyReviewValidator,
    ITimeProvider timeProvider,
    IRecruitQaOuterApiClient outerApiClient)
    : IRequestHandler<ReferVacancyReviewCommand, Unit>
{
    public async Task<Unit> Handle(ReferVacancyReviewCommand message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Referring vacancy review {reviewId}.", message.ReviewId);

        var review = await vacancyReviewQuery.GetAsync(message.ReviewId);

        if (!review.CanRefer)
        {
            logger.LogWarning($"Unable to refer review {{reviewId}} for vacancy {{vacancyReference}} due to review having a status of {review.Status}.", message.ReviewId, review.VacancyReference);
            return Unit.Value;
        }

        review.ManualOutcome = ManualQaOutcome.Referred;
        review.Status = ReviewStatus.Closed;
        review.ClosedDate = timeProvider.Now;
        review.ManualQaComment = message.ManualQaComment;
        review.ManualQaFieldIndicators = message.ManualQaFieldIndicators;

        var dismissedFields = review
            .AutomatedQaOutcomeIndicators
            .Where(x => !message.SelectedAutomatedQaRuleOutcomeIds.Contains(x.Id))
            .Select(x => x.Target)
            .Distinct()
            .ToList();
            
        review.DismissedAutomatedQaOutcomeIndicators = dismissedFields;
        
        Validate(review);

        await vacancyReviewRepositoryRunner.UpdateAsync(review);
        
        var retryPolicy = PollyRetryPolicy.GetPolicy();
        await retryPolicy.Execute(
            _ => outerApiClient.Post(new PostReferVacancyRequest(review.VacancyReference)),
            new Dictionary<string, object> { { "apiCall", "ReferVacancy" } });
        
        return Unit.Value;
    }

    private void Validate(VacancyReview review)
    {
        var validationResult = vacancyReviewValidator.Validate(review);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
    }
}