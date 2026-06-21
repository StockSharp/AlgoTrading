# VininI Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Beschreibung
Diese Strategie konvertiert den ursprünglichen MQL-Expert-Advisor **Exp_VininI_Trend** in StockSharp. Sie verwendet den Commodity Channel Index (CCI), um den VininI-Trend-Oszillator zu emulieren. Eine Long-Position wird eröffnet, wenn der Oszillator das obere Niveau überschreitet oder sich nach oben dreht. Eine Short-Position wird eröffnet, wenn der Oszillator unter das untere Niveau fällt oder sich nach unten dreht. Die Strategie verarbeitet nur abgeschlossene Kerzen.

## Parameter
- **CCI Period** – Länge des CCI-Indikators.
- **Upper Level** – Schwelle, die Kaufsignale auslöst.
- **Lower Level** – Schwelle, die Verkaufssignale auslöst.
- **Entry Modes** – `Breakdown` reagiert auf Niveaukreuzungen, `Twist` reagiert auf Richtungsänderungen.
- **Candle Type** – Kerzen-Zeitrahmen für Berechnungen.

## Original
Konvertiert aus der MQL5-Strategie unter `MQL/1365/exp_vinini_trend.mq5`.
