# Maximus vX Lite-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie versucht, Ausbrüche aus kurzfristigen Konsolidierungszonen zu handeln. Sie sucht nach kompakten Preisbereichen im 15-Minuten-Chart und eröffnet Trades, wenn der Preis diese Bereiche um eine bestimmte Distanz verlässt.

## Strategielogik

1. Für jede abgeschlossene 15-Minuten-Kerze werden das höchste Hoch und das niedrigste Tief der letzten 40 Kerzen berechnet.
2. Wenn der Abstand zwischen diesen Extremen unter dem Parameter **Range** liegt, wird eine Konsolidierungszone angenommen.
3. Nach Ablauf der **Delay Open**-Periode ohne neue Trades löst ein Ausbruch über die obere Grenze plus **Distance** Punkte eine Long-Position aus, während ein Ausbruch unter die untere Grenze minus **Distance** Punkte eine Short-Position auslöst.
4. Ein fester **Stop Loss** und ein Trailing-Stop von **Trail** Punkten werden angewandt, sobald eine Position eröffnet wird.
5. Konsolidierungsgrenzen werden nach Ablauf der **Period**-Stunden zurückgesetzt.

## Parameter

- `DelayOpen` – Stunden Wartezeit vor der Eröffnung eines neuen Trades.
- `Distance` – Ausbruchsdistanz von der Konsolidierungsgrenze in Punkten.
- `Period` – Stunden, nach denen Konsolidierungsniveaus neu berechnet werden.
- `Range` – Maximale Größe der Konsolidierungszone in Punkten.
- `StopLoss` – Anfänglicher Stop-Loss in Punkten.
- `Trail` – Trailing-Stop-Distanz in Punkten.

## Hinweise

Die Strategie verwendet ausschließlich die High-Level-API: Kerzen werden über `SubscribeCandles` empfangen, und Indikatorwerte werden mit `Bind` gebunden. Orders werden mit den Methoden `BuyMarket` und `SellMarket` gesendet. Kommentare im Quellcode sind auf Englisch verfasst.
