namespace Oypa.Crm.Contracts.Categories;

public sealed record SetCompanyCategoriesRequest(IReadOnlyList<Guid> CategoryIds);
