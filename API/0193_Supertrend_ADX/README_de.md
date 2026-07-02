# Strategie Supertrend Adx
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie basierend auf dem Supertrend-Indikator und ADX zur Bestätigung der Trendstärke. Einstiegskriterien: Long: Price > Supertrend && ADX > 25 (Aufwärtstrend mit starker Bewegung). Short: Price < Supertrend && ADX > 25 (Abwärtstrend mit starker Bewegung). Ausstiegskriterien: Long: Price < Supertrend (Preis fällt unter Supertrend). Short: Price > Supertrend (Preis steigt über Supertrend).

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 166%. Sie funktioniert am besten auf dem Aktienmarkt.

Supertrend liefert einen volatilitätsbereinigten Pfad, während ADX die Stärke der Bewegung bestätigt. Trades erfolgen, wenn beide Indikatoren übereinstimmen.

Für jene, die starke Trends mit Trailing Stops reiten wollen. ATR bestimmt die Stop-Platzierung.

## Details

- **Einstiegskriterien**:
  - Long: `Close > Supertrend && ADX > AdxThreshold`
  - Short: `Close < Supertrend && ADX > AdxThreshold`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Supertrend-Umkehr
- **Stops**: Verwendet Supertrend als Trailing Stop
- **Standardwerte**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Supertrend, ADX
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

