# Strategie Volumen-Gewichtete MA-Steigung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie **Volumen-Gewichtete MA-Steigung** analysiert die Richtung des volumengewichteten gleitenden Durchschnitts (VWMA). Das System eröffnet eine Long-Position, wenn die VWMA zwei aufeinanderfolgende Balken steigt, und öffnet eine Short-Position, wenn die VWMA zwei Balken fällt. Bestehende Positionen werden geschlossen, sobald sich die Indikatorsteigung umkehrt.

Dieser Ansatz versucht, entstehende Trends zu verfolgen, indem volumengewichtete Preisdurchschnitte verwendet werden und Bewegungen bei niedrigem Volumen herausgefiltert werden.

## Details

- **Einstiegskriterien**: VWMA steigt zwei Balken (Long) oder fällt zwei Balken (Short).
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzte VWMA-Steigung.
- **Stops**: Ja (konfigurierbar, Standard 1% Stop-Loss / 2% Take-Profit).
- **Standardwerte**:
  - `VwmaPeriod` = 12
  - `CandleType` = TimeSpan.FromHours(4)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: VWMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Swing
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
