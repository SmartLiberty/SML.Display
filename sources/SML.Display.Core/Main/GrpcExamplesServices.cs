namespace SML.Display.Core.Main;

using AutoMapper;
using Core.Database;
using Core.Exceptions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Proto;

/// <summary>
/// gRPC service for examples.
/// </summary>
public class GrpcExamplesServices : Examples.ExamplesBase
{
    private readonly ILogger<GrpcExamplesServices> _logger;

    private readonly DatabaseContext _dbContext;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="dbContext">Database context</param>
    /// <param name="mapper">Auto mapper.</param>
    public GrpcExamplesServices(ILogger<GrpcExamplesServices> logger, DatabaseContext dbContext, IMapper mapper)
    {
        _logger = logger;
        _logger.LogTrace("Begin");

        _dbContext = dbContext;
        _mapper = mapper;
    }

    /// <summary>
    /// Ping the gRPC service.
    /// </summary>
    /// <param name="request">Request to ping.</param>
    /// <param name="context">gRPC context.</param>
    /// <returns>gRPC response.</returns>
    public override Task<Empty> Ping(Empty request, ServerCallContext context)
    {
        _logger.LogTrace("Begin");
        return Task.FromResult(new Empty());
    }

    /// <summary>
    /// Create a new example.
    /// </summary>
    /// <param name="request">Request to create a new example.</param>
    /// <param name="context">gRPC context.</param>
    /// <returns>gRPC response.</returns>
    public override async Task<GrpcExampleResponse> Create(GrpcCreateRequest request, ServerCallContext context)
    {
        _logger.LogTrace("Begin");

        if (await _dbContext.Examples.Where(x => x.DisplayName.Equals(request.DisplayName)).AnyAsync())
        {
            throw new AlreadyExistException($"An example with the name '{request.DisplayName}' is already exist!")
            {
                AlreadyExistsElement = request.DisplayName
            };
        }

        var example = _mapper.Map<Data.Storable.Example>(request);

        await _dbContext.AddAsync(example);
        await _dbContext.SaveChangesAsync();

        return new GrpcExampleResponse
        {
            Example = _mapper.Map<GrpcExample>(example)
        };
    }

    /// <summary>
    /// Read an existing example.
    /// </summary>
    /// <param name="request">Request to read an existing example.</param>
    /// <param name="context">gRPC context.</param>
    /// <returns>gRPC response.</returns>
    public override async Task<GrpcExampleResponse> Read(GrpcIdRequest request, ServerCallContext context)
    {
        _logger.LogTrace("Begin");

        var example = await _dbContext.Examples.FindAsync(request.Id)
            ?? throw new NotFoundException($"Example[{request.Id}] does not exist!");

        return new GrpcExampleResponse
        {
            Example = _mapper.Map<GrpcExample>(example)
        };
    }

    /// <summary>
    /// Read all existing examples.
    /// </summary>
    /// <param name="request">Request to read all existing examples.</param>
    /// <param name="context">gRPC context.</param>
    /// <returns>gRPC response.</returns>
    public override async Task<GrpcExamplesListResponse> ReadAll(Empty request, ServerCallContext context)
    {
        _logger.LogTrace("Begin");

        var examples = await _dbContext.Examples.Where(x => !x.Archived).ToListAsync();

        var response = new GrpcExamplesListResponse();
        response.Examples.AddRange(_mapper.Map<IEnumerable<GrpcExample>>(examples));

        return response;
    }

    /// <summary>
    /// Update an existing example.
    /// </summary>
    /// <param name="request">Request to update an existing example.</param>
    /// <param name="context">gRPC context.</param>
    /// <returns>gRPC response.</returns>
    public override async Task<GrpcExampleResponse> Update(GrpcUpdateRequest request, ServerCallContext context)
    {
        _logger.LogTrace("Begin");

        var example = (await _dbContext.Examples.FindAsync(request.Id))
            ?? throw new NotFoundException($"Example[{request.Id}] does not exist!")
            {
                NotFoundElement = request.DisplayName
            };

        if (example.Archived)
        {
            throw new NotFoundException($"Example[{example.Id}] is archived!")
            {
                NotFoundElement = request.DisplayName
            };
        }

        if (await _dbContext.Examples.Where(x => x.Id != request.Id && x.DisplayName.Equals(request.DisplayName)).AnyAsync())
        {
            throw new AlreadyExistException($"An example with the name '{request.DisplayName}' is already exist!")
            {
                AlreadyExistsElement = request.DisplayName
            };
        }

        example.DisplayName = request.DisplayName;

        if ((await _dbContext.SaveChangesAsync()) == 0)
        {
            _logger.LogInformation("Example[{ExampleId}] does not need to be updated!", example.Id);
        }

        await _dbContext.SaveChangesAsync();

        return new GrpcExampleResponse
        {
            Example = _mapper.Map<GrpcExample>(example)
        };
    }

    /// <summary>
    /// Archive an existing example.
    /// </summary>
    /// <param name="request">Request to archive an existing example.</param>
    /// <param name="context">gRPC context.</param>
    /// <returns>gRPC response.</returns>
    public override async Task<GrpcExampleResponse> Archive(GrpcIdRequest request, ServerCallContext context)
    {
        _logger.LogTrace("Begin");
        var example = (await _dbContext.Examples.FindAsync(request.Id))
            ?? throw new NotFoundException($"Example[{request.Id}] does not exist!");
        if (example.Archived)
        {
            _logger.LogWarning("Example[{ExampleId}] is already archived!", request.Id);
        }
        else
        {
            example.Archived = true;
            await _dbContext.SaveChangesAsync();
        }
        return new GrpcExampleResponse
        {
            Example = _mapper.Map<GrpcExample>(example)
        };
    }
}
