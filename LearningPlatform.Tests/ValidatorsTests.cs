using LearningPlatform.Application;
using LearningPlatform.Domain;
using Xunit;

namespace LearningPlatform.Tests;

public class ValidatorsTests
{
    [Fact]
    public void RegisterRequestValidator_accepts_valid_email_and_password()
    {
        var v = new RegisterRequestValidator();
        var r = v.Validate(new RegisterRequest("a@b.co", "password1"));
        Assert.True(r.IsValid);
    }

    [Fact]
    public void RegisterRequestValidator_rejects_invalid_email()
    {
        var v = new RegisterRequestValidator();
        var r = v.Validate(new RegisterRequest("not-an-email", "password1"));
        Assert.False(r.IsValid);
    }

    [Fact]
    public void RegisterRequestValidator_rejects_short_password()
    {
        var v = new RegisterRequestValidator();
        var r = v.Validate(new RegisterRequest("a@b.co", "short"));
        Assert.False(r.IsValid);
    }

    [Fact]
    public void ChildLoginRequestValidator_accepts_four_digit_pin()
    {
        var v = new ChildLoginRequestValidator();
        var r = v.Validate(new ChildLoginRequest(Guid.NewGuid(), "1234"));
        Assert.True(r.IsValid);
    }

    [Fact]
    public void ChildLoginRequestValidator_rejects_non_digits_in_pin()
    {
        var v = new ChildLoginRequestValidator();
        var r = v.Validate(new ChildLoginRequest(Guid.NewGuid(), "12ab"));
        Assert.False(r.IsValid);
    }

    [Fact]
    public void CreateChildRequestValidator_accepts_valid_age_and_track()
    {
        var v = new CreateChildRequestValidator();
        var r = v.Validate(new CreateChildRequest(
            Guid.NewGuid(),
            "Name",
            8,
            "avatar-03",
            "Nick",
            "1234",
            ProgramDifficultyTrack.Beginner));
        Assert.True(r.IsValid);
    }

    [Fact]
    public void CreateChildRequestValidator_rejects_invalid_avatar_id()
    {
        var v = new CreateChildRequestValidator();
        var r = v.Validate(new CreateChildRequest(
            Guid.NewGuid(),
            "Name",
            8,
            "https://example.com/a.png",
            "Nick",
            "1234",
            ProgramDifficultyTrack.Beginner));
        Assert.False(r.IsValid);
    }

    [Fact]
    public void CreateChildRequestValidator_rejects_age_out_of_range()
    {
        var v = new CreateChildRequestValidator();
        var r = v.Validate(new CreateChildRequest(
            Guid.NewGuid(),
            "Name",
            20,
            "avatar-01",
            "Nick",
            "1234"));
        Assert.False(r.IsValid);
    }

    [Fact]
    public void MarkNotificationsReadRequestValidator_rejects_empty_list()
    {
        var v = new MarkNotificationsReadRequestValidator();
        var r = v.Validate(new MarkNotificationsReadRequest([]));
        Assert.False(r.IsValid);
    }

    [Fact]
    public void ChangeParentPasswordRequestValidator_accepts_valid_passwords()
    {
        var v = new ChangeParentPasswordRequestValidator();
        var r = v.Validate(new ChangeParentPasswordRequest("oldpass1", "newpass12"));
        Assert.True(r.IsValid);
    }

    [Fact]
    public void ChangeParentPasswordRequestValidator_rejects_short_new_password()
    {
        var v = new ChangeParentPasswordRequestValidator();
        var r = v.Validate(new ChangeParentPasswordRequest("oldpass1", "short"));
        Assert.False(r.IsValid);
    }

    [Fact]
    public void UpdateParentRequestValidator_accepts_valid_email()
    {
        var v = new UpdateParentRequestValidator();
        var r = v.Validate(new UpdateParentRequest("parent@test.dev"));
        Assert.True(r.IsValid);
    }

    [Fact]
    public void CreateUnitRequestValidator_requires_positive_order_index()
    {
        var v = new CreateUnitRequestValidator();
        var bad = v.Validate(new CreateUnitRequest(
            Guid.NewGuid(),
            "T",
            "",
            OrderIndex: 0,
            IsPublished: true));
        Assert.False(bad.IsValid);

        var ok = v.Validate(new CreateUnitRequest(
            Guid.NewGuid(),
            "T",
            "",
            OrderIndex: 1,
            IsPublished: true));
        Assert.True(ok.IsValid);
    }

    [Fact]
    public void SubmitExerciseRequestValidator_enforces_time_bounds()
    {
        var v = new SubmitExerciseRequestValidator();
        Assert.False(v.Validate(new SubmitExerciseRequest(Guid.NewGuid(), true, 50)).IsValid);
        Assert.True(v.Validate(new SubmitExerciseRequest(Guid.NewGuid(), true, 100)).IsValid);
    }

    [Fact]
    public void CompleteLessonRequestValidator_rejects_empty_child_id()
    {
        var v = new CompleteLessonRequestValidator();
        Assert.False(v.Validate(new CompleteLessonRequest(Guid.Empty)).IsValid);
    }

    [Fact]
    public void CreateExerciseRequestValidator_requires_content_and_order()
    {
        var v = new CreateExerciseRequestValidator();
        Assert.False(v.Validate(new CreateExerciseRequest(LessonType.Phonics, 0, "{}")).IsValid);
        Assert.True(v.Validate(new CreateExerciseRequest(LessonType.Phonics, 1, "{}")).IsValid);
    }
}
