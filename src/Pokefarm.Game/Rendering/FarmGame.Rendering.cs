using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static Pokefarm.Game.BuildingWorkerHelpers;
using static Pokefarm.Game.WorkbenchCraftingHelpers;

namespace Pokefarm.Game;

// Main runtime type for farm Game, coordinating state and side effects for this feature.
public sealed partial class FarmGame
{
    // Draws farm for the current frame using the active render context.
    private void DrawFarm()
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        Color dirt = new(116, 82, 56);
        Color boundary = new(74, 49, 30);

        for (int y = 0; y < _worldBounds.Height; y += TileSize)
        {
            for (int x = 0; x < _worldBounds.Width; x += TileSize)
            {
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(x, y, TileSize, TileSize),
                    dirt);
            }
        }

        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, _worldBounds.Width, BorderThickness), boundary);
        _spriteBatch.Draw(_pixel, new Rectangle(0, _worldBounds.Height - BorderThickness, _worldBounds.Width, BorderThickness), boundary);
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, BorderThickness, _worldBounds.Height), boundary);
        _spriteBatch.Draw(_pixel, new Rectangle(_worldBounds.Width - BorderThickness, 0, BorderThickness, _worldBounds.Height), boundary);
    }

    // Draws active Dungeon Room for the current frame using the active render context.
    private void DrawActiveDungeonRoom()
    {
        if (_spriteBatch is null || _pixel is null || _activeDungeonRun is null)
        {
            return;
        }

        Color roomBackground = new Color(48, 48, 54);
        Color wallColor = new Color(26, 26, 30);
        Color accentColor = new Color(136, 136, 144);
        GeneratedDungeonRoom? activeRoom = GetActiveDungeonRoom();

        if (activeRoom is not null)
        {
            DungeonRoomTemplate template = activeRoom.Definition.Template;
            switch (activeRoom.Definition.Type)
            {
                case DungeonRoomType.Reward:
                    roomBackground = new Color(72, 86, 60);
                    wallColor = new Color(38, 44, 30);
                    accentColor = new Color(182, 206, 128);
                    break;
                case DungeonRoomType.Trap:
                    roomBackground = new Color(96, 64, 56);
                    wallColor = new Color(52, 30, 26);
                    accentColor = new Color(216, 130, 104);
                    break;
                case DungeonRoomType.Puzzle:
                    roomBackground = new Color(68, 72, 100);
                    wallColor = new Color(36, 40, 58);
                    accentColor = new Color(146, 166, 228);
                    break;
                case DungeonRoomType.Enemy:
                    roomBackground = new Color(90, 54, 94);
                    wallColor = new Color(46, 24, 52);
                    accentColor = new Color(196, 128, 210);
                    break;
            }

            int tileRenderSize = 48;
            int roomRenderWidth = template.Size.X * tileRenderSize;
            int roomRenderHeight = template.Size.Y * tileRenderSize;
            int roomStartX = _worldBounds.Center.X - (roomRenderWidth / 2);
            int roomStartY = _worldBounds.Center.Y - (roomRenderHeight / 2);

            for (int y = 0; y < _worldBounds.Height; y += TileSize)
            {
                for (int x = 0; x < _worldBounds.Width; x += TileSize)
                {
                    _spriteBatch.Draw(_pixel, new Rectangle(x, y, TileSize, TileSize), roomBackground);
                }
            }

            Rectangle roomBounds = new(roomStartX, roomStartY, roomRenderWidth, roomRenderHeight);
            _spriteBatch.Draw(_pixel, roomBounds, new Color(roomBackground.R, roomBackground.G, roomBackground.B, (byte)220));
            DrawPanelBorder(roomBounds, accentColor);

            for (int y = 0; y < template.Size.Y; y++)
            {
                for (int x = 0; x < template.Size.X; x++)
                {
                    char tile = template.LayoutRows[y][x];
                    Rectangle tileRect = new(
                        roomStartX + (x * tileRenderSize),
                        roomStartY + (y * tileRenderSize),
                        tileRenderSize,
                        tileRenderSize);

                    if (tile == '#')
                    {
                        _spriteBatch.Draw(_pixel, tileRect, wallColor);
                        DrawPanelBorder(tileRect, new Color(18, 18, 22));
                    }
                    else
                    {
                        _spriteBatch.Draw(_pixel, tileRect, new Color(roomBackground.R, roomBackground.G, roomBackground.B, (byte)160));
                    }
                }
            }

            foreach (DungeonObstacleDefinition obstacle in template.Obstacles)
            {
                Rectangle obstacleRect = new(
                    roomStartX + (obstacle.Position.X * tileRenderSize),
                    roomStartY + (obstacle.Position.Y * tileRenderSize),
                    obstacle.Size.X * tileRenderSize,
                    obstacle.Size.Y * tileRenderSize);
                _spriteBatch.Draw(_pixel, obstacleRect, new Color(accentColor.R, accentColor.G, accentColor.B, (byte)120));
                DrawPanelBorder(obstacleRect, accentColor);
            }

            DrawDungeonSpawnMarkers(template, roomStartX, roomStartY, tileRenderSize);
            return;
        }

        for (int y = 0; y < _worldBounds.Height; y += TileSize)
        {
            for (int x = 0; x < _worldBounds.Width; x += TileSize)
            {
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, TileSize, TileSize), roomBackground);
            }
        }

        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, _worldBounds.Width, BorderThickness), wallColor);
        _spriteBatch.Draw(_pixel, new Rectangle(0, _worldBounds.Height - BorderThickness, _worldBounds.Width, BorderThickness), wallColor);
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, BorderThickness, _worldBounds.Height), wallColor);
        _spriteBatch.Draw(_pixel, new Rectangle(_worldBounds.Width - BorderThickness, 0, BorderThickness, _worldBounds.Height), wallColor);
    }

    // Draws dungeon Spawn Markers for the current frame using the active render context.
    private void DrawDungeonSpawnMarkers(DungeonRoomTemplate template, int roomStartX, int roomStartY, int tileRenderSize)
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        foreach (DungeonSpawnPoint spawnPoint in template.SpawnPoints)
        {
            Color markerColor = spawnPoint.Type switch
            {
                DungeonSpawnPointType.PlayerStart => new Color(120, 220, 120),
                DungeonSpawnPointType.Exit => new Color(220, 220, 120),
                DungeonSpawnPointType.EnemySpawn => new Color(220, 120, 120),
                DungeonSpawnPointType.RewardSpawn => new Color(120, 220, 220),
                DungeonSpawnPointType.PuzzleAnchor => new Color(160, 160, 240),
                DungeonSpawnPointType.TrapAnchor => new Color(220, 150, 110),
                _ => new Color(200, 200, 200)
            };

            int markerSize = Math.Max(6, tileRenderSize / 4);
            Rectangle markerRect = new(
                roomStartX + (spawnPoint.Position.X * tileRenderSize) + ((tileRenderSize - markerSize) / 2),
                roomStartY + (spawnPoint.Position.Y * tileRenderSize) + ((tileRenderSize - markerSize) / 2),
                markerSize,
                markerSize);
            _spriteBatch.Draw(_pixel, markerRect, markerColor);
        }
    }

    // Draws placed Items for the current frame using the active render context.
    private void DrawPlacedItems()
    {
        if (_spriteBatch is null || _pixel is null || _circleTexture is null)
        {
            return;
        }

        foreach (PlacedItem item in _placedItems)
        {
            if (_isHitboxDisplayMode && item.Definition == ItemCatalog.Bed)
            {
                DrawBedHomeRange(item);
            }

            Texture2D texture = (item.Definition.IsBuildingLike || item.Definition.Kind == ItemKind.Snack) && item.Definition.HasCollision
                ? _pixel
                : _circleTexture;
            Color drawTint = item.IsConstructionSite
                ? new Color(152, 118, 82)
                : item.Definition.Tint;
            _spriteBatch.Draw(texture, item.Bounds, drawTint);
            DrawPanelBorder(item.Bounds, new Color(40, 28, 20));

            if (item.Definition == ItemCatalog.Pc && !_storyManager.TutorialStarted)
            {
                DrawPcTutorialMarker(item);
            }

            if (item.IsConstructionSite)
            {
                DrawConstructionSiteMarker(item);
                DrawConstructionProgressCircle(item);
            }

            Rectangle exitBounds = GetResourceBuildingExitBounds(item);
            if (!exitBounds.IsEmpty)
            {
                DrawResourceExitMarker(item);
            }

            if (item.Definition.IsResourceProduction)
            {
                DrawProductionProgressCircle(item);
            }
            else if (item.Definition == ItemCatalog.WorkBench)
            {
                DrawWorkbenchCraftingProgressCircle(item);
            }

            bool hasAssignedConstructionWorkers = item.IsConstructionSite &&
                                                  item.ConstructionSiteId.HasValue &&
                                                  _spawnedDittos.Any(pokemon => pokemon.AssignedConstructionSiteId == item.ConstructionSiteId);
            if ((item.Definition.IsResourceProduction || item.Definition == ItemCatalog.WorkBench) &&
                GetWorkerPokemonNames(item).Count > 0 ||
                hasAssignedConstructionWorkers)
            {
                DrawBuildingWorkerIcons(item);
            }
        }
    }

    private void DrawPcTutorialMarker(PlacedItem pc)
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        Point textSize = MeasurePixelText("!", UiFontPixelSize, UiFontSpacing);
        Rectangle panel = new(
            pc.Bounds.Center.X - (textSize.X / 2) - 6,
            pc.Bounds.Y - 20,
            textSize.X + 12,
            textSize.Y + 8);
        _spriteBatch.Draw(_pixel, panel, UnclaimedMarkerBackground);
        DrawPanelBorder(panel, new Color(181, 138, 95));
        DrawPixelText("!", new Vector2(panel.X + 6, panel.Y + 4), UnclaimedMarkerText);
    }

    // Draws construction-site marker text and overlay so unfinished buildings are visually distinct.
    private void DrawConstructionSiteMarker(PlacedItem constructionSite)
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        Rectangle bannerBounds = new(
            constructionSite.Bounds.X + 2,
            constructionSite.Bounds.Y + 2,
            Math.Max(20, constructionSite.Bounds.Width - 4),
            14);
        _spriteBatch.Draw(_pixel, bannerBounds, new Color(44, 31, 23, 220));
        DrawPanelBorder(bannerBounds, new Color(215, 178, 124, 220));
        DrawPixelText("SITE", new Vector2(bannerBounds.X + 4, bannerBounds.Y + 3), new Color(236, 220, 196));
    }

    // Draws construction Progress Circle for the current frame using the active render context.
    private void DrawConstructionProgressCircle(PlacedItem constructionSite)
    {
        if (_spriteBatch is null || _pixel is null || _circleTexture is null || !constructionSite.IsConstructionSite)
        {
            return;
        }

        float requiredEffort = Math.Max(0.1f, constructionSite.Definition.ConstructionEffortRequired);
        float progress = MathHelper.Clamp(constructionSite.ConstructionEffort / requiredEffort, 0f, 1f);
        DrawProgressCircleAtBuildingCenter(constructionSite, progress, new Color(255, 226, 74, 235));
    }

    // Draws bed Home Range for the current frame using the active render context.
    private void DrawBedHomeRange(PlacedItem bed)
    {
        if (_spriteBatch is null || _circleTexture is null)
        {
            return;
        }

        int radius = (int)MathF.Ceiling(HomeWanderRadius);
        int diameter = radius * 2;
        Rectangle rangeBounds = new(
            bed.Bounds.Center.X - radius,
            bed.Bounds.Center.Y - radius,
            diameter,
            diameter);

        _spriteBatch.Draw(_circleTexture, rangeBounds, new Color(84, 166, 255, 46));
        DrawPanelBorder(rangeBounds, new Color(84, 166, 255, 168));
    }

    // Draws resource Exit Marker for the current frame using the active render context.
    private void DrawResourceExitMarker(PlacedItem building)
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        Rectangle exitBounds = GetResourceBuildingExitBounds(building);
        if (exitBounds.IsEmpty)
        {
            return;
        }

        _spriteBatch.Draw(_pixel, exitBounds, new Color(236, 220, 196, 65));
        DrawPanelBorder(exitBounds, new Color(181, 138, 95, 210));
        DrawPixelText("EXIT", new Vector2(exitBounds.X + 4, exitBounds.Y + 6), new Color(236, 220, 196));
    }

    // Draws production Progress Circle for the current frame using the active render context.
    private void DrawProductionProgressCircle(PlacedItem building)
    {
        if (_spriteBatch is null || _pixel is null || _circleTexture is null)
        {
            return;
        }

        if (building.Definition.EffortPerProducedUnit <= 0f || building.Definition.MaxStoredProducedUnits <= 0)
        {
            return;
        }

        float progress = building.StoredProducedUnits >= building.Definition.MaxStoredProducedUnits
            ? 1f
            : MathHelper.Clamp(building.StoredProductionEffort / building.Definition.EffortPerProducedUnit, 0f, 1f);

        Color fillColor = GetProductionProgressFillColor(building);
        DrawProgressCircleAtBuildingCenter(building, progress, fillColor);
    }

    // Draws workbench Crafting Progress Circle for the current frame using the active render context.
    private void DrawWorkbenchCraftingProgressCircle(PlacedItem workbench)
    {
        if (!HasWorkbenchQueuedItems(workbench) || workbench.WorkbenchCraftEffortRequired <= 0f)
        {
            return;
        }

        float completed = Math.Max(0f, workbench.WorkbenchCraftEffortRequired - workbench.WorkbenchCraftEffortRemaining);
        float progress = MathHelper.Clamp(completed / workbench.WorkbenchCraftEffortRequired, 0f, 1f);
        DrawProgressCircleAtBuildingCenter(workbench, progress, new Color(91, 188, 110, 235));
    }

    // Draws progress Circle At Building Center for the current frame using the active render context.
    private void DrawProgressCircleAtBuildingCenter(PlacedItem building, float progress, Color fillColor)
    {
        if (_spriteBatch is null || _pixel is null || _circleTexture is null)
        {
            return;
        }

        const int diameter = 20;
        int centerX = building.Bounds.Center.X;
        int centerY = building.Bounds.Center.Y;
        Rectangle circleBounds = new(centerX - (diameter / 2), centerY - (diameter / 2), diameter, diameter);

        _spriteBatch.Draw(_circleTexture, circleBounds, new Color(30, 20, 14, 210));

        int filledRows = (int)MathF.Round(diameter * progress);
        float radius = diameter / 2f;
        float radiusSquared = radius * radius;
        for (int row = 0; row < diameter; row++)
        {
            float dy = (row + 0.5f) - radius;
            float widthSquared = radiusSquared - (dy * dy);
            if (widthSquared <= 0f)
            {
                continue;
            }

            int halfWidth = (int)MathF.Floor(MathF.Sqrt(widthSquared));
            int rowStartX = centerX - halfWidth;
            int rowWidth = Math.Max(1, halfWidth * 2);
            int rowY = circleBounds.Y + row;

            if (row >= diameter - filledRows)
            {
                _spriteBatch.Draw(_pixel, new Rectangle(rowStartX, rowY, rowWidth, 1), fillColor);
            }
        }

        DrawPanelBorder(circleBounds, new Color(181, 138, 95));
    }

    // Computes and returns production Progress Fill Color without mutating persistent game state.
    private static Color GetProductionProgressFillColor(PlacedItem building)
    {
        if (building.Definition == ItemCatalog.Farm)
        {
            int stepIndex = Math.Clamp(building.ProductionStepIndex, 0, 2);
            return stepIndex switch
            {
                0 => new Color(91, 188, 110, 235),   // Planting
                1 => new Color(112, 188, 236, 235),  // Watering
                _ => new Color(166, 198, 86, 235)    // Harvesting
            };
        }

        return building.Definition.RequiredSkill switch
        {
            SkillType.Lumber => new Color(176, 123, 73, 235),
            SkillType.Farming => new Color(91, 188, 110, 235),
            SkillType.Crafting => new Color(104, 164, 214, 235),
            _ => new Color(91, 188, 110, 235)
        };
    }

    // Draws building Worker Icons for the current frame using the active render context.
    private void DrawBuildingWorkerIcons(PlacedItem building)
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        List<string> workerNames = building.IsConstructionSite && building.ConstructionSiteId.HasValue
            ? _spawnedDittos
                .Where(pokemon => pokemon.AssignedConstructionSiteId == building.ConstructionSiteId)
                .Select(pokemon => pokemon.Name)
                .ToList()
            : GetWorkerPokemonNames(building);
        const int iconSize = 16;
        const int spacing = 3;
        int totalWidth = (workerNames.Count * iconSize) + ((workerNames.Count - 1) * spacing);
        int startX = building.Bounds.Center.X - (totalWidth / 2);

        for (int index = 0; index < workerNames.Count; index++)
        {
            string workerName = workerNames[index];
            if (!TryGetPokemonIconTexture(workerName, out Texture2D? iconTexture))
            {
                continue;
            }

            Rectangle iconBounds = new(
                startX + (index * (iconSize + spacing)),
                building.Bounds.Bottom - (iconSize / 2),
                iconSize,
                iconSize);

            _spriteBatch.Draw(_pixel, iconBounds, new Color(30, 20, 14, 220));
            DrawPanelBorder(iconBounds, new Color(181, 138, 95));
            Rectangle innerBounds = new(iconBounds.X + 2, iconBounds.Y + 2, iconBounds.Width - 4, iconBounds.Height - 4);
            _spriteBatch.Draw(iconTexture, innerBounds, Color.White);
        }
    }

    // Draws placement Preview for the current frame using the active render context.
    private void DrawPlacementPreview()
    {
        if (_spriteBatch is null || _pixel is null || _circleTexture is null || _previewItem is null)
        {
            return;
        }

        Color tint = _previewPlacementValid
            ? new Color(_previewItem.Definition.Tint.R, _previewItem.Definition.Tint.G, _previewItem.Definition.Tint.B, (byte)150)
            : new Color((byte)220, (byte)80, (byte)80, (byte)150);

        Texture2D texture = (_previewItem.Definition.IsBuildingLike || _previewItem.Definition.Kind == ItemKind.Snack) && _previewItem.Definition.HasCollision
            ? _pixel
            : _circleTexture;
        _spriteBatch.Draw(texture, _previewItem.Bounds, tint);
        DrawPanelBorder(_previewItem.Bounds, _previewPlacementValid ? new Color(255, 245, 180) : new Color(180, 70, 70));

        if (_previewItem.Definition.IsBuildingLike)
        {
            DrawResourceExitMarker(_previewItem);
        }
    }

    // Draws spawned Dittos for the current frame using the active render context.
    private void DrawSpawnedDittos()
    {
        if (_spriteBatch is null)
        {
            return;
        }

        foreach (SpawnedPokemon pokemon in _spawnedDittos)
        {
            if (pokemon.IsWorking)
            {
                continue;
            }

            DrawPokemonAt(
                pokemon.Position,
                pokemon.Name,
                pokemon.Direction,
                isWalking: false,
                walkFrame: 0,
                idleFrame: pokemon.IsMoving ? 0 : pokemon.IdleAnimationFrame);
            if (_isHitboxDisplayMode)
            {
                DrawPokemonHitbox(pokemon);
            }
            DrawStatusMarker(pokemon);
        }
    }

    // Draws pokemon Hitbox for the current frame using the active render context.
    private void DrawPokemonHitbox(SpawnedPokemon pokemon)
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        Rectangle hitbox = new((int)pokemon.Position.X, (int)pokemon.Position.Y, PlayerSize, PlayerSize);
        _spriteBatch.Draw(_pixel, hitbox, new Color(220, 56, 56, 42));
        DrawPanelBorder(hitbox, new Color(220, 56, 56, 210));
    }

    // Draws status Marker for the current frame using the active render context.
    private void DrawStatusMarker(SpawnedPokemon pokemon)
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        if (pokemon.IsClaimed && !pokemon.ShowWorkBlockedMarker)
        {
            return;
        }

        Point textSize = MeasurePixelText("!", UiFontPixelSize, UiFontSpacing);
        int markerX = (int)pokemon.Position.X + (PlayerSize / 2);
        int markerY = (int)pokemon.Position.Y - 18;
        Rectangle panel = new(
            markerX - (textSize.X / 2) - 5,
            markerY - (textSize.Y / 2) - 3,
            textSize.X + 10,
            textSize.Y + 6);

        _spriteBatch.Draw(_pixel, panel, UnclaimedMarkerBackground);
        DrawPanelBorder(panel, new Color(181, 138, 95));
        DrawPixelText("!", new Vector2(panel.X + 5, panel.Y + 3), UnclaimedMarkerText);
    }

    // Draws removal Preview for the current frame using the active render context.
    private void DrawRemovalPreview()
    {
        if (_spriteBatch is null || _pixel is null || _inputMode != InputMode.Removal)
        {
            return;
        }

        _spriteBatch.Draw(_pixel, _removeSelectorBounds, new Color(255, 255, 255, 45));
        DrawPanelBorder(_removeSelectorBounds, new Color(255, 245, 180));

        if (_removeTarget is null)
        {
            return;
        }

        Color tint = new(255, 215, 90, 140);
        Texture2D texture = (_removeTarget.Definition.IsBuildingLike || _removeTarget.Definition.Kind == ItemKind.Snack) && _removeTarget.Definition.HasCollision
            ? _pixel
            : _circleTexture ?? _pixel;
        _spriteBatch.Draw(texture, _removeTarget.Bounds, tint);
        DrawPanelBorder(_removeTarget.Bounds, Color.Gold);
    }

    // Draws inventory Screen for the current frame using the active render context.
    private void DrawInventoryScreen()
    {
        if (_spriteBatch is null || _pixel is null || _circleTexture is null)
        {
            return;
        }

        Viewport viewport = GraphicsDevice.Viewport;
        Rectangle overlay = new(0, 0, viewport.Width, viewport.Height);
        Rectangle panel = new(viewport.Width / 2 - 360, viewport.Height / 2 - 200, 720, 400);

        _spriteBatch.Draw(_pixel, overlay, new Color(20, 14, 10, 200));
        _spriteBatch.Draw(_pixel, panel, new Color(44, 31, 23, 240));
        DrawPanelBorder(panel, new Color(181, 138, 95));

        int visibleSlots = InventoryColumns * InventoryRows;
        int scrollOffset = _inventoryVisibleStartIndex;

        for (int slotIndex = 0; slotIndex < visibleSlots; slotIndex++)
        {
            int column = slotIndex % InventoryColumns;
            int row = slotIndex / InventoryColumns;
            int entryIndex = scrollOffset + slotIndex;
            Rectangle slot = new(
                panel.X + 84 + (column * 140),
                panel.Y + 74 + (row * 140),
                96,
                96);
            bool hasItem = entryIndex < _inventoryItems.Count;
            bool selected = entryIndex == _selectedInventoryIndex;

            _spriteBatch.Draw(_pixel, slot, hasItem ? new Color(88, 66, 49) : new Color(58, 43, 33));
            DrawPanelBorder(slot, selected ? Color.Gold : new Color(120, 90, 65));

            if (hasItem)
            {
                InventoryEntry entry = _inventoryItems[entryIndex];
                int iconSize = Math.Min(64, Math.Max(entry.Definition.Size.X, entry.Definition.Size.Y));
                Rectangle iconBounds = new(
                    slot.Center.X - (iconSize / 2),
                    slot.Y + 22,
                    iconSize,
                    iconSize);
                Texture2D texture = (entry.Definition.IsBuildingLike || entry.Definition.Kind == ItemKind.Snack) && entry.Definition.HasCollision
                    ? _pixel
                    : _circleTexture;
                string label = entry.Definition.Name.ToUpperInvariant();
                Point labelSize = MeasurePixelText(label, UiFontPixelSize, UiFontSpacing);
                Vector2 labelPosition = new(slot.Center.X - (labelSize.X / 2f), slot.Y + 6);

                DrawPixelText(label, labelPosition, new Color(236, 220, 196));

                _spriteBatch.Draw(texture, iconBounds, entry.Definition.Tint);
                DrawPanelBorder(iconBounds, new Color(48, 36, 26));

                string quantityText = entry.Quantity.ToString();
                Point quantitySize = MeasurePixelText(quantityText, UiFontPixelSize, UiFontSpacing);
                Rectangle quantityBadge = new(
                    slot.Center.X - (quantitySize.X / 2) - 6,
                    slot.Bottom - quantitySize.Y - 10,
                    quantitySize.X + 12,
                    quantitySize.Y + 6);
                _spriteBatch.Draw(_pixel, quantityBadge, new Color(45, 33, 25, 220));
                DrawPanelBorder(quantityBadge, new Color(160, 130, 100));
                DrawPixelText(
                    quantityText,
                    new Vector2(quantityBadge.X + 6, quantityBadge.Y + 3),
                    new Color(236, 220, 196));
            }
        }

        if (scrollOffset > 0)
        {
            DrawTriangleIndicator(new Point(panel.Right - 32, panel.Y + 28), true, new Color(236, 220, 196));
        }

        if (scrollOffset + visibleSlots < _inventoryItems.Count)
        {
            DrawTriangleIndicator(new Point(panel.Right - 32, panel.Bottom - 28), false, new Color(236, 220, 196));
        }
    }

    // Draws crafting Screen for the current frame using the active render context.
    private void DrawCraftingScreen()
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        List<RecipeDefinition> activeRecipes = GetActiveRecipes();
        Viewport viewport = GraphicsDevice.Viewport;
        Rectangle overlay = new(0, 0, viewport.Width, viewport.Height);
        Rectangle panel = new(viewport.Width / 2 - 360, viewport.Height / 2 - 240, 720, 480);
        Rectangle listArea = new(panel.X + 36, panel.Y + 114, panel.Width - 72, panel.Height - 150);
        bool isGrowingMenu = _activeCraftingSource == CraftingSource.FarmGrowing;
        Color panelFill = isGrowingMenu ? new Color(35, 72, 42, 245) : new Color(44, 31, 23, 245);
        Color panelBorder = isGrowingMenu ? new Color(128, 190, 132) : new Color(181, 138, 95);
        Color overlayTint = isGrowingMenu ? new Color(14, 28, 16, 210) : new Color(20, 14, 10, 210);
        Color selectedRowFill = isGrowingMenu ? new Color(24, 56, 33) : new Color(88, 66, 49);
        Color unselectedRowFill = isGrowingMenu ? new Color(42, 88, 52) : new Color(58, 43, 33);
        Color unselectedRowBorder = isGrowingMenu ? new Color(102, 156, 106) : new Color(120, 90, 65);

        _spriteBatch.Draw(_pixel, overlay, overlayTint);
        _spriteBatch.Draw(_pixel, panel, panelFill);
        DrawPanelBorder(panel, panelBorder);
        DrawPixelText(GetCraftingTitle(), new Vector2(panel.X + 24, panel.Y + 18), new Color(236, 220, 196));

        if (_activeCraftingSource == CraftingSource.BasicWorkBenchCrafting &&
            _activeWorkbenchIndex >= 0 &&
            _activeWorkbenchIndex < _placedItems.Count &&
            _placedItems[_activeWorkbenchIndex].Definition == ItemCatalog.WorkBench)
        {
            PlacedItem workbench = _placedItems[_activeWorkbenchIndex];
            string queuedText = GetWorkbenchQueueStatusText(workbench);
            DrawPixelText(queuedText, new Vector2(panel.X + 24, panel.Y + 44), new Color(210, 190, 164));

            string completedText = HasWorkbenchStoredItems(workbench) && workbench.WorkbenchStoredItem is not null
                ? $"STORED: {workbench.WorkbenchStoredItem.Name.ToUpperInvariant()} X{workbench.WorkbenchStoredQuantity}/{GetWorkbenchStorageCapacity(workbench)}"
                : $"STORED: NONE 0/{GetWorkbenchStorageCapacity(workbench)}";
            DrawPixelText(completedText, new Vector2(panel.X + 24, panel.Y + 62), new Color(196, 226, 180));
        }
        else if (_activeCraftingSource == CraftingSource.FarmGrowing &&
                 _activeFarmIndex >= 0 &&
                 _activeFarmIndex < _placedItems.Count &&
                 _placedItems[_activeFarmIndex].Definition == ItemCatalog.Farm)
        {
            PlacedItem farm = _placedItems[_activeFarmIndex];
            ItemDefinition currentPlant = farm.FarmGrowingPlant ?? ItemCatalog.NoBerry;
            DrawPixelText($"CURRENT PLANT: {currentPlant.Name.ToUpperInvariant()}", new Vector2(panel.X + 24, panel.Y + 44), new Color(210, 190, 164));
            DrawPixelText("SPACE SETS SELECTED PLANT", new Vector2(panel.X + 24, panel.Y + 62), new Color(196, 226, 180));
        }

        if (activeRecipes.Count == 0)
        {
            DrawPixelText("NONE", new Vector2(panel.X + 24, panel.Y + 64), new Color(210, 180, 152));
            return;
        }

        int rowHeight = 92;
        int visibleRows = Math.Max(1, listArea.Height / rowHeight);
        int scrollOffset = Math.Clamp(_selectedCraftingIndex - visibleRows + 1, 0, Math.Max(0, activeRecipes.Count - visibleRows));

        for (int visibleIndex = 0; visibleIndex < visibleRows; visibleIndex++)
        {
            int recipeIndex = scrollOffset + visibleIndex;
            if (recipeIndex >= activeRecipes.Count)
            {
                break;
            }

            RecipeDefinition recipe = activeRecipes[recipeIndex];
            Rectangle rowBounds = new(
                listArea.X,
                listArea.Y + (visibleIndex * rowHeight),
                listArea.Width,
                rowHeight - 10);
            bool selected = recipeIndex == _selectedCraftingIndex;
            bool canCraft = CanCraftRecipe(recipe);

            _spriteBatch.Draw(_pixel, rowBounds, selected ? selectedRowFill : unselectedRowFill);
            DrawPanelBorder(rowBounds, selected ? Color.Gold : unselectedRowBorder);

            DrawPixelText(recipe.Output.Name.ToUpperInvariant(), new Vector2(rowBounds.X + 16, rowBounds.Y + 12), new Color(236, 220, 196));
            string inventoryText = $"IN INVENTORY {GetInventoryQuantity(recipe.Output)}";
            Point inventoryTextSize = MeasurePixelText(inventoryText);
            DrawPixelText(
                inventoryText,
                new Vector2(rowBounds.Right - inventoryTextSize.X - 16, rowBounds.Y + 12),
                new Color(210, 190, 164));

            for (int costIndex = 0; costIndex < recipe.Costs.Count; costIndex++)
            {
                RecipeCost cost = recipe.Costs[costIndex];
                int ownedAmount = GetInventoryQuantity(cost.Item);
                Color costColor = ownedAmount >= cost.Quantity ? new Color(196, 226, 180) : new Color(236, 180, 164);
                string costText = $"{cost.Item.Name.ToUpperInvariant()} {ownedAmount}/{cost.Quantity}";
                DrawPixelText(costText, new Vector2(rowBounds.X + 16, rowBounds.Y + 36 + (costIndex * 18)), costColor);
            }

            DrawPixelText(
                canCraft ? "SPACE MAKE" : "NEED ITEMS",
                new Vector2(rowBounds.Right - 142, rowBounds.Y + 34),
                canCraft ? new Color(224, 210, 158) : new Color(180, 130, 110));
        }
    }

    // Draws chest storage screen with chest and inventory panes so items can be transferred between them.
    private void DrawChestStorageScreen()
    {
        if (_spriteBatch is null || _pixel is null || _circleTexture is null)
        {
            return;
        }

        List<InventoryEntry> chestEntries = GetActiveChestStoredItems();
        int chestCapacity = 0;
        if (_activeChestIndex >= 0 &&
            _activeChestIndex < _placedItems.Count &&
            _placedItems[_activeChestIndex].Definition == ItemCatalog.Chest)
        {
            chestCapacity = _placedItems[_activeChestIndex].Definition.StorageCapacity;
        }

        Viewport viewport = GraphicsDevice.Viewport;
        Rectangle overlay = new(0, 0, viewport.Width, viewport.Height);
        Rectangle panel = new(viewport.Width / 2 - 420, viewport.Height / 2 - 260, 840, 520);
        Rectangle chestPanel = new(panel.X + 24, panel.Y + 74, (panel.Width / 2) - 36, panel.Height - 118);
        Rectangle inventoryPanel = new(chestPanel.Right + 24, panel.Y + 74, (panel.Width / 2) - 36, panel.Height - 118);

        _spriteBatch.Draw(_pixel, overlay, new Color(14, 18, 22, 215));
        _spriteBatch.Draw(_pixel, panel, new Color(32, 40, 48, 245));
        DrawPanelBorder(panel, new Color(132, 164, 184));
        DrawPixelText("CHEST STORAGE", new Vector2(panel.X + 24, panel.Y + 18), new Color(226, 234, 240));
        DrawPixelText("A/D SWITCH  W/S SELECT  SPACE TRANSFER  E CLOSE", new Vector2(panel.X + 24, panel.Y + 42), new Color(196, 210, 220));

        _spriteBatch.Draw(_pixel, chestPanel, new Color(44, 62, 76));
        DrawPanelBorder(chestPanel, _isChestSelectionOnChest ? Color.Gold : new Color(120, 150, 170));
        _spriteBatch.Draw(_pixel, inventoryPanel, new Color(52, 48, 44));
        DrawPanelBorder(inventoryPanel, !_isChestSelectionOnChest ? Color.Gold : new Color(132, 116, 98));

        DrawPixelText("CHEST", new Vector2(chestPanel.X + 12, chestPanel.Y + 10), new Color(224, 232, 236));
        DrawPixelText($"{chestEntries.Count}/{Math.Max(0, chestCapacity)} STACKS", new Vector2(chestPanel.X + 12, chestPanel.Y + 30), new Color(196, 210, 220));
        DrawPixelText("INVENTORY", new Vector2(inventoryPanel.X + 12, inventoryPanel.Y + 10), new Color(236, 220, 196));
        DrawPixelText($"{_inventoryItems.Count}/{_inventoryCapacity} STACKS", new Vector2(inventoryPanel.X + 12, inventoryPanel.Y + 30), new Color(210, 190, 164));

        DrawStorageEntryList(chestPanel, chestEntries, _selectedChestStorageIndex, _isChestSelectionOnChest, new Color(74, 108, 132), new Color(58, 84, 102));
        DrawStorageEntryList(inventoryPanel, _inventoryItems, _selectedChestInventoryIndex, !_isChestSelectionOnChest, new Color(116, 90, 60), new Color(90, 70, 48));
    }

    // Draws one pane's storage entries and highlights the selected row.
    private void DrawStorageEntryList(
        Rectangle panel,
        List<InventoryEntry> entries,
        int selectedIndex,
        bool paneSelected,
        Color selectedFill,
        Color unselectedFill)
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        int rowHeight = 32;
        int listTop = panel.Y + 56;
        int maxRows = Math.Max(1, (panel.Height - 68) / rowHeight);
        int clampedSelection = entries.Count <= 0 ? 0 : Math.Clamp(selectedIndex, 0, entries.Count - 1);
        int scrollOffset = Math.Clamp(clampedSelection - maxRows + 1, 0, Math.Max(0, entries.Count - maxRows));

        if (entries.Count <= 0)
        {
            DrawPixelText("EMPTY", new Vector2(panel.X + 14, panel.Y + 64), new Color(196, 202, 208));
            return;
        }

        for (int visibleIndex = 0; visibleIndex < maxRows; visibleIndex++)
        {
            int entryIndex = scrollOffset + visibleIndex;
            if (entryIndex >= entries.Count)
            {
                break;
            }

            InventoryEntry entry = entries[entryIndex];
            Rectangle row = new(panel.X + 10, listTop + (visibleIndex * rowHeight), panel.Width - 20, rowHeight - 4);
            bool selected = entryIndex == clampedSelection;
            _spriteBatch.Draw(_pixel, row, selected ? selectedFill : unselectedFill);
            DrawPanelBorder(row, selected && paneSelected ? Color.Gold : new Color(148, 136, 120));

            DrawPixelText(entry.Definition.Name.ToUpperInvariant(), new Vector2(row.X + 10, row.Y + 8), new Color(236, 220, 196));
            string quantityText = $"X{entry.Quantity}";
            Point quantitySize = MeasurePixelText(quantityText);
            DrawPixelText(quantityText, new Vector2(row.Right - quantitySize.X - 10, row.Y + 8), new Color(236, 220, 196));
        }
    }

    // Builds a compact queue summary that shows each visible slot and quantity on the active workbench.
    private static string GetWorkbenchQueueStatusText(PlacedItem workbench)
    {
        int capacity = GetWorkbenchQueueCapacity(workbench);
        List<string> parts = [];
        for (int slotIndex = 0; slotIndex < capacity; slotIndex++)
        {
            ItemDefinition? queuedItem = GetWorkbenchQueuedItemAtSlot(workbench, slotIndex);
            int queuedQuantity = GetWorkbenchQueuedQuantityAtSlot(workbench, slotIndex);
            if (queuedItem is null || queuedQuantity <= 0)
            {
                parts.Add($"{slotIndex + 1}:EMPTY");
                continue;
            }

            parts.Add($"{slotIndex + 1}:{queuedItem.Name.ToUpperInvariant()} X{queuedQuantity}");
        }

        return parts.Count == 0 ? "QUEUE: NONE" : $"QUEUE: {string.Join(" | ", parts)}";
    }

    // Draws pc Menu Screen for the current frame using the active render context.
    private void DrawPcMenuScreen()
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        Viewport viewport = GraphicsDevice.Viewport;
        Rectangle overlay = new(0, 0, viewport.Width, viewport.Height);
        Rectangle panel = new(viewport.Width / 2 - 360, viewport.Height / 2 - 240, 720, 480);
        Rectangle listArea = new(panel.X + 36, panel.Y + 84, panel.Width - 72, panel.Height - 126);

        GetPcMenuTheme(_activePcMenuScreen, out Color overlayTint, out Color panelFill, out Color panelBorder, out Color rowFill, out Color rowBorder);

        _spriteBatch.Draw(_pixel, overlay, overlayTint);
        _spriteBatch.Draw(_pixel, panel, panelFill);
        DrawPanelBorder(panel, panelBorder);
        DrawPixelText(GetPcMenuTitle(), new Vector2(panel.X + 24, panel.Y + 18), new Color(236, 220, 196));

        if (_activePcMenuScreen == PcMenuScreen.Level)
        {
            DrawPixelText("DITTO SKILLS", new Vector2(panel.X + 24, panel.Y + 58), new Color(236, 220, 196));
            DrawPixelText("MORE SKILLS COMING", new Vector2(panel.X + 24, panel.Y + 86), new Color(210, 190, 164));

            (string Label, int Level)[] unlockedSkills =
            [
                ("CUT", GetDittoSkillLevel(SkillType.Cut)),
                ("ROCK SMASH", GetDittoSkillLevel(SkillType.RockSmash))
            ];

            const int skillSlotCount = 8;
            const int columns = 4;
            const int slotWidth = 154;
            const int slotHeight = 86;
            const int slotGap = 10;
            int startX = panel.X + 24;
            int startY = panel.Y + 116;

            for (int index = 0; index < skillSlotCount; index++)
            {
                int row = index / columns;
                int column = index % columns;
                Rectangle slotBounds = new(
                    startX + (column * (slotWidth + slotGap)),
                    startY + (row * (slotHeight + slotGap)),
                    slotWidth,
                    slotHeight);

                bool unlocked = index < unlockedSkills.Length;
                _spriteBatch.Draw(_pixel, slotBounds, unlocked ? new Color(58, 43, 33) : new Color(46, 36, 31));
                DrawPanelBorder(slotBounds, unlocked ? new Color(120, 90, 65) : new Color(88, 70, 58));

                if (unlocked)
                {
                    (string skillLabel, int skillLevel) = unlockedSkills[index];
                    DrawPixelText(skillLabel, new Vector2(slotBounds.X + 10, slotBounds.Y + 14), new Color(236, 220, 196));
                    DrawPixelText($"LV {skillLevel}", new Vector2(slotBounds.X + 10, slotBounds.Y + 45), new Color(210, 190, 164));
                }
                else
                {
                    DrawPixelText("UNLOCKED LATER", new Vector2(slotBounds.X + 10, slotBounds.Y + 27), new Color(160, 140, 122));
                }
            }

            DrawPixelText("PRESS E TO CLOSE", new Vector2(panel.X + 24, panel.Bottom - 32), new Color(210, 190, 164));
            return;
        }

        List<PcStorageEntry> storageEntries = GetPcStorageEntries();
        List<string> entries = _activePcMenuScreen == PcMenuScreen.Storage
            ? storageEntries.ConvertAll(entry => entry.PokemonName)
            : GetPcMenuEntries();
        if (entries.Count == 0)
        {
            DrawPixelText(
                _activePcMenuScreen == PcMenuScreen.Quests ? "NO QUESTS" : "NO POKEMON STORED",
                new Vector2(panel.X + 24, panel.Y + 58),
                new Color(236, 220, 196));
            DrawPixelText("PRESS E TO CLOSE", new Vector2(panel.X + 24, panel.Bottom - 32), new Color(210, 190, 164));
            return;
        }

        int rowHeight = 58;
        int visibleRows = Math.Max(1, listArea.Height / rowHeight);
        int scrollOffset = Math.Clamp(_selectedPcMenuIndex - visibleRows + 1, 0, Math.Max(0, entries.Count - visibleRows));

        for (int visibleIndex = 0; visibleIndex < visibleRows; visibleIndex++)
        {
            int entryIndex = scrollOffset + visibleIndex;
            if (entryIndex >= entries.Count)
            {
                break;
            }

            Rectangle rowBounds = new(
                listArea.X,
                listArea.Y + (visibleIndex * rowHeight),
                listArea.Width,
                rowHeight - 8);
            bool selected = entryIndex == _selectedPcMenuIndex;

            _spriteBatch.Draw(_pixel, rowBounds, selected ? new Color(38, 30, 28) : rowFill);
            DrawPanelBorder(rowBounds, selected ? Color.Gold : rowBorder);
            if (_activePcMenuScreen == PcMenuScreen.Storage)
            {
                DrawPcStorageEntryRow(storageEntries[entryIndex], rowBounds);
            }
            else
            {
                DrawPixelText(entries[entryIndex].ToUpperInvariant(), new Vector2(rowBounds.X + 14, rowBounds.Y + 16), new Color(236, 220, 196));
            }
        }

        string footerText = _activePcMenuScreen == PcMenuScreen.Storage
            ? (_isPcStorageActionMenuOpen ? "SPACE SELECT  E BACK" : "SPACE ACTIONS  E CLOSE")
            : "E CLOSE";
        DrawPixelText(footerText, new Vector2(panel.X + 24, panel.Bottom - 32), new Color(210, 190, 164));

        if (_activePcMenuScreen == PcMenuScreen.Storage)
        {
            DrawPixelText("BOX + FARM", new Vector2(panel.Right - 170, panel.Y + 18), new Color(210, 220, 240));
            if (_isPcStorageActionMenuOpen)
            {
                DrawPcStorageActionMenu(panel);
            }
        }
    }

    // Computes and returns pc Menu Theme without mutating persistent game state.
    private static void GetPcMenuTheme(
        PcMenuScreen screen,
        out Color overlayTint,
        out Color panelFill,
        out Color panelBorder,
        out Color rowFill,
        out Color rowBorder)
    {
        switch (screen)
        {
            case PcMenuScreen.Quests:
                overlayTint = new Color(30, 10, 10, 210);
                panelFill = new Color(108, 34, 34, 245);
                panelBorder = new Color(214, 124, 124);
                rowFill = new Color(136, 46, 46);
                rowBorder = new Color(182, 94, 94);
                return;
            case PcMenuScreen.Storage:
                overlayTint = new Color(8, 18, 36, 210);
                panelFill = new Color(30, 62, 132, 245);
                panelBorder = new Color(120, 168, 236);
                rowFill = new Color(42, 80, 156);
                rowBorder = new Color(94, 136, 198);
                return;
            default:
                overlayTint = new Color(22, 12, 34, 210);
                panelFill = new Color(80, 44, 116, 245);
                panelBorder = new Color(176, 130, 226);
                rowFill = new Color(100, 58, 142);
                rowBorder = new Color(148, 104, 194);
                return;
        }
    }

    // Computes and returns pc Menu Title without mutating persistent game state.
    private string GetPcMenuTitle()
    {
        return _activePcMenuScreen switch
        {
            PcMenuScreen.Quests => "QUESTS",
            PcMenuScreen.Storage => "PC STORAGE",
            PcMenuScreen.Level => "LEVEL",
            _ => "PC"
        };
    }

    // Computes and returns pc Menu Entries without mutating persistent game state.
    private List<string> GetPcMenuEntries()
    {
        if (_activePcMenuScreen == PcMenuScreen.Quests)
        {
            return _activeQuests.ConvertAll(quest => quest.Name);
        }

        if (_activePcMenuScreen == PcMenuScreen.Storage)
        {
            return [.. _storedPcPokemonNames];
        }

        return [];
    }

    // Draws one row in the PC storage list, including icon and section label for boxed vs on-farm Pokemon.
    private void DrawPcStorageEntryRow(PcStorageEntry entry, Rectangle rowBounds)
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        Rectangle iconBounds = new(rowBounds.X + 8, rowBounds.Y + 7, 36, 36);
        _spriteBatch.Draw(_pixel, iconBounds, new Color(24, 42, 78));
        DrawPanelBorder(iconBounds, new Color(120, 168, 236));
        if (TryGetPokemonIconTexture(entry.PokemonName, out Texture2D? iconTexture) && iconTexture is not null)
        {
            Rectangle inner = new(iconBounds.X + 3, iconBounds.Y + 3, iconBounds.Width - 6, iconBounds.Height - 6);
            _spriteBatch.Draw(iconTexture, inner, Color.White);
        }
        else
        {
            DrawPixelText("?", new Vector2(iconBounds.X + 13, iconBounds.Y + 12), new Color(236, 220, 196));
        }

        string sectionLabel = entry.IsStoredInPc ? "BOX" : "FARM";
        DrawPixelText(sectionLabel, new Vector2(rowBounds.Right - 72, rowBounds.Y + 8), new Color(170, 206, 255));
        DrawPixelText(entry.PokemonName.ToUpperInvariant(), new Vector2(rowBounds.X + 54, rowBounds.Y + 16), new Color(236, 220, 196));
    }

    // Draws the per-entry action menu for the highlighted PC storage Pokemon.
    private void DrawPcStorageActionMenu(Rectangle parentPanel)
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        List<string> actions = GetSelectedPcStorageEntryActions();
        if (actions.Count == 0)
        {
            return;
        }

        Rectangle menuBounds = new(parentPanel.Right - 210, parentPanel.Bottom - 170, 180, 130);
        _spriteBatch.Draw(_pixel, menuBounds, new Color(18, 36, 72, 240));
        DrawPanelBorder(menuBounds, new Color(120, 168, 236));
        DrawPixelText("ACTION", new Vector2(menuBounds.X + 12, menuBounds.Y + 10), new Color(210, 220, 240));

        for (int index = 0; index < actions.Count; index++)
        {
            Rectangle actionBounds = new(menuBounds.X + 10, menuBounds.Y + 34 + (index * 28), menuBounds.Width - 20, 22);
            bool selected = index == _selectedPcStorageActionIndex;
            _spriteBatch.Draw(_pixel, actionBounds, selected ? new Color(42, 84, 156) : new Color(24, 52, 98));
            DrawPanelBorder(actionBounds, selected ? Color.Gold : new Color(120, 168, 236));
            DrawPixelText(actions[index], new Vector2(actionBounds.X + 8, actionBounds.Y + 6), new Color(236, 220, 196));
        }
    }

    // Draws dungeon Menu Screen for the current frame using the active render context.
    private void DrawDungeonMenuScreen()
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        Viewport viewport = GraphicsDevice.Viewport;
        Rectangle overlay = new(0, 0, viewport.Width, viewport.Height);
        Rectangle panel = new(viewport.Width / 2 - 420, viewport.Height / 2 - 250, 840, 500);
        Rectangle dungeonListArea = new(panel.X + 24, panel.Y + 78, 280, panel.Height - 120);
        Rectangle roomPreviewArea = new(dungeonListArea.Right + 18, panel.Y + 78, panel.Right - dungeonListArea.Right - 42, panel.Height - 120);

        _spriteBatch.Draw(_pixel, overlay, new Color(12, 12, 14, 215));
        _spriteBatch.Draw(_pixel, panel, new Color(62, 62, 68, 245));
        DrawPanelBorder(panel, new Color(170, 170, 176));
        DrawPixelText("DUNGEON PORTAL", new Vector2(panel.X + 24, panel.Y + 18), new Color(236, 236, 240));
        DrawPixelText("SPACE ENTER DUNGEON  E CLOSE", new Vector2(panel.X + 24, panel.Y + 42), new Color(204, 204, 212));

        _spriteBatch.Draw(_pixel, dungeonListArea, new Color(48, 48, 54));
        DrawPanelBorder(dungeonListArea, new Color(126, 126, 132));
        DrawPixelText("DUNGEONS", new Vector2(dungeonListArea.X + 12, dungeonListArea.Y + 10), new Color(230, 230, 236));

        if (_availableDungeons.Count == 0)
        {
            DrawPixelText("NONE", new Vector2(dungeonListArea.X + 12, dungeonListArea.Y + 34), new Color(220, 220, 220));
        }
        else
        {
            for (int index = 0; index < _availableDungeons.Count; index++)
            {
                Rectangle row = new(dungeonListArea.X + 10, dungeonListArea.Y + 34 + (index * 26), dungeonListArea.Width - 20, 22);
                bool selected = index == _selectedDungeonIndex;
                _spriteBatch.Draw(_pixel, row, selected ? new Color(70, 70, 78) : new Color(54, 54, 60));
                DrawPanelBorder(row, selected ? Color.Gold : new Color(110, 110, 118));
                DrawPixelText(_availableDungeons[index].Name.ToUpperInvariant(), new Vector2(row.X + 8, row.Y + 5), new Color(236, 236, 240));
            }
        }

        _spriteBatch.Draw(_pixel, roomPreviewArea, new Color(48, 48, 54));
        DrawPanelBorder(roomPreviewArea, new Color(126, 126, 132));
        DrawPixelText("ROOM PREVIEW", new Vector2(roomPreviewArea.X + 12, roomPreviewArea.Y + 10), new Color(230, 230, 236));

        if (_generatedDungeonPreview is null)
        {
            DrawPixelText("ENTER TO GENERATE ROOMS", new Vector2(roomPreviewArea.X + 12, roomPreviewArea.Y + 36), new Color(220, 220, 220));
            return;
        }

        DrawPixelText(_generatedDungeonPreview.DungeonName.ToUpperInvariant(), new Vector2(roomPreviewArea.X + 12, roomPreviewArea.Y + 36), new Color(236, 236, 240));
        int maxVisibleRooms = Math.Max(1, (roomPreviewArea.Height - 70) / 20);
        for (int index = 0; index < _generatedDungeonPreview.Rooms.Count && index < maxVisibleRooms; index++)
        {
            GeneratedDungeonRoom room = _generatedDungeonPreview.Rooms[index];
            string roomText = $"{room.Index}. {room.Definition.Type.ToString().ToUpperInvariant()} - {room.Definition.Name.ToUpperInvariant()}";
            DrawPixelText(roomText, new Vector2(roomPreviewArea.X + 12, roomPreviewArea.Y + 58 + (index * 20)), new Color(220, 220, 228));
        }
    }

    // Draws panel Border for the current frame using the active render context.
    private void DrawPanelBorder(Rectangle rectangle, Color color)
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        _spriteBatch.Draw(_pixel, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, 2), color);
        _spriteBatch.Draw(_pixel, new Rectangle(rectangle.X, rectangle.Bottom - 2, rectangle.Width, 2), color);
        _spriteBatch.Draw(_pixel, new Rectangle(rectangle.X, rectangle.Y, 2, rectangle.Height), color);
        _spriteBatch.Draw(_pixel, new Rectangle(rectangle.Right - 2, rectangle.Y, 2, rectangle.Height), color);
    }

    // Draws pixel Text for the current frame using the active render context.
    private void DrawPixelText(string text, Vector2 position, Color color, int pixelSize = UiFontPixelSize, int spacing = UiFontSpacing)
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        int cursorX = (int)position.X;
        int cursorY = (int)position.Y;

        foreach (char rawCharacter in text)
        {
            char character = char.ToUpperInvariant(rawCharacter);
            if (!PixelFont.TryGetValue(character, out string[]? glyph))
            {
                glyph = PixelFont[' '];
            }

            int glyphWidth = glyph[0].Length;
            for (int row = 0; row < glyph.Length; row++)
            {
                for (int column = 0; column < glyph[row].Length; column++)
                {
                    if (glyph[row][column] != '1')
                    {
                        continue;
                    }

                    _spriteBatch.Draw(
                        _pixel,
                        new Rectangle(
                            cursorX + (column * pixelSize),
                            cursorY + (row * pixelSize),
                            pixelSize,
                            pixelSize),
                        color);
                }
            }

            cursorX += (glyphWidth * pixelSize) + spacing;
        }
    }

    // Measures pixel Text for layout decisions before rendering.
    private Point MeasurePixelText(string text, int pixelSize = UiFontPixelSize, int spacing = UiFontSpacing)
    {
        int width = 0;
        int maxHeight = 0;

        foreach (char rawCharacter in text)
        {
            char character = char.ToUpperInvariant(rawCharacter);
            if (!PixelFont.TryGetValue(character, out string[]? glyph))
            {
                glyph = PixelFont[' '];
            }

            width += (glyph[0].Length * pixelSize) + spacing;
            maxHeight = Math.Max(maxHeight, glyph.Length * pixelSize);
        }

        if (text.Length > 0)
        {
            width -= spacing;
        }

        return new Point(width, maxHeight);
    }

    // Draws player for the current frame using the active render context.
    private void DrawPlayer()
    {
        bool isWalking = _playerMovement != Vector2.Zero;
        int idleFrame = !isWalking && _playerIdleStationaryTimer >= PlayerIdleStartDelaySeconds
            ? _playerIdleAnimationFrame
            : 0;
        DrawPokemonAt(_playerPosition, PlayerPokemonName, _playerDirection, isWalking, _walkAnimationFrame, idleFrame);
    }

    // Draws pokemon At for the current frame using the active render context.
    private void DrawPokemonAt(
        Vector2 topLeftPosition,
        string pokemonName,
        Direction direction = Direction.Down,
        bool isWalking = false,
        int walkFrame = 0,
        int idleFrame = 0)
    {
        if (_spriteBatch is null)
        {
            return;
        }

        if (!TryGetPokemonSpriteData(pokemonName, out Texture2D? spriteSheet, out Dictionary<string, SpriteFrame>? frames) ||
            spriteSheet is null ||
            frames is null)
        {
            if (_pixel is not null)
            {
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle((int)topLeftPosition.X, (int)topLeftPosition.Y, PlayerSize, PlayerSize),
                    Color.CornflowerBlue);
            }

            return;
        }

        SpriteFrame? frame = GetCurrentPlayerFrame(frames, direction, isWalking, walkFrame, idleFrame);
        if (frame is null)
        {
            if (_pixel is not null)
            {
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle((int)topLeftPosition.X, (int)topLeftPosition.Y, PlayerSize, PlayerSize),
                    Color.CornflowerBlue);
            }

            return;
        }

        float scale = PlayerRenderSize / (float)PlayerSpriteCanvasSize;
        int renderX = (int)topLeftPosition.X;
        int renderY = (int)topLeftPosition.Y + PlayerSize - (int)MathF.Round(PlayerSpriteCanvasSize * scale);

        Rectangle destination = new(
            renderX + (int)MathF.Round(frame.OffsetX * scale),
            renderY + (int)MathF.Round(frame.OffsetY * scale),
            (int)MathF.Round(frame.Source.Width * scale),
            (int)MathF.Round(frame.Source.Height * scale));

        _spriteBatch.Draw(spriteSheet, destination, frame.Source, Color.White);
    }

    // Draws interaction Overlay for the current frame using the active render context.
    private void DrawInteractionOverlay()
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        DrawActiveSkillIndicator();

        if (_activeDungeonRun is not null)
        {
            GeneratedDungeonRoom? activeRoom = GetActiveDungeonRoom();
            if (activeRoom is not null)
            {
                string roomText = $"ROOM {_activeDungeonRoomIndex + 1}/{_activeDungeonRun.Rooms.Count} {activeRoom.Definition.Name.ToUpperInvariant()}";
                DrawPromptPanel(roomText, new Point(GraphicsDevice.Viewport.Width / 2, 48));
                DrawPromptPanel("PRESS E FOR NEXT ROOM", new Point(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height - 64));
            }

            if (!string.IsNullOrEmpty(_interactionMessage))
            {
                DrawPromptPanel(_interactionMessage, new Point(GraphicsDevice.Viewport.Width / 2, 96));
            }

            return;
        }

        if (_talkTargetIndex >= 0)
        {
            string pokemonName = _spawnedDittos[_talkTargetIndex].Name.ToUpperInvariant();
            DrawPromptPanel($"PRESS Q TO TALK TO {pokemonName}", new Point(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height - 104));
        }

        if (_interactTarget?.Definition == ItemCatalog.Bed)
        {
            List<string> residents = GetBedResidentPokemonNames(_interactTarget);
            if (residents.Count > 0)
            {
                string residentText = string.Join(", ", residents.Select(name => name.ToUpperInvariant()));
                DrawPromptPanel($"{residentText} LIVE HERE", new Point(GraphicsDevice.Viewport.Width / 2, 48));
            }
        }
        if (_interactTarget is not null)
        {
            string buildingName = _interactTarget.IsConstructionSite
                ? $"{_interactTarget.Definition.Name.ToUpperInvariant()} SITE"
                : _interactTarget.Definition.Name.ToUpperInvariant();
            string promptText = IsBuildingDamageSkillSelected()
                ? $"PRESS E TO DAMAGE {buildingName}"
                : $"PRESS E TO USE {buildingName}";
            DrawPromptPanel(promptText, new Point(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height - 64));
        }

        if (!string.IsNullOrEmpty(_interactionMessage))
        {
            DrawPromptPanel(_interactionMessage, new Point(GraphicsDevice.Viewport.Width / 2, 48));
        }
    }

    // Draws the selected Cut/Rock Smash indicator at the top-right HUD area.
    private void DrawActiveSkillIndicator()
    {
        if (_spriteBatch is null || _pixel is null || !IsBuildingDamageSkillSelected())
        {
            return;
        }

        Viewport viewport = GraphicsDevice.Viewport;
        Rectangle outer = new(viewport.Width - 62, 14, 48, 48);
        Rectangle inner = new(outer.X + 4, outer.Y + 4, outer.Width - 8, outer.Height - 8);
        Color iconColor = _activeDittoSkill == SkillType.RockSmash
            ? new Color(132, 94, 58)
            : new Color(74, 166, 88);

        _spriteBatch.Draw(_pixel, outer, new Color(30, 20, 14, 230));
        DrawPanelBorder(outer, new Color(236, 220, 196));
        _spriteBatch.Draw(_pixel, inner, iconColor);
        DrawPixelText(GetActiveSkillLabel().ToUpperInvariant(), new Vector2(outer.X - 8, outer.Bottom + 6), new Color(236, 220, 196));
    }

    // Computes and returns active Dungeon Room without mutating persistent game state.
    private GeneratedDungeonRoom? GetActiveDungeonRoom()
    {
        if (_activeDungeonRun is null ||
            _activeDungeonRoomIndex < 0 ||
            _activeDungeonRoomIndex >= _activeDungeonRun.Rooms.Count)
        {
            return null;
        }

        return _activeDungeonRun.Rooms[_activeDungeonRoomIndex];
    }

    // Draws talk Screen for the current frame using the active render context.
    private void DrawTalkScreen()
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        Viewport viewport = GraphicsDevice.Viewport;
        Rectangle panel = new(36, viewport.Height - 137, viewport.Width - 72, 127);
        Rectangle iconPanel = new(panel.X + 18, panel.Y + 15, 97, 97);
        Rectangle textPanel = new(iconPanel.Right + 18, panel.Y + 15, panel.Width - 97 - 265 - 72, 97);
        Rectangle optionsPanel = new(textPanel.Right + 18, panel.Y + 15, 205, 97);
        List<PokemonDialogueOption> options = _talkState.Options;
        int scrollOffset = Math.Clamp(_talkState.SelectedOptionIndex - VisibleTalkOptionCount + 1, 0, Math.Max(0, options.Count - VisibleTalkOptionCount));
        bool hideSelectableOptions = _talkExitTimer > 0f;

        _spriteBatch.Draw(_pixel, panel, new Color(44, 31, 23, 245));
        DrawPanelBorder(panel, new Color(181, 138, 95));

        _spriteBatch.Draw(_pixel, iconPanel, new Color(58, 43, 33));
        DrawPanelBorder(iconPanel, new Color(120, 90, 65));
        Rectangle portraitBounds = new(iconPanel.X + 17, iconPanel.Y + 13, 64, 64);
        if (_talkState.IconTexture is not null)
        {
            _spriteBatch.Draw(_talkState.IconTexture, portraitBounds, Color.White);
        }
        else
        {
            _spriteBatch.Draw(_circleTexture ?? _pixel, portraitBounds, new Color(178, 208, 118));
        }

        DrawPanelBorder(portraitBounds, new Color(236, 220, 196));
        DrawPixelText(_talkState.SpeakerName.ToUpperInvariant(), new Vector2(iconPanel.X + 8, iconPanel.Bottom - 19), new Color(236, 220, 196));

        _spriteBatch.Draw(_pixel, textPanel, new Color(58, 43, 33));
        DrawPanelBorder(textPanel, new Color(120, 90, 65));
        DrawPixelText(_talkState.Text, new Vector2(textPanel.X + 16, textPanel.Y + 18), new Color(236, 220, 196));

        _spriteBatch.Draw(_pixel, optionsPanel, new Color(58, 43, 33));
        DrawPanelBorder(optionsPanel, new Color(120, 90, 65));

        if (hideSelectableOptions)
        {
            return;
        }

        DrawPixelText("YOU SAY", new Vector2(optionsPanel.X + 12, optionsPanel.Y + 8), new Color(236, 220, 196));

        if (scrollOffset > 0)
        {
            DrawTriangleIndicator(new Point(optionsPanel.Right - 10, optionsPanel.Y + 13), true, new Color(236, 220, 196));
        }

        if (scrollOffset + VisibleTalkOptionCount < options.Count)
        {
            DrawTriangleIndicator(new Point(optionsPanel.Right - 10, optionsPanel.Bottom - 13), false, new Color(236, 220, 196));
        }

        for (int visibleIndex = 0; visibleIndex < VisibleTalkOptionCount; visibleIndex++)
        {
            int optionIndex = scrollOffset + visibleIndex;
            if (optionIndex >= options.Count)
            {
                break;
            }

            Rectangle optionBounds = new(optionsPanel.X + 10, optionsPanel.Y + 28 + (visibleIndex * 20), optionsPanel.Width - 30, 16);
            bool selected = optionIndex == _talkState.SelectedOptionIndex;

            _spriteBatch.Draw(_pixel, optionBounds, selected ? new Color(88, 66, 49) : new Color(58, 43, 33));
            DrawPanelBorder(optionBounds, selected ? Color.Gold : new Color(120, 90, 65));
            DrawPixelText(options[optionIndex].Label, new Vector2(optionBounds.X + 7, optionBounds.Y + 2), new Color(236, 220, 196));
        }
    }

    // Draws prompt Panel for the current frame using the active render context.
    private void DrawPromptPanel(string text, Point center)
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        Point textSize = MeasurePixelText(text, UiFontPixelSize, UiFontSpacing);
        Rectangle panel = new(
            center.X - (textSize.X / 2) - 12,
            center.Y - (textSize.Y / 2) - 8,
            textSize.X + 24,
            textSize.Y + 16);

        _spriteBatch.Draw(_pixel, panel, new Color(30, 20, 14, 220));
        DrawPanelBorder(panel, new Color(181, 138, 95));
        DrawPixelText(text, new Vector2(panel.X + 12, panel.Y + 8), new Color(236, 220, 196));
    }

    // Draws triangle Indicator for the current frame using the active render context.
    private void DrawTriangleIndicator(Point center, bool pointUp, Color color)
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        int[] rowWidths = [2, 4, 6];
        for (int row = 0; row < rowWidths.Length; row++)
        {
            int width = rowWidths[row];
            int y = pointUp ? center.Y + row : center.Y - row - 1;
            Rectangle rowRect = new(center.X - (width / 2), y, width, 1);
            _spriteBatch.Draw(_pixel, rowRect, color);
        }
    }
}
