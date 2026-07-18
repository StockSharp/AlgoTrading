# Strategie Parabolic Sar Rsi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie, die den Parabolic SAR zur Bestimmung der Trendrichtung und den RSI zur Einstiegsbestätigung mit überkauften/überverkauften Bedingungen kombiniert.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 166%. Sie funktioniert am besten im Aktienmarkt.

Hier skizziert der Parabolic SAR den vorherrschenden Trend und der RSI misst die Erschöpfung. Trades werden eröffnet, sobald beide Indikatoren dieselbe Richtung signalisieren.

Die Kombination ist attraktiv für diejenigen, die Trailing Stops mögen, da SAR auch einen dynamischen Ausstieg bietet. Die Stop-Platzierung folgt der SAR-Kurve.

## Details

- **Einstiegskriterien**:
  - Long: `Close > SAR && RSI < RsiOversold`
  - Short: `Close < SAR && RSI > RsiOverbought`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Long: `Close < SAR`
  - Short: `Close > SAR`
- **Stops**: Verwendet Parabolic SAR als Trailing Stop
- **Standardwerte**:
  - `SarAf` = 0.02m
  - `SarMaxAf` = 0.2m
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Parabolic SAR, Parabolic SAR, RSI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

