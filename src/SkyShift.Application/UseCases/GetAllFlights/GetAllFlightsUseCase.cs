using AutoMapper;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;

namespace SkyShift.Application.UseCases.GetAllFlights;

/// <summary>
/// Manager flight monitoring, with optional filters, server-side paginated.
/// from/to filter by the business Date field (see
/// IFlightRepository.GetPagedAsync), matching how a Manager browsing
/// "flights in date range X" would expect it -- not by CreatedAt, which is
/// reserved for trend/audit statistics (GetFlightStatsUseCase).
///
/// page/pageSize are optional and validated/defaulted here rather than at
/// the API layer, consistent with how other request defaulting in this
/// codebase lives in Application: an omitted or out-of-range value falls
/// back to a sensible default instead of failing the request.
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
