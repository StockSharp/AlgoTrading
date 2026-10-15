# RSI Long-Position DAX 2 Stunden Dow Jones 1 Stunde
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

RSI Long Position kauft, wenn der RSI ein Überverkauft-Niveau nach oben kreuzt, und schließt, wenn der RSI ein Take-Profit-Niveau überschreitet oder unter ein Stop-Niveau fällt.

## Details

- **Einstiegskriterien**: RSI kreuzt `Oversold` nach oben
- **Long/Short**: Long
- **Ausstiegskriterien**: RSI größer als `TakeProfit` oder RSI kreuzt `StopLoss` nach unten
- **Stops**: Nein
- **Standardwerte**:
  - `RsiLength` = 14
  - `Oversold` = 35
  - `TakeProfit` = 55
  - `StopLoss` = 30
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Long
  - Indikatoren: RSI
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
