# Stochastic-Histogramm-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein StockSharp-Port des originalen MQL-Experten `Exp_Stochastic_Histogram`.
Sie verwendet den Stochastic-Oszillator, um in zwei Modi konträre Handelssignale zu erzeugen:

- **Levels** – Ein Signal erscheint, wenn %K die durch `HighLevel` und `LowLevel` definierten überkauften oder überverkauften Bereiche verlässt.
- **Cross** – Ein Signal erscheint, wenn %K die %D-Linie kreuzt. Der Trade wird in der entgegengesetzten Richtung der Kreuzung eröffnet.

Wann immer ein neues Signal empfangen wird, schließt die Strategie eine bestehende Position und eröffnet eine neue in der erforderlichen Richtung.

## Parameter

- `KPeriod` – Haupt-%K-Zeitraum.
- `DPeriod` – %D-Glättungszeitraum.
- `Slowing` – Zusätzliche Glättung von %K.
- `HighLevel` – Obere Schwelle für den Levels-Modus.
- `LowLevel` – Untere Schwelle für den Levels-Modus.
- `Mode` – Levels oder Cross.
- `CandleType` – Kerzen-Zeitrahmen für Berechnungen.

## Funktionsweise

Für jede abgeschlossene Kerze wird der Stochastic-Oszillator aktualisiert und bewertet. Im **Levels**-Modus wird ein Long-Trade eröffnet, wenn %K unter das hohe Niveau zurückkehrt, und ein Short-Trade, wenn %K über das niedrige Niveau steigt. Im **Cross**-Modus wird ein Long-Trade bei abwärts gerichteten Kreuzungen von %K unter %D eröffnet, während aufwärts gerichtete Kreuzungen Short-Trades auslösen. Die Strategie hat jederzeit höchstens eine offene Position.
