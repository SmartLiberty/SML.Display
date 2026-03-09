namespace SML.ExampleGrpc.Core.Validators;

using FluentValidation;
using Shared.Proto;

public class GrpcUpdateRequestValidator : AbstractValidator<GrpcUpdateRequest>
{
    public GrpcUpdateRequestValidator()
    {
        RuleFor(request => request).NotNull().WithMessage("request is mandatory");
        RuleFor(request => request.DisplayName).NotEmpty().WithMessage("exampleDisplayNameLocalizerKey : Display name is mandatory");
        RuleFor(request => request.Id).GreaterThan(0).WithMessage("exampleIdLocalizerKey : Id must be grater than 0");
    }
}