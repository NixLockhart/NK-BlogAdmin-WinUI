using Blog_Manager.Models;
using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blog_Manager.Services.Api
{
    /// <summary>
    /// 分类API接口
    /// </summary>
    public interface ICategoryApi
    {
        [Get("/api/admin/categories")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<List<Category>>> GetCategoriesAsync();

        [Get("/api/admin/categories/{id}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<Category>> GetCategoryAsync(long id);

        [Post("/api/admin/categories")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<long>> CreateCategoryAsync([Body] CategorySaveRequest category);

        [Put("/api/admin/categories/{id}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> UpdateCategoryAsync(long id, [Body] CategorySaveRequest category);

        [Delete("/api/admin/categories/{id}")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> DeleteCategoryAsync(long id, [Query] bool deleteArticles = false);

        [Put("/api/admin/categories/sort")]
        [Headers("Authorization: Bearer")]
        Task<ApiResult<object>> UpdateCategoriesSortAsync([Body] List<long> categoryIds);
    }
}
