# Strategie Auto Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Fügt bestehenden Positionen automatisch Stop-Loss- und Take-Profit-Aufträge hinzu und zieht den Stop nach, während sich der Preis in die günstige Richtung bewegt.

## Details
- **Einstiegskriterien**: Keine, die Strategie eröffnet keine Trades.
- **Long/Short**: Arbeitet sowohl mit bereits offenen Long- als auch Short-Positionen.
- **Ausstiegskriterien**: Stop-Loss- und Take-Profit-Aufträge. Der Trailing Stop wird aktualisiert, nachdem sich der Preis um die Hälfte der Trailing-Distanz bewegt hat.
- **Stops**: Initiale Stop-Loss- und Take-Profit-Aufträge werden gesetzt, sobald die Position erscheint; der Stop-Loss folgt durch `TrailingStopStep`.
- **Standardwerte**: TrailingStop 6, TrailingStopStep 1, TakeProfit 35, StopLoss 114.
- **Filter**: Optionale Deaktivierung von Trailing Stop, automatischem Take-Profit oder automatischem Stop-Loss über Parameter.

## Parameter
- `FridayTrade` - Trailing an Freitagen erlauben.
- `UseTrailingStop` - Trailing-Stop-Logik aktivieren.
- `AutoTrailingStop` - Standard-Trailing-Distanz von 6 verwenden, wenn wahr.
- `TrailingStop` - Trailing-Distanz in Preiseinheiten, wenn AutoTrailingStop falsch ist.
- `TrailingStopStep` - minimale Preisbewegung, bevor der Trailing Stop verschoben wird.
- `AutomaticTakeProfit` - automatisch einen Take-Profit-Auftrag platzieren.
- `TakeProfit` - Take-Profit-Distanz.
- `AutomaticStopLoss` - automatisch einen Stop-Loss-Auftrag platzieren.
- `StopLoss` - Stop-Loss-Distanz.
- `CandleType` - Kerzentyp für Preisaktualisierungen (Standard 1 Minute).
