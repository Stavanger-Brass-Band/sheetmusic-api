using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SheetMusic.Api.CQRS.Query;
using System;
using System.Threading.Tasks;

namespace SheetMusic.Api.Controllers.ModelBinders
{
    public class MusicPartBinder : IModelBinder
    {
        private readonly IMediator mediator;

        public MusicPartBinder(IMediator mediator)
        {
            this.mediator = mediator;
        }

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext is null)
                throw new ArgumentNullException(nameof(bindingContext));

            var modelName = bindingContext.ModelName;

            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

            if (valueProviderResult == ValueProviderResult.None)
                return;

            bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);
            var value = valueProviderResult.FirstValue;

            if (string.IsNullOrEmpty(value))
                return;

            var result = await mediator.Send(new GetMusicPart(value));

            if (result is null)
            {
                bindingContext.ModelState.TryAddModelError(modelName, "Part was not found");
                return;
            }

            bindingContext.Result = ModelBindingResult.Success(result);
        }
    }
}
