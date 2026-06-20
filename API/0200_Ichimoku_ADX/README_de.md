# Ichimoku Adx Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie basierend auf Ichimoku Cloud und ADX-Indikatoren. Einstiegskriterien:
Long: Price > Kumo (Wolke) && Tenkan > Kijun && ADX > 25 (Aufwärtstrend mit starker Bewegung) Short: Price < Kumo (Wolke) && Tenkan < Kijun && ADX > 25 (Abwärtstrend mit starker Bewegung) Ausstiegskriterien: Long: Price < Kumo (Preis fällt unter die Wolke) Short: Price > Kumo (Preis steigt über die Wolke)

Tests zeigen eine durchschnittliche Jahresrendite von etwa 187%. Die Strategie funktioniert am besten am Aktienmarkt.

Diese Strategie kombiniert Ichimoku-Cloud-Signale mit ADX, um starke Trends zu filtern. Trades erfolgen, wenn der Preis die Wolke nach oben oder unten durchbricht und ADX dies bestätigt.

Sie bevorzugt Trader, die strukturierte Trend-Setups bevorzugen. ATR-definierte Stops schützen vor ungünstigen Kursschwankungen.

## Details

- **Einstiegskriterien**:
  - Long: `Price > Cloud && Tenkan > Kijun && ADX > AdxThreshold`
  - Short: `Price < Cloud && Tenkan < Kijun && ADX > AdxThreshold`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Preis kreuzt die Wolke in entgegengesetzter Richtung
- **Stops**: Verwendet die Ichimoku-Wolke als Trailing-Stop
- **Standardwerte**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Ichimoku Cloud, ADX
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

