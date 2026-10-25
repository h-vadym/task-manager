using FluentValidation;
using Task.Api.Contracts;

namespace Task.Api.Validators;

// Перевіряє запит, що змінює лише статус задачі.
public sealed class ChangeTaskStatusRequestValidator
    : AbstractValidator<ChangeTaskStatusRequest>
{
    public ChangeTaskStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotNull()
            .WithMessage("Status is required.");
    }
}