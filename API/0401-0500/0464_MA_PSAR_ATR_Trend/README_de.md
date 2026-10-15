# MA PSAR ATR Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die MA PSAR ATR Trend-Strategie kombiniert einen gleitenden-Durchschnitt-Kreuzung mit einem täglichen Parabolic SAR-Filter. Trades werden nur dann eingegangen, wenn der Preis über oder unter beiden Durchschnitten ausgerichtet ist und der PSAR übereinstimmt. Ein ATR-basierter Stop kontrolliert das Risiko.

Die Methode eignet sich für Trader, die Trendfolge mit dynamischen Stops suchen. Signale werden standardmäßig auf 5-Minuten-Kerzen ausgelöst.

## Details
- **Einstiegskriterien**:
  - **Long**: Schnelle MA > Langsame MA, Schlusskurs > Schnelle MA, Tief > Täglicher PSAR
  - **Short**: Schnelle MA < Langsame MA, Schlusskurs < Schnelle MA, Hoch < Täglicher PSAR
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Trend wird bärisch oder Preis fällt unter ATR-Stop
  - **Short**: Trend wird bullisch oder Preis steigt über ATR-Stop
- **Stops**: Ja, ATR-basiert.
- **Standardwerte**:
  - `FastMaPeriod` = 40
  - `SlowMaPeriod` = 160
  - `SarStep` = 0.02m
  - `SarMaxStep` = 0.2m
  - `AtrPeriod` = 14
  - `AtrMultiplierLong` = 2m
  - `AtrMultiplierShort` = 2m
  - `UsePsarFilter` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MA, Parabolic SAR, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
