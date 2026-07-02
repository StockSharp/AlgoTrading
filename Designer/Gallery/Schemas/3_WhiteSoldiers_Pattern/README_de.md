# Beispiel zur Erkennung des Three-White-Soldiers-Musters im StockSharp Strategy Designer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Dieses Beispiel demonstriert die Implementierung einer Handelsstrategie im StockSharp Strategy Designer, die das Candlestick-Muster „Three White Soldiers" nutzt. Dieses Muster wird häufig als bullisches Umkehrsignal interpretiert und kann für Trader, die von Impulswechseln profitieren möchten, entscheidend sein. Die im JSON-Schema beschriebene Konfiguration umfasst die Erkennung dieses Musters und die Einleitung von Trades bei seinem Auftreten.

![schema](schema.png)

## Beschreibung des Schemas

Das Schema beschreibt einen komplexen Workflow, der dazu dient, das „Three White Soldiers"-[Muster](https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/pattern.html) zu erkennen und entsprechend Trades auszuführen. Hier sind die wichtigsten Komponenten und ihre Rollen:

1. **Security Node**: Gibt das [Wertpapier](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) an, für das die Strategie angewendet wird. Dient als primäre Dateneingabequelle und liefert die Marktdaten für die anschließende Analyse.

2. **TimeFrameCandle Node**: Generiert [Kerzendaten](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) für das angegebene Wertpapier. Dieser Node ist entscheidend, da er eingehende Marktdaten in ein nutzbares Format (Kerzen) verarbeitet, das der Mustererkennungsalgorithmus analysieren kann.

3. **Mustererkennungs-Node**: Speziell konfiguriert, um das „Three White Soldiers"-[Muster](https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/pattern.html) über einen [Indikator](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) zu erkennen. Dieser Node analysiert die Kerzendaten und löst eine Aktion aus, wenn das Muster identifiziert wird.

4. **Chart Panel Node**: Visualisiert die Handelsdaten, einschließlich Candlestick-Muster und möglicherweise von der Strategie ausgeführte Trades. Diese [Komponente](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html) hilft bei der Überwachung der Strategieleistung und dem Verständnis, wie das Muster Handelsentscheidungen beeinflusst.

5. **Handels-Nodes (Kaufen, Verkaufen)**: Diese [Nodes](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) sind konfiguriert, um Trades auszuführen, wenn das Muster erkannt wird. Die Aktionen können je nach zusätzlichen Bedingungen in der Strategie variieren, etwa Marktbedingungen oder anderen technischen Indikatoren.

## Workflow

- Der **Security Node** speist Marktdaten in den **TimeFrameCandle Node**, wo die Daten in Kerzen umgewandelt werden.
- Diese Kerzen werden dann an den **Mustererkennungs-Node** weitergeleitet, der konfiguriert ist, um das „Three White Soldiers"-Muster zu identifizieren.
- Bei der Erkennung des Musters kann der Node einen oder mehrere **Handels-Nodes** auslösen, um Kauf- oder Verkaufsorders entsprechend dem Strategiedesign auszuführen.
- Der **Chart Panel Node** bietet eine Echtzeit-Visualisierung der Kerzen und ausgeführten Trades, die bei der Bewertung der Strategieeffektivität und bei Anpassungen hilft.

## Praktische Anwendung

Diese Konfiguration ist besonders nützlich für Trader, die sich auf impulsbasierte Strategien spezialisieren, bei denen frühzeitiges Erkennen von Mustern zu erheblichen Gewinnen führen kann. Das „Three White Soldiers"-Muster ist ein starker Indikator für eine bullische Umkehr, was diese Strategie geeignet macht für:
- Swing-Trading in Märkten, wo Impulswechsel stark und klar ausgeprägt sind.
- Day-Trading in hochvolatilen Märkten, wo frühe Erkennung von Trendumkehrungen zu profitablen Trades führen kann.

## Fazit

Dieses Beispiel aus dem StockSharp Strategy Designer veranschaulicht die anspruchsvolle Nutzung der Candlestick-Mustererkennung im Kontext des algorithmischen Handels. Durch die Automatisierung der Erkennung von Mustern wie „Three White Soldiers" können Trader sich effektiver am Markt positionieren und die Vorhersagekraft historischer Preismuster nutzen. Die detaillierte Visualisierung und Echtzeit-Datenverarbeitung unterstützen auch die Verfeinerung der Strategie basierend auf beobachteten Marktbedingungen und Ergebnissen.
