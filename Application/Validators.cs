using FluentValidation;
using LearningPlatform.Domain;

namespace LearningPlatform.Application;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MinimumLength(20);
    }
}

public class ChildLoginRequestValidator : AbstractValidator<ChildLoginRequest>
{
    public ChildLoginRequestValidator()
    {
        RuleFor(x => x.ChildId).NotEmpty();
        RuleFor(x => x.Pin).NotEmpty().Length(4, 8).Matches("^[0-9]+$");
    }
}

public class CreateChildRequestValidator : AbstractValidator<CreateChildRequest>
{
    public CreateChildRequestValidator()
    {
        RuleFor(x => x.ParentId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Age).InclusiveBetween(3, 14);
        RuleFor(x => x.Pin).NotEmpty().Length(4, 8).Matches("^[0-9]+$");
        RuleFor(x => x.LearningProgramTrack).IsInEnum();
    }
}

public class UpdateChildRequestValidator : AbstractValidator<UpdateChildRequest>
{
    public UpdateChildRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Age).InclusiveBetween(3, 14);
        When(x => x.LearningProgramTrack.HasValue, () =>
        {
            RuleFor(x => x.LearningProgramTrack!.Value).IsInEnum();
        });
    }
}

public class MarkNotificationsReadRequestValidator : AbstractValidator<MarkNotificationsReadRequest>
{
    public MarkNotificationsReadRequestValidator()
    {
        RuleFor(x => x.NotificationIds).NotNull();
        RuleFor(x => x.NotificationIds.Count).GreaterThan(0);
    }
}

public class UpdateParentRequestValidator : AbstractValidator<UpdateParentRequest>
{
    public UpdateParentRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class CreateLearningProgramRequestValidator : AbstractValidator<CreateLearningProgramRequest>
{
    public CreateLearningProgramRequestValidator()
    {
        RuleFor(x => x.DifficultyTrack).IsInEnum();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public class UpdateLearningProgramRequestValidator : AbstractValidator<UpdateLearningProgramRequest>
{
    public UpdateLearningProgramRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public class CreateUnitRequestValidator : AbstractValidator<CreateUnitRequest>
{
    public CreateUnitRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.OrderIndex).GreaterThan(0);
        RuleFor(x => x.ProgramId).NotEmpty();
    }
}

public class UpdateUnitRequestValidator : AbstractValidator<UpdateUnitRequest>
{
    public UpdateUnitRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.OrderIndex).GreaterThan(0);
        RuleFor(x => x.ProgramId).Must(id => !id.HasValue || id.Value != Guid.Empty).WithMessage("ProgramId must be a non-empty GUID when set.");
    }
}

public class CreateLessonRequestValidator : AbstractValidator<CreateLessonRequest>
{
    public CreateLessonRequestValidator()
    {
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.OrderIndex).GreaterThan(0);
        RuleFor(x => x.XpReward).InclusiveBetween(1, 200);
    }
}

public class UpdateLessonRequestValidator : AbstractValidator<UpdateLessonRequest>
{
    public UpdateLessonRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.OrderIndex).GreaterThan(0);
        RuleFor(x => x.XpReward).InclusiveBetween(1, 200);
    }
}

public class CreateExerciseRequestValidator : AbstractValidator<CreateExerciseRequest>
{
    public CreateExerciseRequestValidator()
    {
        RuleFor(x => x.OrderIndex).GreaterThan(0);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(5000);
    }
}

public class UpdateExerciseRequestValidator : AbstractValidator<UpdateExerciseRequest>
{
    public UpdateExerciseRequestValidator()
    {
        RuleFor(x => x.OrderIndex).GreaterThan(0);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(5000);
    }
}

public class SubmitExerciseRequestValidator : AbstractValidator<SubmitExerciseRequest>
{
    public SubmitExerciseRequestValidator()
    {
        RuleFor(x => x.ChildId).NotEmpty();
        RuleFor(x => x.TimeTakenMs).InclusiveBetween(100, 600000);
    }
}

public class CompleteLessonRequestValidator : AbstractValidator<CompleteLessonRequest>
{
    public CompleteLessonRequestValidator()
    {
        RuleFor(x => x.ChildId).NotEmpty();
    }
}
