# MFI-Limit-Einstieg mit zeitgesteuerter Stornierung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Long-only-Diagramm zeigt den gesamten Lebenszyklus einer Pending Order. MFI(14) verlässt überverkauft, setzt einen Kauf unter den Schluss, N values zählt fünf Kerzen, Order cancellation storniert und Trades for order führt Füllungen zum 1%/1%-Schutz.

![schema](schema.svg)

## Strategieübersicht

- Das Aufwärtskreuzen von MFI über 20 modelliert den gespeicherten Besuch der überverkauften Zone.
- Bei Position == 0 wird eine Einheit bei Close × (1 − 0,5/100) gekauft.
- Ein Lebenszyklus-Flag blockiert neue Orders bis nach fünf Kerzen, sodass ein alter Timeout keine neue Order storniert.
- Bei Füllung speist Trades for order den Schutz; sonst storniert der Timer genau die registrierte Order.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: MFI kreuzt 20 aufwärts bei flacher Position; das Kauflimit liegt 0,5% unter dem fertigen Schluss.
- **Short-Einstieg**: Es gibt keinen Short-Einstieg, wie im C#.
- **Ausstieg**: Eine Füllung endet bei +1% oder −1%. Eine offene Order wird nach fünf fertigen Kerzen storniert, inklusive Signalkerze als erster.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candle Time Frame | 00:05:00 | Fertiges Intervall für MFI, Preis und Stornozähler. |
| MFI Period | 14 | Kerzenzahl des MoneyFlowIndex. |
| MFI Oversold Level | 20 | MFI-Niveau, dessen Aufwärtskreuzung den Einstieg aktiviert. |
| Replay Entry Offset, % | 0.5 | Abstand unter Schluss; C# 0,1%, Replay 0,5%. |
| Order Volume | 1 | Volumen des Pending-Kaufs. |
| Cancel After Candles | 5 | Fertige Kerzen bis zur Stornierung. |
| Take Profit, % | 1 | Prozentgewinn ab Füllung. |
| Stop Loss, % | 1 | Prozentverlust ab Füllung. |

## Diagrammdetails

- C# nutzt 0,1%. Kerzenmatching füllt innerhalb Low..High fast sofort; 0,5% macht die Stornierung im Replay sichtbar.
- Trades for order ist veraltet; empfohlen ist Trades von Order registering. Es erscheint nur hier einmal als historische Lektion.
- Der Timer und nicht die Füllung löst das Flag, daher kann ein alter Timer keine Ersatzorder stornieren.
- Der 20-Kerzen-Cooldown der Quelle ist vereinfacht weggelassen; die Fünf-Kerzen-Sperre verhindert Überlappung.
- Trotz Ordnername gibt es im C# kein Averaging; das Diagramm hat eine Order und kein Chart panel.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
