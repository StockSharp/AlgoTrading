# Diagramm zur grundlegenden Verwendung von Datenquelle und Chart-Baustein
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm bietet eine einfache Demonstration, wie die „Candles"-Datenquelle und der „Chart"-Baustein innerhalb der Designer-Plattform verwendet werden. Es ist darauf ausgelegt, Benutzern die Grundlagen des Abrufens von Marktdaten und deren Visualisierung in einem Diagrammformat zu vermitteln.

![schema](schema.png)

## Überblick

Das Diagramm zeigt die grundlegende Konfiguration, die benötigt wird, um Kerzendaten für ein bestimmtes Finanzinstrument abzurufen und in einem Diagramm darzustellen. Dies dient als grundlegendes Beispiel für Nutzer, die neu im Umgang mit Designer sind oder mit einfachen Datenvisualisierungstechniken beginnen möchten.

## Komponenten des Diagramms

- **Candles-Datenquelle**: Dies ist der primäre Knoten, der [Kerzendaten](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) vom ausgewählten Finanzinstrument abruft. Benutzer können das Instrument, den Datenbereich und den Kerzen-Zeitrahmen angeben (z. B. 1-Minuten-, 5-Minuten-Kerzen).
- **Chart-Baustein**: Dieser Knoten wird verwendet, um die abgerufenen Daten in einer grafischen Oberfläche zu [zeichnen](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html). Er kann verschiedene Kerzenattribute wie Eröffnungs-, Hoch-, Tief- und Schlusskurse anzeigen.

## Funktionalität

- **Datenabruf**: Das Diagramm beginnt mit dem Abruf von Kerzendaten unter Verwendung der im Candles-Datenquellen-Baustein angegebenen Parameter.
- **Datenvisualisierung**: Die abgerufenen Daten werden dann an den Chart-Baustein weitergegeben, der die Kerzen in der Designer-Umgebung in einem Diagramm darstellt.

## Anwendungsfall

Dieses Diagramm ist besonders nützlich für:
- Neue Benutzer, die lernen, wie man Datenabruf und Visualisierung in Designer einrichtet.
- Trader und Analysten, die Marktdaten schnell zur Analyse visualisieren möchten.
- Bildungszwecke, um die grundlegende Interaktion zwischen Datenquellen-Knoten und Visualisierungswerkzeugen innerhalb der Plattform zu demonstrieren.

## Praktische Anwendung

Durch das Verstehen und Nutzen dieser grundlegenden Konfiguration können Benutzer:
- Schnell visuelle Darstellungen von Marktdaten für Echtzeit- oder historische Analysen einrichten.
- Das grundlegende Diagramm durch zusätzliche analytische Werkzeuge oder Indikatoren in Designer erweitern.
- Das Diagramm als Baustein für komplexere Handelsstrategien oder Datenstudien verwenden.

Dieses Diagramm ist Teil eines umfangreicheren Satzes von Bildungsressourcen, die in der Designer-Plattform verfügbar sind und darauf abzielen, die Kompetenz der Benutzer im Bereich Datenverarbeitung und Visualisierung zu stärken.
