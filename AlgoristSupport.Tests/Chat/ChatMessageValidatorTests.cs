using AlgoristSupport.Chat;

namespace AlgoristSupport.Tests.Chat;

public class ChatMessageValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" \t\r\n \n")]
    public void Empty_or_whitespace_is_rejected (string? input)
    {
        var result = ChatMessageValidator.Validate(input, 500);

        Assert.False(result.IsValid);
        Assert.Equal(MessageValidationError.Empty, result.Error);
    }

    [Fact]
    public void Exactly_max_length_is_valid ()
    {
        var result = ChatMessageValidator.Validate(new string('a', 500), 500);

        Assert.True(result.IsValid);
        Assert.Null(result.Message);
    }

    [Fact]
    public void Max_plus_one_is_rejected_with_pt_br_message ()
    {
        var result = ChatMessageValidator.Validate(new string('a', 501), 500);

        Assert.False(result.IsValid);
        Assert.Equal(MessageValidationError.TooLong, result.Error);
        Assert.Equal("A mensagem excede o limite de 500 caracteres.", result.Message);
    }

    [Fact]
    public void Custom_limit_is_respected ()
    {
        Assert.True(ChatMessageValidator.Validate(new string('a', 200), 200).IsValid);

        var tooLong = ChatMessageValidator.Validate(new string('a', 201), 200);
        Assert.Equal(MessageValidationError.TooLong, tooLong.Error);
        Assert.Contains("200", tooLong.Message);
    }
}
