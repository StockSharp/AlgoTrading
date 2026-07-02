# High-Break-Strategie-Beispiel im StockSharp Strategy Designer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die im bereitgestellten JSON-Schema dargestellte „High Break"-Strategie ist dafür konzipiert, Trades auf Basis spezifischer Bedingungen im Zusammenhang mit Kursbewegungen und Zeitrahmen auszuführen, und verwendet dabei den StockSharp Strategy Designer. Dieses Beispiel zeigt, wie eine Handelsstrategie eingerichtet wird, die potenzielle Kaufgelegenheiten identifiziert, wenn der Kurs eines Wertpapiers ein vorbestimmtes Hoch über einen bestimmten Zeitraum überschreitet.

![schema](schema.png)

## Beschreibung des Schemas

Das Schema beschreibt eine Sequenz von miteinander verbundenen Komponenten, die dazu dienen, Echtzeit-Marktdaten zu erfassen, zu analysieren und darauf zu reagieren:

1. **Security Node**: Dient als Grundlage und gibt das [Wertpapier](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) (z. B. Aktien, Futures) an, auf das die Strategie angewendet wird. Dieser Node ist entscheidend, da er den Dateneingabe für die Strategie bestimmt.

2. **TimeFrameCandle Node**: Verarbeitet eingehende Marktdaten und organisiert sie in [Kerzen basierend](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) auf einem angegebenen Zeitrahmen. Dieser Node ist für Strategien, die historische Preisanalysen zur Handelsentscheidung nutzen, unverzichtbar.

3. **Highest Node**: Analysiert die Kerzendaten, um [den höchsten Kurs](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) über einen bestimmten Zeitraum (z. B. 60 Minuten) zu ermitteln. Dieser Wert setzt einen Benchmark für die Identifizierung bedeutender Kursausbrüche.

4. **Vergleichs-Node**: [Vergleicht](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) aktuelle Kurse mit dem historischen Hoch, das der Highest Node ermittelt hat. Wenn der aktuelle Kurs dieses Hoch überschreitet, löst er ein potenzielles Handelssignal aus.

5. **Chart Panel Node**: [Visualisiert](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html) die Kursdaten und die Aktionen der Strategie und liefert eine grafische Darstellung des Strategiebetriebs, die bei Überwachung und Anpassungen hilft.

6. **Handelsausführungs-Nodes (Kaufen/Verkaufen)**: Verantwortlich für die [Ausführung von Trades](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html), wenn die Bedingungen der Strategie erfüllt sind. Beispielsweise kann eine Kauforder ausgeführt werden, wenn der Kurs das historische Hoch überschreitet.

## Workflow

- Der **Security Node** speist Marktdaten in den **TimeFrameCandle Node**, um einen strukturierten zeitbasierten Kerzendatensatz zu erstellen.
- Der **Highest Node** berechnet den höchsten Kurs aus diesen Kerzen über einen definierten Zeitraum.
- Der **Vergleichs-Node** vergleicht kontinuierlich den aktuellen Kurs mit diesem Hoch. Wenn der aktuelle Kurs das historische Hoch überschreitet, deutet dies auf einen bullischen Ausbruch hin und löst potenziell ein Kaufsignal aus.
- Der **Chart Panel Node** bietet Echtzeit-Visualisierung und ermöglicht sofortiges visuelles Feedback zur Strategieleistung und den Marktbedingungen.
- Wenn die Kaufbedingung erfüllt ist, initiiert der **Handelsausführungs-Node** (Kaufen) einen Trade, der den erwarteten Aufwärtstrend nutzt.

## Praktische Anwendung

Diese Konfiguration ist besonders nützlich für Trader, die sich auf Ausbruchsstrategien spezialisieren, bei denen das Erkennen und Reagieren auf Kursbewegungen über bestimmten Schwellenwerten zu profitablen Trades führen kann. Solche Strategien sind in volatilen Märkten beliebt, wo Kursausbrüche auf starke Trends hinweisen können.

## Fazit

Das „High Break"-Strategie-Beispiel im StockSharp Strategy Designer veranschaulicht eine anspruchsvolle Nutzung von Marktdaten, um Handelsentscheidungen auf Basis identifizierter Kursbewegungen zu automatisieren. Durch die Nutzung von Echtzeit-Datenverarbeitung und Visualisierungswerkzeugen hilft die Strategie Tradern, Marktchancen durch Kursausbrüche effizient zu nutzen. Dieses Beispiel demonstriert nicht nur die Stärke der StockSharp-Plattform bei der Entwicklung dynamischer Handelsstrategien, sondern dient auch als Grundlage für weitere Anpassungen und Optimierungen basierend auf individuellen Handelsanforderungen und Marktbedingungen.
