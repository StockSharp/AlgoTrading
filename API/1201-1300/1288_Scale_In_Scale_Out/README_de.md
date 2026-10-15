# Strategie zur schrittweisen Positionsaufstockung und -reduzierung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie baut eine Position schrittweise auf, indem sie auf jeder Kerze einen festen Prozentsatz des verfügbaren Kapitals investiert. Wenn der Positionswert ein konfigurierbares Gewinnniveau erreicht, wird ein Teil der Position verkauft und optional ein Teil des realisierten Gewinns zurückgelegt.

## Details

- **Einstiegskriterien**: Immer kaufen, wenn Kapital verfügbar ist.
- **Ausstiegskriterien**: Verkaufen, wenn der Gewinnprozentsatz den Schwellenwert überschreitet.
- **Long/Short**: Nur Long.
- **Standardwerte**:
  - `Buy Scaling Size %` = 2
  - `Take Profit Level %` = 50
  - `Take Profit Size %` = 1
  - `Retain Profit Portion %` = 50
  - `Minimum Position Value` = 200000
  - `Minimum Buy Value` = 100
- **Filter**:
  - Kategorie: Sonstige
  - Richtung: Long
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
