# Frank Ud Hedging-Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Frank Ud Hedging-Grid-Strategie** ist eine direkte Portierung des MetaTrader-Expertenberaters "Frank Ud" in die High-Level-API von StockSharp. Der Bot hält gleichzeitig Long- und Short-Körbe auf demselben Instrument und führt martingale-artiges Averaging durch, wenn der Preis gegen den aktiven Korb driftet. Die gesamte Signalverarbeitung erfolgt auf Basis von Bestes-Bid/Ask-(Level-1-)Updates, was die Strategie für die Ausführung mit niedriger Latenz oder Tick-by-Tick-Backtesting geeignet macht.

## Handelslogik
1. **Initiales Hedging** – wenn keine Positionen offen sind, eröffnet die Strategie sofort eine Kauf- und eine Verkaufsmarktorder mit demselben Volumen. Jede Order erhält einen in Pips ausgedrückten Stop-Loss und Take-Profit.
2. **Stop/Take-Verwaltung** – solange beide Körbe existieren, werden ihre Schutzlevels respektiert. Wenn der Preis ein Schutzlevel erreicht, wird der entsprechende Korb geschlossen.
3. **Einseitige Verwaltung** – wenn nur noch Kauf- oder nur noch Verkaufspositionen verbleiben:
   - Berechnet den volumengewichteten Durchschnittseinstiegspreis des aktiven Korbs.
   - Setzt den gemeinsamen Take-Profit auf den Durchschnittspreis ± konfigurierten Abstand.
   - Entfernt den Stop-Loss (der ursprüngliche EA stützt sich ab diesem Punkt ausschließlich auf den Take-Profit).
4. **Martingale-Schritt** – wenn der Preis um mehr als den konfigurierten Schritt gegen den aktiven Korb läuft, verdoppelt die Strategie den Multiplikator und eröffnet eine neue Marktorder. Die Hilfsmethode `AdjustVolume` hält jede Order am Volumenschritt, Minimum und Maximum des Instruments ausgerichtet.
5. **Zyklusreset** – sobald alle Körbe geschlossen sind, wird der Multiplikator auf 1 zurückgesetzt und ein neuer Hedging-Zyklus beginnt.

## Parameter
- `TakeProfitPips` – Abstand zwischen dem Korbdurchschnittspreis und dem kollektiven Take-Profit-Ziel (Standard: 12 Pips).
- `StopLossPips` – Schutzstop-Abstand, der nur für die allerersten Hedge-Orders verwendet wird (Standard: 12 Pips).
- `StepPips` – adverse Bewegung, die erforderlich ist, bevor die nächste Martingale-Order hinzugefügt wird (Standard: 16 Pips).
- `AutoLot` – wenn `true`, verwendet die Strategie `LotSize`; andernfalls handelt sie mit dem Mindestvolumen des Instruments.
- `LotSize` – benutzerdefinierte Basis-Lot-Größe, die zusammen mit dem Martingale-Multiplikator verwendet wird, wenn `AutoLot` aktiviert ist.

## Implementierungshinweise
- Die Konvertierung verwendet die High-Level-`Strategy`-API: Level-1-Abonnements treiben die Logik, und die Order-Platzierung basiert auf `BuyMarket`/`SellMarket`-Hilfsmethoden.
- Das Positions-Tracking ist intern: die Strategie speichert den Einstiegspreis und das Volumen jeder Korb-Order, um die originalen MetaTrader-Averaging-Regeln reproduzieren zu können.
- Der Multiplikator (`_multiplier`) spiegelt die Variable `Coefficient` des EA wider und verdoppelt sich nach jeder zusätzlichen Order. Sobald alle Trades geschlossen sind, wird der Multiplikator auf `1` zurückgesetzt.
- `AdjustVolume` emuliert die MQL5-Funktion `LotCheck`, indem angeforderte Volumes auf den erlaubten Handelsschritt und die Kontraktlimits begrenzt werden.
- Die Strategie erwartet ein Hedging-fähiges Konto, da sie genau wie der Quell-EA gleichzeitig Long- und Short-Körbe hält.

## Dateien
- `CS/FrankUdStrategy.cs` – Haupt-Strategieimplementierung mit englischen Inline-Kommentaren, die jeden Block erklären.
- `README.md` – dieses Dokument.
- `README_ru.md` – russische Übersetzung.
- `README_zh.md` – vereinfachte chinesische Übersetzung.
