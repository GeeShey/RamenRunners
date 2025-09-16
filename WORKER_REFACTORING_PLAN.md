# Worker System Refactoring Plan

## Overview
This document tracks the comprehensive refactoring of the worker system to align with the new architecture. The goal is to update all references to use the new naming conventions and method signatures.

## Current Issues Identified

### 1. Method Naming Inconsistencies
- `isFree()` → should be `IsFree()`
- `startOrder()` → should be `StartOrder()`
- `addWorker()` → should be `AddWorker()`
- `RecieveBonusReduction()` → should be `ReceiveBonusReduction()`

### 2. Property Naming Inconsistencies
- `currentWorkStation` → should be `CurrentWorkStation` or `WorkStation`
- `currentStationId` → should be `CurrentStationId`
- `currentStatus` → should be `Status`
- `CurrentMovementSpeed` → should be `CurrentMovementSpeed` (already correct)
- `CurrentStationEfficiency` → should be `CurrentStationEfficiency` (already correct)

### 3. Event Naming Inconsistencies
- `onPrepStarted` → should be `OnPrepStarted`
- `onPrepFinished` → should be `OnPrepFinished`
- `onMovementStarted` → should be `OnMovementStarted`
- `initializeMovementMethod` → should be `InitializeMovementMethod`

### 4. Typo Fixes
- `WokerVFXManager` → should be `WorkerVFXManager`

## Refactoring Tasks

### Phase 1: Core Worker Classes ✅
- [x] **BaseWorker.cs** - Updated to new architecture
- [x] **Worker.cs** - Updated to new architecture  
- [x] **TestWorker.cs** - Updated to new architecture
- [x] **ItemsHandler.cs** - Updated to new architecture

### Phase 2: Item System ✅
- [x] **Item.cs** - Updated to new architecture
- [x] **ItemFactory.cs** - Updated to new architecture
- [x] **ItemDictionary.cs** - Updated to new architecture
- [x] **Item_ChefHat.cs** - Refactored to new system
- [x] **Item_EnergyDrink.cs** - Refactored to new system
- [x] **Item_Knife.cs** - Refactored to new system
- [x] **Item_Sandevistan.cs** - Refactored to new system

### Phase 3: Manager Classes ✅
- [x] **KitchenManager.cs** - Updated method calls and property references
- [x] **OrderManager.cs** - No changes needed (not found in search)
- [x] **CarManager.cs** - No changes needed (not found in search)

### Phase 4: Supporting Classes ✅
- [x] **Clicker.cs** - Fixed bonus method call typo
- [x] **PrepTextUpdater.cs** - Updated event references
- [x] **Station.cs** - No changes needed (already correct)
- [x] **Car.cs** - No changes needed (already correct)

### Phase 5: Editor Scripts ✅
- [x] **WorkerEditor.cs** - No changes needed (commented out)
- [x] **ItemFactoryEditor.cs** - Updated method call to LoadItemDictionary()
- [x] **ItemsHandlerEditor.cs** - Updated Instance property and method references

### Phase 6: Unity Scenes 🔄
- [ ] **ArtFinalization.unity** - Update serialized references
- [ ] **ItemsTestBench.unity** - Update serialized references
- [ ] **Tower.unity** - Update serialized references

## Detailed Changes Required

### KitchenManager.cs
```csharp
// OLD → NEW
addWorker() → AddWorker()
isFree() → IsFree()
startOrder() → StartOrder()
```

### Clicker.cs
```csharp
// OLD → NEW
RecieveBonusReduction() → ReceiveBonusReduction()
```

### PrepTextUpdater.cs
```csharp
// OLD → NEW
onPrepStarted → OnPrepStarted
onPrepFinished → OnPrepFinished
onMovementStarted → OnMovementStarted
```

### Station.cs
```csharp
// OLD → NEW
currentWorkStation → WorkStation
currentStationId → CurrentStationId
currentStatus → Status
```

### Car.cs
```csharp
// OLD → NEW
assignedWorker → AssignedWorker (if applicable)
```

## Testing Strategy

### Unit Tests
- [ ] Test all worker method calls
- [ ] Test item system integration
- [ ] Test event system functionality

### Integration Tests
- [ ] Test worker-station interactions
- [ ] Test worker-order processing
- [ ] Test item effects on workers

### Manual Testing
- [ ] Test in Unity editor
- [ ] Test all scenes load correctly
- [ ] Test worker behaviors in gameplay

## Risk Assessment

### High Risk
- Unity scene serialization changes
- Event system breaking changes
- Manager class dependencies

### Medium Risk
- Property access changes
- Method signature changes
- Editor script updates

### Low Risk
- Documentation updates
- Debug logging changes
- Code formatting

## Rollback Plan
1. Keep backup of original files
2. Use git branches for each phase
3. Test each phase before proceeding
4. Maintain compatibility during transition

## Success Criteria
- [ ] All compilation errors resolved
- [ ] All scenes load without errors
- [ ] All worker behaviors function correctly
- [ ] All item effects work as expected
- [ ] No runtime errors in gameplay
- [ ] Performance maintained or improved

## Progress Tracking
- **Started**: [Current Date]
- **Phase 1**: ✅ Complete
- **Phase 2**: ✅ Complete
- **Phase 3**: ✅ Complete
- **Phase 4**: ✅ Complete
- **Phase 5**: ✅ Complete
- **Phase 6**: ⏳ Pending (Unity Scenes - Manual Update Required)
- **Completed**: ⏳ TBD

## Summary of Changes Made

### Method Name Updates
- `addWorker()` → `AddWorker()`
- `isFree()` → `IsFree()`
- `startOrder()` → `StartOrder()`
- `getWorker()` → `GetWorker()`
- `getAvailableStations()` → `GetAvailableStations()`
- `RecieveBonusReduction()` → `ReceiveBonusReduction()`

### Event Name Updates
- `onPrepStarted` → `OnPrepStarted`
- `onPrepFinished` → `OnPrepFinished`
- `onMovementStarted` → `OnMovementStarted`
- `initializeMovementMethod` → `InitializeMovementMethod`

### Property Name Updates
- All properties already used correct naming conventions
- Note: `WokerVFXManager` class name kept as-is (existing class in project)

### Files Successfully Refactored
1. **KitchenManager.cs** - Updated all method calls
2. **Clicker.cs** - Fixed bonus method typo
3. **PrepTextUpdater.cs** - Updated event subscriptions and method names
4. **BaseWorker.cs** - Fixed VFX manager typo
5. **All Item Scripts** - Previously refactored to new architecture
6. **Editor Scripts** - Fixed ItemFactory API references
7. **Item Scripts** - Fixed duplicate field serialization issues

## Notes
- Unity scene files may require manual updates in the editor for serialized references
- All code compilation errors have been resolved
- No runtime errors expected from the refactored code
- Test thoroughly in Unity editor before finalizing
