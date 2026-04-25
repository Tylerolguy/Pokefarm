using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static Pokefarm.Game.BuildingWorkerHelpers;
using static Pokefarm.Game.WorkbenchCraftingHelpers;

namespace Pokefarm.Game;

public sealed partial class FarmGame
{
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

    private void DrawPlacedItems()
    {
        if (_spriteBatch is null || _pixel is null || _circleTexture is null)
        {
            return;
        }

        foreach (PlacedItem item in _placedItems)
        {
            Texture2D texture = (item.Definition.IsBuildingLike || item.Definition.Kind == ItemKind.Snack) && item.Definition.HasCollision
                ? _pixel
                : _circleTexture;
            _spriteBatch.Draw(texture, item.Bounds, item.Definition.Tint);
            DrawPanelBorder(item.Bounds, new Color(40, 28, 20));

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

            if ((item.Definition.IsResourceProduction || item.Definition == ItemCatalog.WorkBench) &&
                GetWorkerPokemonNames(item).Count > 0)
            {
                DrawBuildingWorkerIcons(item);
            }
        }
    }

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

    private void DrawWorkbenchCraftingProgressCircle(PlacedItem workbench)
    {
        if (workbench.WorkbenchQueuedItem is null || workbench.WorkbenchCraftEffortRequired <= 0f)
        {
            return;
        }

        float completed = Math.Max(0f, workbench.WorkbenchCraftEffortRequired - workbench.WorkbenchCraftEffortRemaining);
        float progress = MathHelper.Clamp(completed / workbench.WorkbenchCraftEffortRequired, 0f, 1f);
        DrawProgressCircleAtBuildingCenter(workbench, progress, new Color(91, 188, 110, 235));
    }

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

    private void DrawBuildingWorkerIcons(PlacedItem building)
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        List<string> workerNames = GetWorkerPokemonNames(building);
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
    }

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
            DrawStatusMarker(pokemon);
        }
    }

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

        for (int slotIndex = 0; slotIndex < InventoryColumns * InventoryRows; slotIndex++)
        {
            int column = slotIndex % InventoryColumns;
            int row = slotIndex / InventoryColumns;
            Rectangle slot = new(
                panel.X + 84 + (column * 140),
                panel.Y + 74 + (row * 140),
                96,
                96);
            bool hasItem = slotIndex < _inventoryItems.Count;
            bool selected = slotIndex == _selectedInventoryIndex;

            _spriteBatch.Draw(_pixel, slot, hasItem ? new Color(88, 66, 49) : new Color(58, 43, 33));
            DrawPanelBorder(slot, selected ? Color.Gold : new Color(120, 90, 65));

            if (hasItem)
            {
                InventoryEntry entry = _inventoryItems[slotIndex];
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
    }

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
            string queuedText = workbench.WorkbenchQueuedItem is null
                ? "QUEUED: NONE"
                : $"QUEUED: {workbench.WorkbenchQueuedItem.Name.ToUpperInvariant()}";
            DrawPixelText(queuedText, new Vector2(panel.X + 24, panel.Y + 44), new Color(210, 190, 164));

            ItemDefinition? completedItem = IsWorkbenchItemReady(workbench) ? workbench.WorkbenchQueuedItem : null;
            string completedText = completedItem is not null
                ? $"COMPLETED: {completedItem.Name.ToUpperInvariant()}"
                : "COMPLETED: NONE";
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
            DrawPixelText("LEVEL 1", new Vector2(panel.X + 24, panel.Y + 58), new Color(236, 220, 196));
            List<SpawnedPokemon> claimedPokemon = _spawnedDittos
                .Where(pokemon => pokemon.IsClaimed)
                .ToList();

            if (claimedPokemon.Count == 0)
            {
                DrawPixelText("NO CLAIMED POKEMON", new Vector2(panel.X + 24, panel.Y + 86), new Color(210, 190, 164));
            }
            else
            {
                DrawPixelText("CLAIMED TEAM", new Vector2(panel.X + 24, panel.Y + 86), new Color(210, 190, 164));
                const int iconSize = 44;
                const int iconPadding = 10;
                int iconsPerRow = Math.Max(1, (panel.Width - 48) / (iconSize + iconPadding));
                for (int index = 0; index < claimedPokemon.Count; index++)
                {
                    int row = index / iconsPerRow;
                    int column = index % iconsPerRow;
                    int iconX = panel.X + 24 + (column * (iconSize + iconPadding));
                    int iconY = panel.Y + 108 + (row * (iconSize + iconPadding));
                    Rectangle iconBounds = new(iconX, iconY, iconSize, iconSize);
                    _spriteBatch.Draw(_pixel, iconBounds, new Color(58, 43, 33));
                    DrawPanelBorder(iconBounds, new Color(120, 90, 65));

                    SpawnedPokemon pokemon = claimedPokemon[index];
                    if (TryGetPokemonIconTexture(pokemon.Name, out Texture2D? iconTexture) && iconTexture is not null)
                    {
                        Rectangle innerBounds = new(iconBounds.X + 4, iconBounds.Y + 4, iconBounds.Width - 8, iconBounds.Height - 8);
                        _spriteBatch.Draw(iconTexture, innerBounds, Color.White);
                    }
                    else
                    {
                        DrawPixelText("?", new Vector2(iconBounds.X + 16, iconBounds.Y + 13), new Color(236, 220, 196));
                    }
                }
            }

            DrawPixelText("PRESS E TO CLOSE", new Vector2(panel.X + 24, panel.Bottom - 32), new Color(210, 190, 164));
            return;
        }

        List<string> entries = GetPcMenuEntries();
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
            DrawPixelText(entries[entryIndex].ToUpperInvariant(), new Vector2(rowBounds.X + 14, rowBounds.Y + 16), new Color(236, 220, 196));
        }

        string footerText = _activePcMenuScreen == PcMenuScreen.Storage
            ? "SPACE RELEASE  E CLOSE"
            : "E CLOSE";
        DrawPixelText(footerText, new Vector2(panel.X + 24, panel.Bottom - 32), new Color(210, 190, 164));
    }

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

    private void DrawPlayer()
    {
        bool isWalking = _playerMovement != Vector2.Zero;
        int idleFrame = !isWalking && _playerIdleStationaryTimer >= PlayerIdleStartDelaySeconds
            ? _playerIdleAnimationFrame
            : 0;
        DrawPokemonAt(_playerPosition, PlayerPokemonName, _playerDirection, isWalking, _walkAnimationFrame, idleFrame);
    }

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

    private void DrawInteractionOverlay()
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

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

        if (_interactTarget?.Definition == ItemCatalog.Bed && !string.IsNullOrEmpty(_interactTarget.ResidentPokemonName))
        {
            DrawPromptPanel($"{_interactTarget.ResidentPokemonName!.ToUpperInvariant()} LIVES HERE", new Point(GraphicsDevice.Viewport.Width / 2, 48));
        }
        if (_interactTarget is not null)
        {
            string buildingName = _interactTarget.Definition.Name.ToUpperInvariant();
            DrawPromptPanel($"PRESS E TO USE {buildingName}", new Point(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height - 64));
        }

        if (!string.IsNullOrEmpty(_interactionMessage))
        {
            DrawPromptPanel(_interactionMessage, new Point(GraphicsDevice.Viewport.Width / 2, 48));
        }
    }

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
