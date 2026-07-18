# Exp 3XMA Ichimoku-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine Konvertierung des MQL-Experten `exp_3xma_ishimoku`. Sie verwendet den Ichimoku-Indikator mit verkürzten Perioden und handelt konträr zu Cloud-Ausbrüchen.

Die Kijun-Linie wird mit den Ichimoku-Cloud-Grenzen verglichen. Wenn Kijun von oberhalb der Cloud in sie hineinfällt, schließt die Strategie Short-Positionen und eröffnet eine Long-Position, wenn Kaufen erlaubt ist. Wenn Kijun von unterhalb der Cloud in sie hinaufsteigt, werden Long-Positionen geschlossen und eine Short-Position kann eröffnet werden.

Der Standard-Analysezeitraum sind 4-Stunden-Kerzen.

## Parameter
- **Tenkan Period** – Länge der Tenkan-sen-Linie.
- **Kijun Period** – Länge der Kijun-sen-Linie.
- **Senkou Span B Period** – Periode der zweiten Senkou-Spanne.
- **Allow Buy** – Long-Positionen eröffnen aktivieren.
- **Allow Sell** – Short-Positionen eröffnen aktivieren.
- **Candle Type** – Kerzen-Serie für die Indikatorberechnung.

## Funktionsweise
1. Abonniert die ausgewählte Kerzen-Serie und bindet den Ichimoku-Indikator.
2. Verarbeitet nur abgeschlossene Kerzen.
3. Erkennt, wenn die Kijun-Linie die Cloud-Grenzen kreuzt.
4. Schließt Gegenpositionen und eröffnet eine neue in Signalrichtung, wenn erlaubt.

## Haftungsausschluss
Dieses Beispiel dient ausschließlich Bildungszwecken und stellt keine Finanzberatung dar. Verwendung auf eigenes Risiko.
