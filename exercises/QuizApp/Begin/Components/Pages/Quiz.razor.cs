using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;
using System.ComponentModel.DataAnnotations;

namespace QuizApp.Components.Pages;

public partial class Quiz(IChatClient chatClient) : ComponentBase
{
    private const string QuizSubject = "car makes and models";

    private ElementReference answerInput;
    private int numQuestions = 5;
    private int pointsScored = 0;

    private int currentQuestionNumber = 0;
    private string? currentQuestionText;
    private string? currentQuestionOutcome;
    private bool answerSubmitted;
    private bool DisableForm => currentQuestionText is null || answerSubmitted;

    [Required]
    public string? UserAnswer { get; set; }

    protected override Task OnInitializedAsync()
        => MoveToNextQuestionAsync();

    private async Task MoveToNextQuestionAsync()
    {
        // Can't move on until you answer the question and we mark it
        if (currentQuestionNumber > 0 && string.IsNullOrEmpty(currentQuestionOutcome))
        {
            return;
        }

        // Reset state for the next question
        currentQuestionNumber++;
        currentQuestionText = null;
        currentQuestionOutcome = null;
        answerSubmitted = false;
        UserAnswer = null;

        // TODO:
        //  - Ask the LLM for a question on the subject 'QuizSubject'
        //  - Assign the question text to 'currentQuestionText'
        //  - Make sure it doesn't repeat the previous questions

        var prompt = $"""
            Provide a quiz question about the following subject: {QuizSubject}
            Reply only with the question and no other text. Ask factual questions for which
            the answer only needs to be a single word or phrase.
            """;

        var response = await chatClient.CompleteAsync(prompt);
        currentQuestionText = response.Message.Text;
    }

    private async Task SubmitAnswerAsync()
    {
        // Prevent double-submission
        if (answerSubmitted)
        {
            return;
        }

        // Mark the answer
        answerSubmitted = true;

        // TODO:
        //  - Ask the LLM whether the answer 'UserAnswer' is correct for the question 'currentQuestionText'
        //  - If it's correct, increment 'pointsScored'
        //  - Set 'currentQuestionOutcome' to a string explaining why the answer is correct or incorrect
        var prompt = $"""
            You are marking quiz answers as correct or incorrect.
            The quiz subject is {QuizSubject}.
            The question is: {currentQuestionText}
            The student's answer is: {UserAnswer}

            Is the student's answer correct? Your answer must start with CORRECT: or INCORRECT:
            followed by an explanation or another remark about the question.
            Examples: CORRECT: And did you know, Jupiter is made of gas?
                      INCORRECT: The Riemann hypothesis is still unsolved.
            """;

        var response = await chatClient.CompleteAsync(prompt);

        currentQuestionOutcome = response.Message.Text!;

        // There's a better way to do this using structured output. We'll get to that later.
        if (currentQuestionOutcome.StartsWith("CORRECT"))
        {
            pointsScored++;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
        => await answerInput.FocusAsync();
}
