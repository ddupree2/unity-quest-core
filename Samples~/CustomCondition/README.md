# Custom Condition Sample

Shows how to create a custom condition type for your game's specific needs.

## What It Shows

- Creating a `ConditionAsset` (ScriptableObject for designer setup)
- Implementing `IConditionInstance` (runtime condition logic)
- Event-driven condition evaluation
- Progress tracking with `IProgressReportingCondition`

## Structure

1. **EnemyKilledCondition** - Asset class with designer-facing properties
2. **EnemyKilledConditionInstance** - Runtime logic built on `EventDrivenConditionBase`
3. **EnemyKilledEvent** / **EnemyKilledScriptableEvent** - Event payload and the event asset that carries it

## Usage

1. Create an event asset: Right-click → Create → Quest Samples → Events → Enemy Killed Event
2. Create the condition: Right-click → Create → Quest Samples → Conditions → Enemy Killed, assign the event asset, and set enemy type and required kills
3. Use in quest objectives like any built-in condition
4. Raise the same event asset from your game: `enemyKilledEvent.Raise(new EnemyKilledEvent("Goblin"))`

**~70 lines** for a complete custom condition with event handling.
