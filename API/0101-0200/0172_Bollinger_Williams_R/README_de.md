# Strategie Bollinger Williams R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf Bollinger Bands und Williams %R-Indikatoren. Geht Long, wenn der Preis am unteren Band liegt und Williams %R überverkauft ist (< -80). Geht Short, wenn der Preis am oberen Band liegt und Williams %R überkauft ist (> -20).

Tests zeigen eine durchschnittliche Jahresrendite von etwa 103%. Die Strategie funktioniert am besten auf dem Aktienmarkt.

Bollinger Bands zeigen Volatilitätsausbrüche auf, und Williams %R stellt sicher, dass das Momentum extrem ist. Positionen öffnen sich, wenn der Preis außerhalb eines Bandes mit einer passenden Williams %R-Lesung schließt.

Am besten für Volatilitätsexpansions-Trader. ATR-Stops handhaben ungünstige Wendungen.

## Details

- **Einstiegskriterien**:
  - Long: `Close < LowerBand && WilliamsR < -80`
  - Short: `Close > UpperBand && WilliamsR > -20`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Preis kehrt zum mittleren Band zurück
- **Stops**: ATR-basiert mit `AtrMultiplier`
- **Standardwerte**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `WilliamsRPeriod` = 14
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, Williams %R, R
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

