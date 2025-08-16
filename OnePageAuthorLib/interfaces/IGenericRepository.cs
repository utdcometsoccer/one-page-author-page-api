namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Generic repository interface for entities with Guid-based id and authorId.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    public interface IGenericRepository<TEntity>
    {
        /// <summary>
        /// Gets an entity by its Guid id.
        /// </summary>
        /// <param name="id">The Guid id of the entity.</param>
        /// <returns>The entity if found, otherwise null.</returns>
        Task<TEntity?> GetByIdAsync(Guid id);

        /// <summary>
        /// Gets all entities for a given author Guid.
        /// </summary>
        /// <param name="authorId">The Guid authorId.</param>
        /// <returns>A list of entities for the author.</returns>
        Task<IList<TEntity>> GetByAuthorIdAsync(Guid authorId);

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
        /// Deletes an entity by its Guid id.
        /// </summary>
        /// <param name="id">The Guid id of the entity.</param>
        /// <returns>True if deleted, false otherwise.</returns>
        Task<bool> DeleteAsync(Guid id);
    }
}
