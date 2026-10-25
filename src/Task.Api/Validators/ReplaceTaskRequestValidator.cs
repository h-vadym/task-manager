using FluentValidation;
using Task.Api.Contracts;

namespace Task.Api.Validators;

// Перевіряє, що PUT-запит містить повний коректний стан задачі.
public sealed class ReplaceTaskRequestValidator
    : AbstractValidator<ReplaceTaskRequest>
{
    public ReplaceTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required.");

        RuleFor(x => x.Type)
            .NotNull()
            .WithMessage("Type is required.");

        RuleFor(x => x.Status)
            .NotNull()
            .WithMessage("Status is required.");

        RuleFor(x => x.Priority)
            .NotNull()
            .WithMessage("Priority is required.");
    }
}