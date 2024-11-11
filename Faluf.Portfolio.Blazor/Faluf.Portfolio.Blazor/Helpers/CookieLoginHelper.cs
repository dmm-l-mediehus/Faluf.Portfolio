using System.Collections.Concurrent;

namespace Faluf.Portfolio.Blazor.Helpers;

public static class CookieLoginHelper
{
	public static ConcurrentQueue<(TokenDTO TokenDTO, bool IsPersisted)> CookieLoginQueue { get; private set; } = [];
}