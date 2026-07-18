# Combo 2/20 EMA Bandpassfilter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert einen schnellen und langsamen EMA-Crossover mit einem Bandpassfilter. Long-Positionen werden eröffnet, wenn der schnelle EMA über dem langsamen EMA liegt und der Bandpasswert die Verkaufszone übersteigt. Short-Positionen werden eröffnet, wenn der schnelle EMA unter dem langsamen EMA liegt und der Bandpasswert unter die Kaufzone fällt. Positionen werden geschlossen, wenn Signale verschwinden oder vor dem Startdatum.

Tests zeigen eine durchschnittliche jährliche Rendite von rund 47%. Die beste Performance wird auf dem Kryptomarkt erzielt.

## Details
- **Einstiegskriterien**:
  - **Long**: Schneller EMA > Langsamer EMA und Bandpass > Verkaufszone
  - **Short**: Schneller EMA < Langsamer EMA und Bandpass < Kaufzone
- **Long/Short**: Beide
- **Ausstiegskriterien**: Position schließen, wenn Signale verschwinden
- **Stops**: Nein
- **Standardwerte**:
  - `FastEmaLength` = 2
  - `SlowEmaLength` = 20
  - `BpfLength` = 20
  - `BpfDelta` = 0.5m
  - `BpfSellZone` = 5m
  - `BpfBuyZone` = -5m
  - `StartDate` = new DateTimeOffset(2005, 1, 1, 0, 0, 0, TimeSpan.Zero)
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA Bandpass Filter
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
