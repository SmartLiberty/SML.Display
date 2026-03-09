namespace SML.ExampleGrpc.Tests.Unit.Validators;

using Core.Validators;
using FluentValidation.TestHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shared.Proto;

[TestClass]
public class GrpcExampleRequestValidatorShould
{
    private readonly GrpcUpdateRequestValidator _validator;

    public GrpcExampleRequestValidatorShould()
    {
        _validator = new GrpcUpdateRequestValidator();
    }
        
    [TestMethod]
    public void HaveErrorWhen_DisplayName_IsEmpty()
    {
        var model = new GrpcUpdateRequest { Id = 0, DisplayName = "" };
        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(request => request.DisplayName).WithErrorCode("NotEmptyValidator");
    }

    [TestMethod]
    public void HaveErrorWhen_Id_IsZero()
    {
        var model = new GrpcUpdateRequest { Id = 0, DisplayName = "Test" };
        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(request => request.Id).WithErrorCode("GreaterThanValidator");
    }
        
    [TestMethod]
    public void HaveErrorWhen_Id_IsNegative()
    {
        var model = new GrpcUpdateRequest { Id = -1, DisplayName = "Test" };
        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(request => request.Id).WithErrorCode("GreaterThanValidator");
    }
        
    [TestMethod]
    public void NotHaveErrorWhen_ObjectIsValid()
    {
        var model = new GrpcUpdateRequest { Id = 1, DisplayName = "Test" };
        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(request => request.Id);
    }
}