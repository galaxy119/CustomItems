// <copyright file="EmpGrenade.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CustomItems.Items
{
    using System.Collections.Generic;
    using System.Linq;
    using Exiled.API.Extensions;
    using Exiled.API.Features;
    using Exiled.CustomItems.API;
    using Exiled.Events.EventArgs;
    using Exiled.Events.Handlers;
    using Interactables.Interobjects.DoorUtils;
    using MEC;
    using Map = Exiled.Events.Handlers.Map;
    using Player = Exiled.API.Features.Player;

    /// <inheritdoc />
    public class EmpGrenade : CustomGrenade
    {
        private static List<Room> lockedRooms079 = new List<Room>();

        /// <summary>
        /// A list of doors locked by the EMP Grenades.
        /// </summary>
        private readonly List<DoorVariant> lockedDoors = new List<DoorVariant>();

        /// <inheritdoc />
        public EmpGrenade(ItemType type, int itemId)
            : base(type, itemId)
        {
        }

        /// <inheritdoc/>
        public override string Name { get; } = Plugin.Singleton.Config.ItemConfigs.EmpCfg.Name;

        /// <inheritdoc/>
        public override SpawnProperties SpawnProperties { get; set; } =
            Plugin.Singleton.Config.ItemConfigs.EmpCfg.SpawnProperties;

        /// <inheritdoc/>
        public override string Description { get; } = Plugin.Singleton.Config.ItemConfigs.EmpCfg.Description;

        /// <inheritdoc/>
        protected override bool ExplodeOnCollision { get; } = Plugin.Singleton.Config.ItemConfigs.EmpCfg.ExplodeOnCollision;

        /// <inheritdoc/>
        protected override float FuseTime { get; } = Plugin.Singleton.Config.ItemConfigs.EmpCfg.FuseDuration;

        /// <inheritdoc/>
        protected override void LoadEvents()
        {
            Scp079.ChangingCamera += OnChangingCamera;
            Scp079.InteractingDoor += OnInteractingDoor;
            Map.ExplodingGrenade += OnExplodingGrenade;
            base.LoadEvents();
        }

        /// <inheritdoc/>
        protected override void UnloadEvents()
        {
            Map.ExplodingGrenade -= OnExplodingGrenade;
            base.UnloadEvents();
        }

        private static void OnChangingCamera(ChangingCameraEventArgs ev)
        {
            Room room = ev.Camera.Room();
            if (room != null && lockedRooms079.Contains(room))
                ev.IsAllowed = false;
        }

        private void OnInteractingDoor(InteractingDoorEventArgs ev)
        {
            if (lockedDoors.Contains(ev.Door))
                ev.IsAllowed = false;
        }

        private void OnExplodingGrenade(ExplodingGrenadeEventArgs ev)
        {
            if (!CheckGrenade(ev.Grenade))
                return;

            ev.IsAllowed = false;

            Room room = Exiled.API.Features.Map.FindParentRoom(ev.Grenade);
            Log.Debug($"{ev.Grenade.transform.position} - {room.Position} - {Exiled.API.Features.Map.Rooms.Count}", Plugin.Singleton.Config.Debug);

            lockedRooms079.Add(room);
            room.TurnOffLights(Plugin.Singleton.Config.ItemConfigs.EmpCfg.Duration);
            Log.Debug($"{room.Doors.Count()} - {room.Type}", Plugin.Singleton.Config.Debug);
            foreach (DoorVariant door in room.Doors)
            {
                if (door.NetworkActiveLocks > 0 && !Plugin.Singleton.Config.ItemConfigs.EmpCfg.OpenLockedDoors)
                    continue;

                if (door.RequiredPermissions.RequiredPermissions != KeycardPermissions.None && !Plugin.Singleton.Config.ItemConfigs.EmpCfg.OpenKeycardDoors)
                    continue;

                Log.Debug("Opening a door!", Plugin.Singleton.Config.Debug);
                door.NetworkTargetState = true;
                door.ServerChangeLock(DoorLockReason.NoPower, true);
                if (lockedDoors.Contains(door))
                    lockedDoors.Add(door);

                Timing.CallDelayed(Plugin.Singleton.Config.ItemConfigs.EmpCfg.Duration, () =>
                {
                    door.ServerChangeLock(DoorLockReason.NoPower, false);
                    lockedDoors.Remove(door);
                });
            }

            foreach (Player player in Player.Get(RoleType.Scp079))
                if (player.Camera != null && player.Camera.Room() == room)
                        player.SetCamera(198);

            Timing.CallDelayed(Plugin.Singleton.Config.ItemConfigs.EmpCfg.Duration, () => lockedRooms079.Remove(room));
        }
    }
}