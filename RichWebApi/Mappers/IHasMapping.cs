using AutoMapper;

namespace RichWebApi.Mappers;

public interface IHasMapping
{
	public static abstract void AddProfileMapping(Profile profile);
}