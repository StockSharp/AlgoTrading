# Einfaches APF-Strategie-Backtesting
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert ein vereinfachtes Autokorrelations-Preisvorhersage-Modell (APF). Sie erkennt Preiszyklen über Autokorrelation und prognostiziert zukünftige Preise mithilfe einer linearen Regression der jüngsten Renditen. Eine Long-Position wird eröffnet, wenn der vorhergesagte Gewinn einen angegebenen Schwellenwert überschreitet. Die Position wird geschlossen, wenn der Zielpreis erreicht ist.

## Parameter

- `Length` – Anzahl der Balken für Autokorrelation und Regression.
- `Threshold Gain` – minimaler erwarteter Kursanstieg für einen Einstieg.
- `Signal Threshold` – Autokorrelationsniveau, das zum Speichern einer Prognose erforderlich ist.
- `Candle Type` – Kerzentyp für Berechnungen.
