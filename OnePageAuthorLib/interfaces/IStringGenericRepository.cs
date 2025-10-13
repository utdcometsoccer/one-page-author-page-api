namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Generic repository interface for entities with string-based id.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    public interface IStringGenericRepository<TEntity>
    {
        /// <summary>
        /// Gets an entity by its string id.
        /// </summary>
        /// <param name="id">The string id of the entity.</param>
        /// <returns>The entity if found, otherwise null.</returns>
        Task<TEntity?> GetByIdAsync(string id);

        /// <summary>
        /// Gets all entities in the container.
        /// </summary>
        /// <returns>A list of all entities.</returns>
        Task<IList<TEntity>> GetAllAsync();

        /// <summary>
        /// Adds a new entity.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <returns>The added entity.</returns>
        Task<TEntity> AddAsync(TEntity entity);

        /// <summary>
        /// Updates an entity.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <returns>The updated entity.</returns>
        Task<TEntity> UpdateAsync(TEntity entity);

        /// <summary>
        /// Deletes an entity by its string id.
        /// </summary>
        /// <param name="id">The string id of the entity.</param>
        /// <returns>True if deleted, false otherwise.</returns>
        Task<bool> DeleteAsync(string id);
    }
}