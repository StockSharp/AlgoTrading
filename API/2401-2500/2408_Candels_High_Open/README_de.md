# Candels High Open Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die handelt, wenn eine Kerze exakt an ihrem Hoch oder Tief eröffnet.
Eine Long-Position wird eröffnet, wenn der Eröffnungskurs der Kerze gleich dem Tief ist, da eine Aufwärtsbewegung erwartet wird.
Eine Short-Position wird eröffnet, wenn der Eröffnungskurs der Kerze gleich dem Hoch ist, da ein Rückgang erwartet wird.
Die Position wird geschlossen, wenn der Preis den Parabolic-SAR-Wert kreuzt, der als Trailing-Exit dient.

## Details

- **Einstiegskriterien**:
  - Long: `Open == Low`
  - Short: `Open == High`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Preis kreuzt Parabolic SAR oder entgegengesetztes Signal
- **Stops**: Verwendet feste Stop-Loss- und Take-Profit-Niveaus
- **Standardwerte**:
  - `StopLevel` = 50m
  - `TakeLevel` = 50m
  - `SarStep` = 0.02m
  - `SarMax` = 0.2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `ReverseSignals` = false
- **Filter**:
  - Kategorie: Preisaktion
  - Richtung: Beide
  - Indikatoren: Parabolic SAR
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
