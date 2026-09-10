using Moq;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Application.UseCases.SelectVehicle;
using SkyShift.Domain.Entities;
using SkyShift.Domain.Enums;
using SkyShift.Domain.Exceptions;
using Xunit;

namespace SkyShift.Tests.UseCases;

/// <summary>
/// Service-layer (Use Case) tests for the limited-resource logic, with
/// repositories mocked via Moq -- the specific, mandatory coverage the
/// course spec calls out: at least one test on the valid case and one on
/// the rejected case for the resource competed over (Vehicle).
///
/// These tests exercise SelectVehicleUseCase's orchestration and the real
/// Domain rule in Vehicle.Assign() (never mocked/reimplemented here) — only
/// the repositories and IUnitOfWork are mocked. They do not, and are not
/// meant to, prove the concurrency-race behavior itself: that needs a real
/// EF Core provider, not a Moq mock of an interface -- see
/// VehicleConcurrencyTests for that separately-required proof.
/// </summary>
public class SelectVehicleUseCaseTests
{
    private static Vehicle CreateAvailableVehicle()
    {
        return Vehicle.Create(isPersonalExperimental: false, createdAt: DateTime.UtcNow);
    }

    [Fact]
    public async Task ExecuteAsync_ValidCase_VehicleAvailable_AssignsAndSaves()
    {
        // Arrange
        const int userId = 1;
        var vehicle = CreateAvailableVehicle();

        var vehicleRepository = new Mock<IVehicleRepository>();
        vehicleRepository.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);

        var assignmentRepository = new Mock<IVehicleAssignmentRepository>();
        assignmentRepository.Setup(r => r.GetActiveForUserAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((VehicleAssignment?)null);

        var unitOfWork = new Mock<IUnitOfWork>();

        var useCase = new SelectVehicleUseCase(vehicleRepository.Object, assignmentRepository.Object, unitOfWork.Object);
        var request = new SelectVehicleRequest { VehicleId = vehicle.Id };

        // Act
        await useCase.ExecuteAsync(userId, request);

        // Assert -- the real Domain rule ran: the vehicle is now InUse.
        Assert.Equal(VehicleStatus.InUse, vehicle.Status);
        vehicleRepository.Verify(r => r.UpdateAsync(vehicle, It.IsAny<CancellationToken>()), Times.Once);
        assignmentRepository.Verify(r => r.AddAsync(It.Is<VehicleAssignment>(a => a.UserId == userId && a.VehicleId == vehicle.Id), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_RejectedCase_VehicleNotAvailable_ThrowsAndDoesNotSave()
    {
        // Arrange -- the vehicle is already taken (InUse), which is the
        // exact "someone else already holds the limited resource" case.
        const int userId = 1;
        var vehicle = CreateAvailableVehicle();
        vehicle.Assign(); // Available -> InUse, via the real Domain rule.

        var vehicleRepository = new Mock<IVehicleRepository>();
        vehicleRepository.Setup(r => r.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);

        var assignmentRepository = new Mock<IVehicleAssignmentRepository>();
        assignmentRepository.Setup(r => r.GetActiveForUserAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((VehicleAssignment?)null);

        var unitOfWork = new Mock<IUnitOfWork>();

        var useCase = new SelectVehicleUseCase(vehicleRepository.Object, assignmentRepository.Object, unitOfWork.Object);
        var request = new SelectVehicleRequest { VehicleId = vehicle.Id };

        // Act & Assert
        await Assert.ThrowsAsync<VehicleNotAvailableException>(() => useCase.ExecuteAsync(userId, request));

        // Rejected before any write: nothing should have been persisted.
        vehicleRepository.Verify(r => r.UpdateAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Never);
        assignmentRepository.Verify(r => r.AddAsync(It.IsAny<VehicleAssignment>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
