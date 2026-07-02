# Diagramm zur Erstellung eines zusammengesetzten Index aus mehreren Kerzenserien
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Diagrammdatei veranschaulicht eine Strategie zur Erstellung eines zusammengesetzten Index aus Kerzenserien verschiedener Finanzinstrumente mithilfe der Strategiegalerie der Designer-Plattform. Die Strategie aggregiert Daten verschiedener Wertpapiere, um einen einheitlichen Index zu bilden, der zur Beurteilung der allgemeinen Marktstimmung oder der Sektorentwicklung genutzt werden kann.

![schema](schema.png)

## Strategieübersicht

Die Strategie kombiniert Preisdaten mehrerer Wertpapiere zu einem einzigen [Index](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/index.html). Dabei werden typischerweise Normalisierungs- oder Gewichtungsverfahren eingesetzt, um sicherzustellen, dass jedes Wertpapier proportional zum endgültigen Indexwert beiträgt.

## Komponenten des Diagramms

- **Datenerhebungsknoten**: sind für den Abruf der [Kerzendaten](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) für jedes ausgewählte Wertpapier zuständig.
- **Normalisierungsknoten**: wenden Normalisierung auf die Kerzendaten an, um einen gleichmäßigen Einfluss auf die endgültige [Indexberechnung](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/index.html) zu gewährleisten und die Auswirkungen unterschiedlicher Preisskalen zu mildern.
- **Gewichtungsknoten**: weisen jedem Wertpapier anhand vordefinierter Kriterien wie Marktkapitalisierung oder historischer Volatilität Gewichte zu.
- **Indexberechnungsknoten**: aggregiert die normalisierten und gewichteten Preisdaten, um den endgültigen Indexwert zu berechnen.

## Ein- und Ausstiegspunkte

- **Einstiegspunkte**: traditionelle Einstiegspunkte existieren in der Regel nicht, da diese Strategie keine direkten Handelsentscheidungen beinhaltet.
- **Ausgabe**: die Hauptausgabe ist der Echtzeit-Indexwert, der die kollektive Bewegung der enthaltenen Wertpapiere widerspiegelt.

## Verwendung

Trader und Analysten können dieses Diagramm nutzen, um:
- die Gesamtentwicklung eines bestimmten Sektors oder Marktes durch die Erstellung eines benutzerdefinierten Index zu beobachten;
- einzelne Wertpapiere mit dem breiteren Marktindex zu vergleichen, um über- oder unterdurchschnittliche Entwicklungen zu identifizieren;
- den benutzerdefinierten Index als Benchmark für die Portfolioentwicklung zu verwenden.

## Bildungswert

Dieses Strategie-Diagramm ist besonders wertvoll für Bildungszwecke und bietet Einblicke in:
- die Mechanik der Indexberechnung sowie die Bedeutung der Datennormalisierung und -gewichtung in der Finanzanalyse;
- die Anwendung kombinierter Daten aus mehreren Quellen zur Erstellung aussagekräftiger Finanzkennzahlen.

Benutzer können dieses Diagramm in die Designer-Plattform importieren, um den Ansatz zu erkunden und zu modifizieren, ihn an verschiedene Wertpapiersets anzupassen oder die Komplexität der Indexberechnungsmethodik zu erhöhen.

Diese Datei ist Teil einer vielfältigen Strategiensammlung der Designer-Plattform, die darauf abzielt, das Verständnis der Benutzer für die Aggregation von Finanzdaten und die Indexkonstruktion zu verbessern.
