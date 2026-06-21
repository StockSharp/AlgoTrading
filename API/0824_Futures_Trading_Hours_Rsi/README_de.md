# Futures-RSI-Strategie für Handelszeiten
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt ausschließlich während der US-Futures-Sitzungszeiten (08:30–15:00 Uhr CT). Sie verwendet den Relative Strength Index (RSI), um long einzusteigen, wenn der Oszillator den überverkauften Level nach oben kreuzt, und short, wenn er den überkauften Level nach unten kreuzt. Um 15:00 Uhr CT oder danach werden alle offenen Positionen geschlossen.

## Details

- **Einstiegskriterien**:
  - **Long**: RSI kreuzt den überverkauften Level während der Sitzung nach oben
  - **Short**: RSI kreuzt den überkauften Level während der Sitzung nach unten
- **Long/Short**: Beide Seiten
- **Ausstiegskriterien**:
  - Alle Positionen werden am Sitzungsende geschlossen (15:00 Uhr CT)
- **Stops**: Keine
- **Standardwerte**:
  - `RsiLength` = 14
  - `OverSoldLevel` = 30
  - `OverBoughtLevel` = 70
  - `SessionStart` = 08:30
  - `SessionEnd` = 15:00
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
