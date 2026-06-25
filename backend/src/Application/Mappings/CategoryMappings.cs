using Oypa.Crm.Contracts.Categories;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Application.Mappings;

public static class CategoryMappings
{
    public static CategoryDto ToDto(this Category c) => new(c.Id, c.Name, c.Color);
}
