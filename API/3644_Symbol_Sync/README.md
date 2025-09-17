# Symbol Sync Strategy

## Overview
The **Symbol Sync Strategy** replicates the MetaTrader utility `SymbolSyncEA` inside the StockSharp environment. The strategy keeps the main strategy symbol and all registered linked strategies synchronized. Whenever the primary symbol changes, the strategy automatically propagates the new security to every linked strategy, ensuring that the entire workspace follows the same instrument without manual intervention.

## Core ideas
- Capture the initial strategy security at startup and reuse it as a fallback option.
- Keep a configurable list of linked strategies that should always mirror the main security.
- Allow symbol changes triggered either by a direct `Security` assignment or by specifying a new security identifier.
- Provide manual synchronization and reset operations to match the original Expert Advisor behaviour.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `ChartLimit` | Maximum number of linked strategies that can be synchronized. Prevents accidental mass-updates. | `10` |
| `SyncSecurityId` | Identifier of the security propagated to linked strategies. An empty value falls back to the strategy security. | `""` |

## Public methods
- `RegisterLinkedStrategy(Strategy strategy)` – adds a strategy instance to the synchronization list. Returns `true` when successfully registered.
- `UnregisterLinkedStrategy(Strategy strategy)` – removes a strategy from the list.
- `ChangeSyncSecurity(Security security)` – switches to the provided security instance and propagates it to every linked strategy.
- `ChangeSyncSecurity(string securityId)` – resolves the identifier through the current `SecurityProvider` and calls `ChangeSyncSecurity(Security)`.
- `ResetToInitialSecurity()` – restores the symbol captured at startup.
- `SyncSymbols()` – forces a manual resynchronization without changing the stored identifier.

## Usage workflow
1. Instantiate `SymbolSyncStrategy` and set the primary `Security` or assign `SyncSecurityId` before starting the strategy.
2. Call `RegisterLinkedStrategy` for each child strategy that must mirror the active symbol (for example different timeframes or dashboards).
3. Whenever the main symbol should change, call `ChangeSyncSecurity(Security)` or `ChangeSyncSecurity(string)`.
4. Optionally call `SyncSymbols()` to force propagation if an external component modified a linked strategy.

## Differences compared to the MQL version
- Works with StockSharp `Strategy` instances instead of MetaTrader chart windows.
- Uses the `SecurityProvider` abstraction to resolve identifiers.
- Adds defensive logging and a configurable limit for synchronized strategies.
- Offers explicit reset and manual synchronization methods for advanced automation scenarios.

## Notes
- The strategy does not issue market orders; it operates as an infrastructure helper.
- All code comments are kept in English to comply with project requirements.
