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

        DrawProgressCircleAtBuildingCenter(building, progress);
    }

    private void DrawWorkbenchCraftingProgressCircle(PlacedItem workbench)
    {
        if (workbench.WorkbenchQueuedItem is null || workbench.WorkbenchCraftEffortRequired <= 0f)
        {
            return;
        }

        float completed = Math.Max(0f, workbench.WorkbenchCraftEffortRequired - workbench.WorkbenchCraftEffortRemaining);
        float progress = MathHelper.Clamp(completed / workbench.WorkbenchCraftEffortRequired, 0f, 1f);
        DrawProgressCircleAtBuildingCenter(workbench, progress);
    }

    private void DrawProgressCircleAtBuildingCenter(PlacedItem building, float progress)
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
                _spriteBatch.Draw(_pixel, new Rectangle(rowStartX, rowY, rowWidth, 1), new Color(91, 188, 110, 235));
            }
        }

        DrawPanelBorder(circleBounds, new Color(181, 138, 95));
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

        _spriteBatch.Draw(_pixel, overlay, new Color(20, 14, 10, 210));
        _spriteBatch.Draw(_pixel, panel, new Color(44, 31, 23, 245));
        DrawPanelBorder(panel, new Color(181, 138, 95));
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

            _spriteBatch.Draw(_pixel, rowBounds, selected ? new Color(88, 66, 49) : new Color(58, 43, 33));
            DrawPanelBorder(rowBounds, selected ? Color.Gold : new Color(120, 90, 65));

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
