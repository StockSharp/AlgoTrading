# ColorXADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf der Kreuzung der +DI- und -DI-Linien, bestätigt durch die ADX-Stärke.

Das System überwacht die Directional-Movement-Indikatoren. Wenn +DI über -DI kreuzt und der Average Directional Index einen festgelegten Schwellenwert überschreitet, wird eine Long-Position eröffnet und jede bestehende Short-Position geschlossen. Umgekehrt öffnet ein bärisches Kreuz (-DI über +DI) mit starkem ADX eine Short-Position und schließt Longs. Stop-Loss- und Take-Profit-Levels werden zur Risikosteuerung angewendet.

## Details

- **Einstiegskriterien**: +DI/-DI-Kreuz mit ADX über dem Schwellenwert.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Stop-Levels.
- **Stops**: Ja, fester Stop-Loss und Take-Profit.
- **Standardwerte**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 30m
  - `StopLoss` = 1000
  - `TakeProfit` = 2000
  - `CandleType` = TimeSpan.FromHours(4)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: ADX, DMI
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Swing (4h)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
