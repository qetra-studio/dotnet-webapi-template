using JetBrains.Annotations;
using Microsoft.OpenApi.Models;
using RichWebApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RichWebApi.Swagger;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
internal sealed class Response400OperationFilter : IOperationFilter
{
	public void Apply(OpenApiOperation operation, OperationFilterContext context)
	{
		if (operation.Parameters.Count == 0 && operation.RequestBody is null)
		{
			return;
		}

		var schema = context.SchemaGenerator.GenerateSchema(typeof(ValidationResponseDto),
			context.SchemaRepository);
		var badRequestResponse = new OpenApiResponse
		{
			Description = "Bad request.",
			Content =
			{
				["application/json"] = new OpenApiMediaType
				{
					Schema = schema
				}
			},
		};

		operation.Responses.TryAdd("400", badRequestResponse);
	}
}