﻿using EddiDataDefinitions;

namespace EddiSpanshService
{
    public interface ISpanshService
    {
        NavWaypointCollection GetCarrierRoute(string currentSystem, string[] targetSystems, long usedCarrierCapacity,
            bool calculateTotalFuelRequired = true, string[] refuel_destinations = null);

        NavWaypointCollection GetGalaxyRoute(string currentSystem, string targetSystem, Ship ship,
            int? cargoCarriedTons = null, bool is_supercharged = false, bool use_supercharge = true,
            bool use_injections = false, bool exclude_secondary = false);
    }
}
