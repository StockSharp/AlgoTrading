# Ultimate-Strategie-Vorlage
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

RSI-Momentumstrategie mit zwei EMAs als Trendfilter. Signale werden anhand abgeschlossener Kerzen ausgewertet; prozentuale Take-Profit- und Stop-Loss-Absicherungen bleiben während der Signalpause von 80 Kerzen aktiv.

## Details

- **Einstiegskriterien**: Long, wenn der RSI die 50 nach oben kreuzt und der schnelle EMA über dem langsamen liegt; Short beim Kreuzen nach unten, wenn der schnelle EMA unter dem langsamen liegt.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Entgegengesetzte RSI-Kreuzung, Take-Profit oder Stop-Loss.
- **Stops**: Prozentualer Stop-Loss und Take-Profit.
- **Signalpause**: 80 abgeschlossene Kerzen nach einem Signaleinstieg oder RSI-Ausstieg; Take-Profit und Stop-Loss bleiben dabei aktiv.
- **Standardwerte**:
  - `FastLength` = 9
  - `SlowLength` = 21
  - `StopLossPercent` = 1
  - `TakeProfitPercent` = 3
  - `CandleType` = 5 Minuten
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: RSI, EMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittel
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
