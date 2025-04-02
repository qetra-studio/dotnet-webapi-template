namespace RichWebApi.Mappers;

internal sealed record TypeAdapter(Action<object, object> Update, Func<object, object> Map, Func<IQueryable, IQueryable> Project);