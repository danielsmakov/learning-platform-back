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
            "https://x.test/a.png",
            "Nick",
            "1234",
            ProgramDifficultyTrack.Beginner));
        Assert.True(r.IsValid);
    }

    [Fact]
    public void CreateChildRequestValidator_rejects_age_out_of_range()
    {
        var v = new CreateChildRequestValidator();
        var r = v.Validate(new CreateChildRequest(
            Guid.NewGuid(),
            "Name",
            20,
            "https://x.test/a.png",
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
    public void UpdateParentRequestValidator_accepts_valid_email()
    {
        var v = new UpdateParentRequestValidator();
        var r = v.Validate(new UpdateParentRequest("parent@test.dev"));
        Assert.True(r.IsValid);
    }
}
