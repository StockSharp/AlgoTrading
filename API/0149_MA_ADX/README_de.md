# Ma Adx Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie basierend auf MA- und ADX-Indikatoren. Einstieg in die Position, wenn der Preis den MA bei starkem Trend kreuzt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 184%. Sie funktioniert am besten im Kryptomarkt.

Der gleitende Durchschnitt gibt den Trend vor, und ADX prüft, ob er stark genug zum Handeln ist. Einstiege folgen den Preiskreuzungen des MA, wenn ADX einen Schwellenwert überschreitet.

Dieser klassische Trendansatz spricht systematische Trader an. Verluste werden mit einem ATR-basierten Stop gesteuert.

## Details

- **Einstiegskriterien**:
  - Long: `Close > MA && ADX > 25`
  - Short: `Close < MA && ADX > 25`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Umgekehrter MA-Kreuz oder Stop
- **Stops**: `StopLossPercent` Prozent mit Take-Profit `TakeProfitAtrMultiplier` ATR
- **Standardwerte**:
  - `MaPeriod` = 20
  - `AdxPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `StopLossPercent` = 2m
  - `TakeProfitAtrMultiplier` = 2m
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Moving Average, ADX
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

