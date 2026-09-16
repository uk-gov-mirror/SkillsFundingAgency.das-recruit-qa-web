using System.Linq;
using System.Threading;
using AutoFixture.NUnit4;
using FluentValidation;
using FluentValidation.Results;
using NUnit.Framework;
using Recruit.Vacancies.Client.Application.CommandHandlers;
using Recruit.Vacancies.Client.Application.Commands;
using Recruit.Vacancies.Client.Application.Providers;
using Recruit.Vacancies.Client.Domain.Entities;
using Recruit.Vacancies.Client.Domain.Repositories;
using Recruit.Vacancies.Client.Infrastructure.OuterApi.Interfaces;
using Recruit.Vacancies.Client.Infrastructure.OuterApi.Requests;

namespace Recruit.Qa.Vacancies.Client.UnitTests.Vacancies.Client.Application.CommandHandlers;

public class WhenReferringVacancy
{
    [Test]
    [MoqAutoData]
    public async Task Then_Dismissed_Automated_Qa_Outcome_Indicators_Are_Set(
        Guid reviewId,
        [Frozen] Mock<IVacancyReviewRepository> vacancyReviewRepository,
        [Frozen] Mock<IVacancyReviewQuery> vacancyReviewQuery,
        [Frozen] Mock<ITimeProvider> timeProvider,
        [Frozen] Mock<IValidator<VacancyReview>> vacancyReviewValidator,
        [Greedy] ReferVacancyReviewCommandHandler sut)
    {
        // arrange
        timeProvider.Setup(t => t.Now).Returns(DateTime.UtcNow);

        var review = new VacancyReview
        {
            Id = reviewId,
            Status = ReviewStatus.UnderReview,
            VacancyReference = 1234567890,
            AutomatedQaOutcomeIndicators =
            [
                new RuleOutcome { Id = Guid.NewGuid(), Target = "Title" },
                new RuleOutcome { Id = Guid.NewGuid(), Target = "Description" },
                new RuleOutcome { Id = Guid.NewGuid(), Target = "Description" }
            ]
        };

        vacancyReviewQuery
            .Setup(x => x.GetAsync(reviewId))
            .ReturnsAsync(review);
        
        vacancyReviewValidator
            .Setup(x => x.Validate(It.IsAny<VacancyReview>()))
            .Returns(new ValidationResult());

        var command = new ReferVacancyReviewCommand(reviewId, "comment", [], [review.AutomatedQaOutcomeIndicators.First().Id]);

        // act
        await sut.Handle(command, CancellationToken.None);

        // assert
        vacancyReviewRepository.Verify(x => x.UpdateAsync(It.Is<VacancyReview>(vacancyReview =>
            vacancyReview.DismissedAutomatedQaOutcomeIndicators.Count == 1 &&
            vacancyReview.DismissedAutomatedQaOutcomeIndicators.Contains("Description"))), Times.Once);
    }
    
    [Test]
    [MoqAutoData]
    public async Task Then_Vacancy_Is_Referred(
        Guid reviewId,
        long vacancyReference,
        [Frozen] Mock<IVacancyReviewQuery> vacancyReviewQuery,
        [Frozen] Mock<ITimeProvider> timeProvider,
        [Frozen] Mock<IValidator<VacancyReview>> vacancyReviewValidator,
        [Frozen] Mock<IRecruitQaOuterApiClient> outerApiClient,
        [Greedy] ReferVacancyReviewCommandHandler sut)
    {
        // arrange
        timeProvider.Setup(t => t.Now).Returns(DateTime.UtcNow);
        var expectedPostUrl = new PostReferVacancyRequest(vacancyReference).PostUrl;

        var review = new VacancyReview
        {
            Id = reviewId,
            Status = ReviewStatus.UnderReview,
            VacancyReference = vacancyReference,
            AutomatedQaOutcomeIndicators =
            [
                new RuleOutcome { Id = Guid.NewGuid(), Target = "Title" },
                new RuleOutcome { Id = Guid.NewGuid(), Target = "Description" },
                new RuleOutcome { Id = Guid.NewGuid(), Target = "Description" }
            ]
        };

        vacancyReviewQuery
            .Setup(x => x.GetAsync(reviewId))
            .ReturnsAsync(review);
        
        vacancyReviewValidator
            .Setup(x => x.Validate(It.IsAny<VacancyReview>()))
            .Returns(new ValidationResult());

        var command = new ReferVacancyReviewCommand(reviewId, "comment", [], [review.AutomatedQaOutcomeIndicators.First().Id]);

        // act
        await sut.Handle(command, CancellationToken.None);

        // assert
        outerApiClient.Verify(x => x.Post(It.Is<PostReferVacancyRequest>(request => request.PostUrl == expectedPostUrl)), Times.Once);
    }
}