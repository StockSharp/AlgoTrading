# EMA Moving Away-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

EMA Moving Away verfolgt, wie weit sich der Preis von einem exponentiellen gleitenden
Durchschnitt entfernt. Wenn eine Reihe von Kerzen den Preis um einen festgelegten
Prozentsatz vom EMA weg drückt, wettet die Strategie auf eine Rückkehr zum Mittelwert.

Das Setup konzentriert sich auf die Long-Seite: Nach einer ausgedehnten bärischen
Bewegung, die den Preis um `MovingAwayPercent` unter den EMA treibt, wird eine
Position eröffnet. Körpergröße- und Serienfilter können hinzugefügt werden, um
sicherzustellen, dass die Bewegung überdehnt ist und nicht nur Rauschen. Ein
prozentualer Stop-Loss schützt das Kapital, wenn die Umkehr ausbleibt.

## Details
- **Daten**: Kurskerzen.
- **Einstiegskriterien**:
  - **Long**: Schlusskurs unter EMA um `MovingAwayPercent` mit erforderlichen Serien-/Größenfiltern.
  - **Short**: nicht verwendet.
- **Ausstiegskriterien**: Rückkehr zum EMA oder Stop-Loss ausgelöst.
- **Stops**: Prozentualer Stop basierend auf `StopLossPercent`.
- **Standardwerte**:
  - `EmaLength` = 55
  - `MovingAwayPercent` = 2.0
  - `StopLossPercent` = 2.0
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Nur Long
  - Indikatoren: EMA
  - Komplexität: Moderat
  - Risikolevel: Mittel
