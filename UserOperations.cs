// 0. IEntity Interface (Optional)
public interface IEntity
{
    int Id { get; set; }
}

// 1. Sample Entity Class
public class User : IEntity
{
    public int Id { get; set; }
    public string Username { get; set; }
    public int Age { get; set; }
    public bool IsActive { get; set; }
}

// 2. IUserRepository Interface (Optional)
public interface IUserRepository : IRepository<User>
{
    // Additional methods specific to the User entity, if needed
}

// 3. UserRepository Implementation (Optional)
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(DbContext context) : base(context)
    {
    }

    // Implement additional methods if needed
}

// 4. UserDTO (Data Transfer Object)
public class UserDTO
{
    public int Id { get; set; }
    public string Username { get; set; }
    public int Age { get; set; }
}

// 5. IUnitOfWork Interface
public interface IUnitOfWork
{
    Task CommitAsync();
    void Rollback();
}

// 6. UnitOfWork Implementation
public class UnitOfWork : IUnitOfWork
{
    private readonly DbContext _context;

    public UnitOfWork(DbContext context)
    {
        _context = context;
    }

    public async Task CommitAsync()
    {
        await _context.SaveChangesAsync();
    }

    public void Rollback()
    {
        // Implement rollback logic if needed.
    }
}

// 7. Updated IRepository Interface with Filtering and Paging
public interface IRepository<T> where T : IEntity
{
    Task<T> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate);
    Task<IEnumerable<T>> GetPagedAsync(Expression<Func<T, bool>> predicate, int pageNumber, int pageSize);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);

    void SetUnitOfWork(IUnitOfWork unitOfWork);

    // New method to project entities to DTOs
    Task<IEnumerable<TDto>> ProjectToAsync<TDto>(Expression<Func<T, bool>> predicate);
}

// 8. Updated Repository<T> Class with Unit of Work and Filtering/Paging
public class Repository<T> : IRepository<T> where T : class, IEntity
{
    private readonly DbContext _context;
    private IUnitOfWork _unitOfWork;

    public Repository(DbContext context)
    {
        _context = context;
    }

    public void SetUnitOfWork(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<T>> GetPagedAsync(Expression<Func<T, bool>> predicate, int pageNumber, int pageSize)
    {
        var query = _context.Set<T>().Where(predicate);
        var result = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return result;
    }

    // New method to project entities to DTOs using AutoMapper
    public async Task<IEnumerable<TDto>> ProjectToAsync<TDto>(Expression<Func<T, bool>> predicate)
    {
        var entities = await _context.Set<T>().Where(predicate).ToListAsync();
        return AutoMapper.Mapper.Map<IEnumerable<TDto>>(entities);
    }

    // ... (existing methods)

    public async Task AddAsync(T entity)
    {
        _context.Set<T>().Add(entity);
        await _unitOfWork.CommitAsync();
    }

    // ... (existing methods)
}

// 9. AutoMapper Configuration
public class AutoMapperConfig : Profile
{
    public AutoMapperConfig()
    {
        CreateMap<User, UserDTO>();
    }
}

// 10. Sample Service Class
public class UserService
{
    private readonly IRepository<User> _userRepository;

    public UserService(IRepository<User> userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<IEnumerable<UserDTO>> GetActiveUsers()
    {
        try
        {
            if (_userRepository == null)
            {
                throw new InvalidOperationException("User repository is not initialized.");
            }

            // Project entities to DTOs using AutoMapper
            var activeUserDTOs = await _userRepository.ProjectToAsync<UserDTO>(u => u.IsActive);

            return activeUserDTOs;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetActiveUsers: {ex.Message}");
            throw;
        }
    }
}
