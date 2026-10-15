# Turtle Trader SAR-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Turtle Trader SAR konvertiert das originale MQL5 Turtle-System mit einem optionalen Parabolic SAR-Trail in StockSharp C#.
Die Strategie handelt Ausbrüche aus Donchian-Kanälen, bemisst Positionen anhand von ATR-basiertem Risiko und kann Gewinntrades pyramidisieren.

## Funktionsweise

1. **Indikatorberechnung**
   - 20-Perioden-ATR für Volatilität.
   - Donchian-Kanäle für `ShortPeriod` und `ExitPeriod`.
   - Optionaler Parabolic SAR für Trailing-Stops.
2. **Positionsgrößenbestimmung**
   - Jeder Einstieg riskiert `RiskFraction` des aktuellen Eigenkapitals.
   - Die Einheitsgröße ist durch `MaxUnits` begrenzt.
3. **Einstiegskriterien**
   - Schluss über dem `ShortPeriod`-Hoch -> kaufen.
   - Schluss unter dem `ShortPeriod`-Tief -> verkaufen.
4. **Pyramidisierung**
   - Fügt jede `AddInterval`-ATR-Bewegung zugunsten eine neue Einheit hinzu bis `MaxUnits`.
5. **Ausstiegskriterien**
   - Gegenseitiger `ExitPeriod`-Ausbruch.
   - ATR-Stop mit `StopAtr` und optionalem Take Profit `TakeAtr`.
   - Wenn `UseSar` true ist, gilt auch der Parabolic SAR-Stop.

## Parameter

- `ExitPeriod` = 10
- `ShortPeriod` = 20
- `LongPeriod` = 55
- `RiskFraction` = 0.01
- `MaxUnits` = 4
- `AddInterval` = 1
- `StopAtr` = 1
- `TakeAtr` = 1
- `UseSar` = false
- `SarStep` = 0.02
- `SarMax` = 0.2
- `CandleType` = 1 day

## Tags

- **Kategorie**: Trendfolge
- **Richtung**: Beide
- **Indikatoren**: ATR, Highest, Lowest, Parabolic SAR
- **Stops**: ATR / SAR
- **Komplexität**: Mittel
- **Zeitrahmen**: Täglich
- **Saisonalität**: Nein
- **Neuronale Netze**: Nein
- **Divergenz**: Nein
- **Risikolevel**: Mittel
