# Beispiel zur Verarbeitung von Datum und Uhrzeit im StockSharp Strategy Designer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Dieses Beispiel im StockSharp Strategy Designer demonstriert eine ausgefeilte Konfiguration, die die Verarbeitung von Datum und Uhrzeit in eine Handelsstrategie integriert. Die Strategie nutzt zeitspezifische Bedingungen, um Handelsentscheidungen auf Basis von Kerzendaten und der Tageszeit zu treffen, und ist damit ein praxisnahes Beispiel für zeitkritische Trading-Szenarien.

![schema](schema.png)

## Beschreibung des Schemas

Das im JSON-File dargestellte Schema beschreibt eine komplexe Interaktion zwischen verschiedenen Knoten, die zeitbasierte Daten verarbeiten, um Handelsaktionen auszulösen:

1. **TimeFrameCandle-Knoten**: Verarbeitet [Kerzendaten](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) für einen bestimmten Zeitrahmen. Unverzichtbar für Strategien, die auf historischen Kursbewegungen basieren, um zukünftige Trends vorherzusagen.

2. **OpenTime- und CloseTime-Knoten**: [Extrahieren](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) Öffnungs- und Schlusszeiten aus den Kerzendaten, die für die Bestimmung der Zeiträume, in denen Handelsbedingungen ausgewertet werden, entscheidend sind.

3. **Vergleichsknoten (Equals, Greater Than)**: [Vergleichen](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) bestimmte Uhrzeiten (z. B. 14:00:00 oder 15:00:00) mit der aus den Kerzendaten extrahierten aktuellen Uhrzeit. Diese Konfiguration ermöglicht es der Strategie, sich zu aktivieren oder zu deaktivieren, je nachdem ob die angegebenen Zeiten übereinstimmen.

4. **Chart-Panel-Knoten**: Implementiert [Visualisierungskomponenten](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html), die Handelsdaten und Indikatoren in einem verständlichen Format anzeigen und die Echtzeit-Entscheidungsfindung sowie Strategieanpassungen unterstützen.

5. **Handelsknoten (Kauf, Verkauf)**: Werden aktiviert, wenn bestimmte Zeitbedingungen erfüllt sind, und ermöglichen der Strategie die Ausführung von [Kauf- oder Verkaufsorders](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) auf Basis der Vergleichsergebnisse und der in der Strategie definierten Handelslogik.

## Arbeitsablauf

- Der **TimeFrameCandle-Knoten** erfasst und verarbeitet Kerzendaten in regelmäßigen Intervallen.
- **OpenTime- und CloseTime-Knoten** analysieren diese Daten, um spezifische Zeitpunkte zu extrahieren.
- **Vergleichsknoten** prüfen diese Zeiten gegen vordefinierte Werte (z. B. 14:00:00 als Einstiegsbedingung und 15:00:00 als Ausstiegsbedingung).
- Wenn Bedingungen erfüllt sind (z. B. aktuelle Uhrzeit entspricht 14:00:00), werden Handelsknoten (Kauf oder Verkauf) ausgelöst, um gemäß der Strategie-Logik Trades auszuführen.
- Der **Chart-Panel-Knoten** stellt diese Trades und Kerzendaten visuell dar und bietet einen klaren Überblick über die Strategie-Operationen und Marktbedingungen.

## Praktische Anwendung

Diese Konfiguration ist besonders nützlich für Strategien, die Trades zu bestimmten Tageszeiten ausführen müssen, wie zum Beispiel:
- **Opening Range Breakouts**, bei denen Trades rund um die Eröffnung einer Marktsitzung platziert werden.
- **Closing Auction-Strategien**, die auf Kursbewegungen und Liquiditätsveränderungen beim Börsenschluss abzielen.

## Fazit

Dieses Beispiel aus dem StockSharp Strategy Designer veranschaulicht einen robusten Rahmen für die Entwicklung zeitkritischer Handelsstrategien, die automatisch zu vordefinierten Zeiten Trades ausführen können. Es ist eine hervorragende Demonstration, wie Trader die Fähigkeiten des Strategy Designers nutzen können, um komplexe, regelbasierte Handelsstrategien zu erstellen, die dynamisch auf Echtzeit-Marktdaten und spezifische zeitliche Bedingungen reagieren.
