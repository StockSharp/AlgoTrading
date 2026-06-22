# Color XCCX Candle-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konvertiert aus MQL-Code `MQL/14260`.

Diese Strategie vergleicht zwei einfache gleitende Durchschnitte (SMA), die aus den Eröffnungs- und Schlusskursen der Kerzen berechnet werden. Wenn der aus den Schlusskursen berechnete SMA den auf den Eröffnungskursen basierenden SMA von unten nach oben kreuzt, wird eine Long-Position eröffnet. Wenn der schlussbasierte SMA den eröffnungsbasierten SMA von oben nach unten kreuzt, wird eine Short-Position eröffnet. Vor dem Öffnen einer neuen Position wird jede bestehende entgegengesetzte Position geschlossen.

Parameter:

- `SMA Length` – Anzahl der Kerzen zur Berechnung beider SMAs.
- `Candle Type` – Zeitrahmen für eingehende Kerzen.
- `Stop Loss %` – Stop-Loss-Größe als Prozentsatz des Einstiegspreises.
- `Take Profit %` – Take-Profit-Größe als Prozentsatz des Einstiegspreises.

Die Strategie verwendet die High-Level StockSharp API, um Kerzen zu abonnieren und Indikatoren zu binden. Sie stellt auch beide SMAs und ausgeführte Trades im Chart dar, wenn Visualisierung verfügbar ist.
