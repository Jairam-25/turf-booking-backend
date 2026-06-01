using Application.DTOs;
using Domain.Entities;
using Mapster;

namespace Infrastructure.Configuration;

public class MappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<User, LoginResponseDto>()
            .Map(dest => dest.Number, src => src.PhoneNumber);
    }
}
