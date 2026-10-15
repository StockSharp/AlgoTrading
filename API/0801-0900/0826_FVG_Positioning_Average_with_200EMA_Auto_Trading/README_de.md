# FVG Positionierungsdurchschnitt mit 200EMA Auto-Trading-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie mittelt die Niveaus von bullischen und bärischen Fair-Value-Gaps (FVG) und kombiniert sie mit einer EMA über 200 Perioden. Ein Trade wird eröffnet, wenn der Preis diese Durchschnitte in Trendrichtung kreuzt.

## Details

- **Einstiegskriterien**:
  - **Long**: Preis kreuzt über den Durchschnitt der bärischen FVGs und alle Durchschnitte liegen über der EMA.
  - **Short**: Preis kreuzt unter den Durchschnitt der bullischen FVGs und alle Durchschnitte liegen unter der EMA.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Stop-Loss am letzten Tief/Hoch.
  - Take-Profit nach dem Risiko-Ertrags-Verhältnis.
- **Stops**: Ja.
- **Standardwerte**:
  - `FvgLookback` = 30
  - `AtrMultiplier` = 0.25
  - `LookbackPeriod` = 20
  - `EmaPeriod` = 200
  - `RiskReward` = 1.5
- **Filter**:
  - Kategorie: Price action
  - Richtung: Beide
  - Indikatoren: ATR, EMA, SMA, Highest, Lowest
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
