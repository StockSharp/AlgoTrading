# Diagramm der Strategie mit Gleitenden Durchschnitten
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Datei enthält eine diagrammatische Darstellung einer Handelsstrategie auf Basis gleitender Durchschnitte, die mithilfe der Strategiegalerie der Designer-Plattform entwickelt wurde. Die Strategie nutzt das Konzept der gleitenden Durchschnitte, um Kauf- und Verkaufssignale auf Basis ihrer Kreuzungen zu generieren — eine in Finanzmärkten weit verbreitete Methode zur Beurteilung von Momentum und Trendbestätigung.

![schema](schema.png)

## Strategieübersicht

Die Strategie umfasst zwei gleitende Durchschnitte:

- **Kurzfristiger Gleitender Durchschnitt**: Ein schnellerer [gleitender Durchschnitt](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html), der schneller auf Kursänderungen reagiert.
- **Langfristiger Gleitender Durchschnitt**: Ein langsamerer [gleitender Durchschnitt](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html), der ein geglättetes Bild der Kurstrends liefert.

## Ein- und Ausstiegsregeln

- **Kaufsignal**: Die Strategie generiert ein [Kauf](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)-Signal, wenn der kurzfristige gleitende Durchschnitt den langfristigen [von unten nach oben kreuzt](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/crossing.html), was auf einen Aufwärtstrend hindeutet.
- **Verkaufssignal**: Umgekehrt wird ein [Verkauf](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)-Signal ausgegeben, wenn der kurzfristige gleitende Durchschnitt den langfristigen [von oben nach unten kreuzt](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/crossing.html), was auf einen möglichen Abwärtstrend hindeutet.

## Diagrammdetails

Das Diagramm stellt den logischen Ablauf der Strategie visuell dar:

- **Berechnung gleitender Durchschnitte**: Knoten berechnen die gleitenden Durchschnitte auf Basis benutzerdefinierter Parameter wie Periode und Typ des gleitenden Durchschnitts (z. B. einfach, exponenziell).
- **Vergleichsknoten**: Bewerten die Kreuzungsbedingungen, um zu entscheiden, ob Positionen ein- oder ausgestiegen werden soll.
- **Handelsaktionen**: Knoten, die Kauf- oder Verkaufsorders auf Basis der Auswertungsergebnisse der Vergleichsknoten ausführen.

## Verwendung

Trader können dieses Diagramm in die Designer-Plattform importieren, um:
- die Strategie anhand historischer Daten zu testen und ihre Wirksamkeit zu beurteilen;
- die Parameter der gleitenden Durchschnitte oder die Logik zu modifizieren, um sie besser auf spezifische Handelsbedürfnisse oder Marktbedingungen abzustimmen;
- die Strategie nach ausreichenden Tests in einer Live-Handelsumgebung einzusetzen.

## Bildungswert

Dieses Strategie-Diagramm dient als Lernwerkzeug für Einsteiger, um die Grundlagen der technischen Analyse und Strategieentwicklung zu verstehen. Es bietet auch eine Grundlage für die Entwicklung komplexerer Strategien für fortgeschrittene Benutzer.

Diese Datei ist Teil einer umfassenden Sammlung von Handelsstrategien der Designer-Plattform, die darauf abzielt, die Handelsfähigkeiten und Strategieentwicklungskompetenzen der Benutzer zu verbessern.
