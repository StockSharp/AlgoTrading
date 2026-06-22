# Doppel-MA-Kreuzung-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Diese Strategie reproduziert den MetaTrader-Expertenberater "DoubleMA Crossover" im StockSharp-Framework. Die Logik überwacht einen schnellen und einen langsamen einfachen gleitenden Durchschnitt, wartet auf eine direktionale Kreuzung und erfordert dann eine Ausbruchsbestätigung, bevor der Markt betreten wird. Der Algorithmus verwaltet jeweils nur eine Position und beinhaltet optionales Trailing-Stop-Verhalten, das die drei ursprünglichen Trailing-Modi imitiert.

## Funktionsweise

1. **Signalerkennung** – Zwei einfache gleitende Durchschnitte (Standard: 2 und 5) werden auf der ausgewählten Kerzenserie berechnet. Eine bullische Kreuzung tritt auf, wenn der schnelle Durchschnitt über den langsamen kreuzt, und umgekehrt für eine bärische Kreuzung.
2. **Ausbruchsbestätigung** – Nach einer Kreuzung speichert die Strategie ein Ausbruchsniveau, das in Preisschritten (`BreakoutPips`) definiert ist. Eine Position wird nur dann geöffnet, wenn der Preis dieses Niveau auf einer nachfolgenden Kerze erreicht, was das Stop-Order-Verhalten aus der MQL-Version repliziert.
3. **Positionsverwaltung** – Es ist nur eine einzige Position erlaubt. Während ein Trade aktiv ist, überwacht die Strategie Stop-Loss, Take-Profit und den konfigurierten Trailing-Stop-Typ. Die internen Tracker emulieren die brokerseitige Ausführung, um das Verhalten in Backtests deterministisch zu halten.
4. **Sitzungsfilter** – Der Handel kann auf ein bestimmtes Zeitfenster (`StartHour`..`StopHour`) beschränkt werden. Die Strategie verwaltet offene Trades außerhalb des Fensters noch, erstellt aber keine neuen Ausbruchsniveaus, wenn der Filter den Handel blockiert.
5. **Trailing Stops** – Drei Trailing-Modi werden unterstützt: sofortiges Trailing mit der anfänglichen Stop-Distanz, Trailing nach einer benutzerdefinierten Distanz, und die Drei-Level-Logik mit Break-Even-Verschiebungen genau wie der ursprüngliche EA.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `FastMaPeriod`, `SlowMaPeriod` | Perioden des schnellen und langsamen einfachen gleitenden Durchschnitts. |
| `BreakoutPips` | Abstand in Preisschritten, der zum Signalkerzenschluss addiert wird, um den Ausbruchstrigger zu definieren. |
| `StopLossPips`, `TakeProfitPips` | Schützender Stop und optionaler Take Profit in Preisschritten. Take Profit auf null setzen, um ihn zu deaktivieren. |
| `UseTrailingStop` | Aktiviert das Trailing-Stop-Management. |
| `TrailingMode` | Trailing-Typ: Type1 verwendet die ursprüngliche Stop-Distanz, Type2 wartet auf eine benutzerdefinierte Distanz (`TrailingStopPips`), Type3 verwendet die drei MQL-Level. |
| `TrailingStopPips` | Distanz für Type2-Trailing. |
| `Level1TriggerPips`, `Level1OffsetPips` | Erster Triggerlevel und Offset für Type3-Trailing (verschiebt Stop standardmäßig auf Break-Even). |
| `Level2TriggerPips`, `Level2OffsetPips` | Zweiter Triggerlevel und Offset für Type3-Trailing. |
| `Level3TriggerPips`, `Level3OffsetPips` | Dritter Triggerlevel und Offset für Type3-Trailing (konvertiert zu einem klassischen Trailing Stop). |
| `UseTimeLimit`, `StartHour`, `StopHour` | Aktiviert den Handelssitzungsfilter und definiert den inklusiven Stundenbereich. |
| `CandleType` | Kerzenserie für Signalberechnungen. |
| `TradeVolume` | Ordervolumen in Lots. |

## Trailing Stop-Modi

- **Type1** – Verschiebt den Stop mit der ursprünglichen Stop-Loss-Distanz, sobald der Preis diesen Betrag vorgerückt ist.
- **Type2** – Wartet, bis der Preis sich um `TrailingStopPips` bewegt hat, bevor er trailt, dann sperrt den Gewinn auf dieser Distanz.
- **Type3** – Verwendet drei Level: Die ersten zwei verschieben den Stop um die definierten Offsets, und der dritte konvertiert zu einem kontinuierlichen Trailing Stop unter Verwendung des aktuellen Schlusses und `Level3OffsetPips`.

## Verwendungstipps

- `BreakoutPips` mit der Instrumenten-Tick-Größe abstimmen, um das gleiche Verhalten wie der MetaTrader-Expertenberater zu erhalten.
- Den Sitzungsfilter überprüfen, um die Handelszeiten abzustimmen; der Standard erlaubt Einstiege zwischen 11:00 und 16:00 Lokalzeit.
- Den Zeitfilter deaktivieren (`UseTimeLimit = false`) für 24/7-Instrumente.
- Beim Testen von Trailing-Typ 3 sicherstellen, dass die Offset-Werte nicht größer sind als ihre entsprechenden Triggerlevel; andernfalls kann der Stop hinter dem Einstandspreis verbleiben.
