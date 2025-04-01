using AutoMapper;

namespace RichWebApi.Mappers;

public interface IHasMapping
{
	static abstract void AddProfileMapping(Profile profile);
}