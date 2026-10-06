using FluentValidation;
using Task.Api.Contracts;

namespace Task.Api.Validators;

public sealed class UpdateTaskRequestValidator
    : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        // Якщо title передано, він не може бути null, порожнім або складатися лише з пробілів.
        RuleFor(x => x.Title)
            .NotEmpty()
            .When(x => x.Title is not null)
            .WithMessage("Title cannot be empty.");

        // Якщо description передано, він не може бути null, порожнім або складатися лише з пробілів.
        RuleFor(x => x.Description)
            .NotEmpty()
            .When(x => x.Description is not null)
            .WithMessage("Description cannot be empty.");

        // Відхиляємо порожній запит, у якому немає жодного поля для оновлення.
        RuleFor(x => x)
            .Must(x => x.Title is not null
                       || x.Description is not null
                       || x.Type is not null
                       || x.Status is not null
                       || x.Priority is not null
                       || x.AssigneeId is not null)
            .WithMessage("At least one field must be provided for update.");
    }
}
