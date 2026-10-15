# Genie Pivot Fest-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert das „Genie"-Pivot-Punkt-Umkehr-Scalping-System, das ursprünglich in MQL4 geschrieben wurde. Es scannt die letzten acht Kerzen, um plötzliche Umkehrungen an Pivot-Punkten zu erkennen. Ein Long-Trade wird ausgelöst, wenn sieben aufeinanderfolgende Tiefs abnehmen und die aktuelle Kerze ein höheres Tief bildet und oberhalb des vorherigen Hochs schließt. Ein Short-Trade wird ausgelöst, wenn sieben aufeinanderfolgende Hochs zunehmen und die aktuelle Kerze ein niedrigeres Hoch bildet und unterhalb des vorherigen Tiefs schließt.

Die Strategie verwendet eine feste Positionsgröße (Strategy.Volume) und wendet sowohl einen Trailing-Stop als auch einen Take-Profit an, die in absoluten Preiseinheiten gemessen werden. Diese Parameter können optimiert werden und ermöglichen es, schnelle Umkehrungen zu erfassen und dabei offene Gewinne zu schützen.

## Details

- **Einstiegskriterien**:
  - **Long**: `Low[7] > Low[6] > ... > Low[1]` && `Low[1] < Low[0]` && `High[1] < Close[0]`.
  - **Short**: `High[7] < High[6] < ... < High[1]` && `High[1] > High[0]` && `Low[1] > Close[0]`.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Trailing-Stop oder Take-Profit wird erreicht.
- **Stops**:
  - Take-Profit: absoluter Abstand vom Einstieg.
  - Trailing-Stop: absoluter Abstand, der mit dem Trade mitläuft.
- **Standardwerte**:
  - `TakeProfit` = 500.
  - `TrailingStop` = 200.
  - `CandleType` = 1 Minute.
- **Filter**:
  - Kategorie: Umkehr.
  - Richtung: Beide.
  - Indikatoren: Keine.
  - Stops: Ja.
  - Komplexität: Einfach.
  - Zeitrahmen: Kurzfristig.
  - Saisonalität: Nein.
  - Neuronale Netze: Nein.
  - Divergenz: Nein.
  - Risikolevel: Mittel.
