# OSF-Gegentrendstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie reproduziert den Open-Source-Forex-Gegentrend-Experten „Overbought/Oversold“.
It approximates the original oscillator by averaging several RSI readings and interprets
der Abstand vom Gleichgewichtsniveau (50) als Richtungs- und Positionsgrößensignal.
Trades are executed on finished candles and closed by a fixed take-profit measured in
Instrumentenpunkte.

## Handelsregeln

- **Data**: Finished candles of the configured `CandleType`.
- **Indicator**: RSI with period defined by `RsiPeriod`. Der ursprüngliche MQL-Experte hatte einen Durchschnitt von fünf
identical RSI values, therefore a single RSI is sufficient here.
- **Signallogik**:
  - When RSI > 50, the market is considered overbought and a short position is opened.
  - When RSI < 50, the market is considered oversold and a long position is opened.
  - Der absolute Abstand |RSI − 50| bestimmt das gehandelte Volumen bis `VolumePerPoint`.
- **Cooldown**: After each trade the strategy waits for `CooldownBars` finished candles before
Bewertung eines neuen Eintrags. This mimics the bar smoothing behaviour from the source code.
- **Exits**: Each entry places a manual take-profit at `TakeProfitPoints` * `PriceStep` away from
der Füllpreis. Es wird kein Stop-Loss verwendet, genau wie beim ursprünglichen Experten.
- **Umkehrungen**: Durch die Eröffnung eines Handels in die entgegengesetzte Richtung wird zunächst jede bestehende Position geschlossen
durch Anpassung des Marktauftragsvolumens.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `RsiPeriod` | RSI length used to approximate the OSF oscillator (default 14). |
| `VolumePerPoint` | Volume traded for each RSI point away from the 50 level (default 0.01). |
| `TakeProfitPoints` | Abstand zum Take-Profit-Ziel, ausgedrückt in Instrumentenpunkten (Standard 150). |
| `CooldownBars` | Number of finished candles to skip after each trade (default 5). |
| `CandleType` | Kerzentyp für Indikatorberechnungen (Standardzeitrahmen 1 Minute). |

## Notizen

- Die Strategie geht davon aus, dass `PriceStep` für das ausgewählte Instrument definiert ist; ansonsten eine Einheit
Schritt 1 wird zur Berechnung des Take-Profit-Niveaus verwendet.
- Da der ursprüngliche Experte keinen schützenden Stop-Loss hatte, sollte ein Risikomanagement hinzugefügt werden
manuell bei der Live-Einführung der Strategie.
