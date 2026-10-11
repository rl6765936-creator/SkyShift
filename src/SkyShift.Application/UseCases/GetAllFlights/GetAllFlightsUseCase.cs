using AutoMapper;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;

namespace SkyShift.Application.UseCases.GetAllFlights;

/// <summary>
/// Manager flight list with optional filters, paginated in the database through
/// IFlightRepository.GetPagedAsync. from/to filter by the business Date field, not CreatedAt.
/// Omitted or out-of-range page/pageSize values fall back to defaults instead of failing the
/// request.
/// </summary>
public class GetAllFlightsUseCase
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly IFlightRepository _flightRepository;
    private readonly IMapper _mapper;

    public GetAllFlightsUseCase(IFlightRepository flightRepository, IMapper mapper)
    {
        _flightRepository = flightRepository;
        _mapper = mapper;
    }

    public async Task<PagedResponse<FlightResponse>> ExecuteAsync(int? userId, int? vehicleId, DateTime? from, DateTime? to, int? page, int? pageSize, CancellationToken cancellationToken = default)
    {
        var effectivePage = page is >= 1 ? page.Value : 1;
        var effectivePageSize = pageSize is >= 1 and <= MaxPageSize ? pageSize.Value : DefaultPageSize;

        var (flights, totalCount) = await _flightRepository.GetPagedAsync(userId, vehicleId, from, to, effectivePage, effectivePageSize, cancellationToken);

        var items = flights
            .Select(f => _mapper.Map<FlightResponse>(f))
            .ToList();

        return new PagedResponse<FlightResponse>
        {
            Items = items,
            Page = effectivePage,
            PageSize = effectivePageSize,
            TotalCount = totalCount
        };
    }
}
