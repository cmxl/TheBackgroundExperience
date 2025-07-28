using Mediator;
using TheBackgroundExperience.Domain.Common;

namespace TheBackgroundExperience.Application.Common.Interfaces;

public interface ICachableRequest<out TResponse> : IRequest<TResponse>, ICachable
{
	
}