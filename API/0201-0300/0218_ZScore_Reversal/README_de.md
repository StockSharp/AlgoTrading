# Z-Score-Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Z-Score-Umkehr-Strategie misst, wie weit der Preis in Standardabweichungen von einem gleitenden Durchschnitt abweicht. Der resultierende Z-Score hebt statistisch überdehnte Bedingungen hervor, die möglicherweise zur Mitte zurückschnappen.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 91%. Sie funktioniert am besten am Aktienmarkt.

Ein Long-Trade wird eröffnet, wenn der Z-Score unter einen negativen Schwellenwert fällt, was einen überverkauften Markt signalisiert. Ein Short-Trade wird eingegangen, wenn der Z-Score über den positiven Schwellenwert steigt. Die Position wird geschlossen, sobald der Z-Score wieder durch null kreuzt, was darauf hinweist, dass sich der Preis normalisiert hat.

Diese Technik ist für Mean-Reversion-Trader attraktiv, die objektive Einstiegsniveaus bevorzugen. Der Stop-Loss-Prozentsatz hält ungünstige Bewegungen beherrschbar, während auf die Umkehr gewartet wird.

## Details
- **Einstiegskriterien**:
  - **Long**: Z-Score < -Schwellenwert
  - **Short**: Z-Score > Schwellenwert
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg, wenn Z-Score über 0 kreuzt
  - **Short**: Ausstieg, wenn Z-Score unter 0 kreuzt
- **Stops**: Ja, prozentualer Stop-Loss.
- **Standardwerte**:
  - `LookbackPeriod` = 20
  - `ZScoreThreshold` = 2.0m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(10)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Z-Score
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
