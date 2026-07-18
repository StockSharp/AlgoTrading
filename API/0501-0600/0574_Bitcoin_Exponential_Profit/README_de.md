# Bitcoin Exponentieller Gewinn-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie geht Long, wenn die schnelle EMA über die langsame EMA kreuzt. Die Positionsgröße wird aus einem Risikoprozentsatz des Kontoguthabens berechnet. Ausstiege erfolgen bei EMA-Kreuzung nach unten, Stop-Loss, Take-Profit oder Trailing-Stop.

## Details

- **Einstiegskriterien**:
  - Schnelle EMA kreuzt über langsame EMA → Long.
- **Long/Short**: Nur Long
- **Ausstiegskriterien**:
  - Schnelle EMA kreuzt unter langsame EMA.
  - Stop-Loss beim Risikoprozentsatz.
  - Take-Profit = Risiko × Gewinnmultiplikator.
  - Trailing-Stop vom höchsten Preis.
- **Stops**: SL, TP, Trailing-Stop
- **Standardwerte**:
  - Schnelle EMA-Länge = 9
  - Langsame EMA-Länge = 21
  - Risikoprozentsatz = 1
  - Gewinnmultiplikator = 2
  - Trailing-Stop-Prozentsatz = 0.5
- **Filter**:
  - Kategorie: Trend
  - Richtung: Long
  - Indikatoren: EMA
  - Stops: SL & TP & Trailing
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
