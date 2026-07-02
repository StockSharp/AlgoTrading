# Beispiel zur Verarbeitung von Markttiefe im StockSharp Strategy Designer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Dieses Beispiel zeigt eine Konfiguration innerhalb des StockSharp Strategy Designers, die sich auf die Verarbeitung von Markttiefen-Daten konzentriert. Markttiefen-Daten, häufig als „Orderbuch" bezeichnet, enthalten Informationen über Kauf- und Verkaufsorders auf verschiedenen Preisniveaus für ein Wertpapier. Sie sind für Strategien unerlässlich, die Angebots- und Nachfragedynamiken auf verschiedenen Preisniveaus in Echtzeit analysieren müssen.

![schema](schema.png)

## Schemabeschreibung

Das Schema umfasst mehrere miteinander verbundene Komponenten, die zum Abrufen, Verarbeiten und Anzeigen von Markttiefendaten konzipiert sind:

1. **Instrument-Knoten**: Dieser Knoten repräsentiert das [Wertpapier](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) (z. B. eine Aktie, ein Future oder ein anderes Finanzinstrument), für das die Markttiefe abgerufen wird. Dies ist ein grundlegendes Element, da es definiert, welcher Markt oder welches Instrument analysiert wird.

2. **TimeFrameCandle-Knoten**: Verarbeitet [Kerzendaten](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) für das Wertpapier, aggregiert über einen festgelegten Zeitrahmen (5 Minuten im Beispiel). Kann zur Korrelation von Markttiefen-Veränderungen mit Kursbewegungen verwendet werden.

3. **Markttiefe-Knoten**: Sind darauf ausgelegt, Echtzeit-Veränderungen in der [Markttiefe](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/market_depths/order_book.html) zu erfassen und darauf zu reagieren. Beinhaltet Einstellungen zur Verarbeitung eingehender Markttiefendaten mit Einblicken in aktuelle Kauf- und Verkaufsorders.

4. **Chart-Panel-Knoten**: Deutet darauf hin, dass Kerzendaten auf einem [Chart](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html) visualisiert werden. Dies hilft Tradern oder Algorithmen, die Marktlage besser zu verstehen und fundierte Entscheidungen zu treffen.

5. **Markttiefe-Panel-Knoten**: Speziell auf die Darstellung der Markttiefendaten in einem [speziellen Panel](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/market_depths/order_book_panel.html) ausgerichtet, mit Funktionen wie der Hervorhebung der besten Geld- und Briefkurse sowie der Visualisierung der Markttiefe.

## Arbeitsablauf

- Der **Instrument-Knoten** liefert Daten, die als Eingabe für den **TimeFrameCandle-Knoten** und den **Markttiefe-Knoten** verwendet werden.
- Der **TimeFrameCandle-Knoten** verarbeitet diese Daten zur Erzeugung von Kerzen für den angegebenen Zeitrahmen, die für Trendanalysen oder andere technische Analysen genutzt werden können.
- Der **Markttiefe-Knoten** verarbeitet die Echtzeit-Markttiefe des angegebenen Wertpapiers. Er kann verwendet werden, um Handelsentscheidungen auf Basis bestimmter Bedingungen auszulösen, etwa bei einem starken Ungleichgewicht zwischen Kauf- und Verkaufsorders auf bestimmten Preisniveaus.
- Die Visualisierung erfolgt über den **Chart-Panel-Knoten** und den **Markttiefe-Panel-Knoten**, sodass die Daten nicht nur für die Handelslogik verarbeitet, sondern auch für die Überwachung durch menschliche Trader zugänglich gemacht werden.

## Praktische Anwendung

Diese Konfiguration kann in verschiedenen Handelsstrategien eingesetzt werden, darunter:
- **Hochfrequenzhandel (HFT)**, bei dem geringfügige Veränderungen in der Orderbuch-Dynamik auf potenzielle profitable Trades hinweisen können.
- **Arbitrage-Strategien**, die den Vergleich von Orderbüchern über mehrere Börsen hinweg beinhalten können, um Preisunterschiede auszunutzen.
- **Market-Making-Strategien**, bei denen das Verständnis beider Seiten des Orderbuchs entscheidend für die Festlegung geeigneter Kauf- und Verkaufsorders ist.

## Fazit

Das im JSON-File beschriebene Schema zeigt einen umfassenden Ansatz zur Verarbeitung von Markttiefendaten im StockSharp Strategy Designer. Durch die Integration von Echtzeit-Datenverarbeitung mit ausgefeilten Visualisierungstools hilft diese Konfiguration Tradern und Algorithmen, schnelle, datengestützte Entscheidungen auf Basis des Orderbuchzustands zu treffen. Dieses Beispiel dient als robuste Grundlage für die Entwicklung komplexerer Handelsstrategien, die tiefe Einblicke in die Marktdynamik erfordern.
