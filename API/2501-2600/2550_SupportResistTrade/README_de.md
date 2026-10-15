# SupportResistTrade-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Aus MetaTrader portierte Ausbruchsstrategie, die einen langfristigen EMA-Trendfilter mit dynamischen Unterstützungs- und Widerstandsniveaus kombiniert. Sie beobachtet den jüngsten Schwingungsbereich, wartet darauf, dass der Preis das vorherige Hoch oder Tief in Trendrichtung bricht, und verwaltet Positionen mit gestaffelten Pip-basierten Trailing-Stops.

## Details

- **Einstiegskriterien**: Preis schließt über dem vorherigen `Lookback`-Perioden-Hoch (Long) oder -Tief (Short), und die Bar öffnet über/unter der EMA `MaPeriod`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Trailing-Stop wird ausgelöst oder eine profitable Position kreuzt zurück durch das aktualisierte Unterstützungs-/Widerstandsband
- **Stops**: Anfangsstopp am gegenüberliegenden Band, Trail nach +20/+40/+60 Pip-Bewegungen (sichert jeweils 10/20/30 Pips)
- **Standardwerte**:
  - `Lookback` = 55
  - `MaPeriod` = 500
  - `CandleType` = 1 Minute
  - `OrderVolume` = 0.1
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: EMA, Highest, Lowest
  - Stops: Trailing
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
