# Erweitertes Mehrfach-Zeitrahmen-Strategie-Diagramm
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Datei veranschaulicht ein komplexes Strategie-Diagramm, das Kerzen aus verschiedenen Zeitrahmen verwendet und speziell für die Designer-Plattform von StockSharp entwickelt wurde. Dieses Beispiel nutzt unterschiedliche Bedingungen in mehreren Zweigen, um Trades auf der Grundlage historischer Preisdaten auszuführen.

## Strategiedetails

Das Diagramm ist in zwei Hauptzweige unterteilt, wobei jeder Zweig Fünf-Minuten-Kerzen verwendet, die mit historischen Preisextremen verglichen werden, um Handelsentscheidungen zu treffen:

### Erster Zweig — Historische Extreme
- **Kaufbedingung**: Die Strategie initiiert eine Kauforder, wenn der Schlusskurs einer Fünf-Minuten-Kerze höher ist als der höchste Preis der letzten 20 Tage.
- **Verkaufsbedingung**: Eine Verkaufsorder wird ausgeführt, wenn der Schlusskurs einer Fünf-Minuten-Kerze niedriger ist als der niedrigste Preis der letzten 10 Tage.

### Zweiter Zweig — Umgekehrte Bedingungen
- **Verkaufsbedingung**: Führt eine Verkaufsorder aus, wenn der Schlusskurs einer Fünf-Minuten-Kerze niedriger ist als der niedrigste Preis der letzten 20 Tage.
- **Kaufbedingung**: Initiiert einen Kauf, wenn der Schlusskurs einer Fünf-Minuten-Kerze höher ist als der höchste Preis der letzten 10 Tage.

## Versionsspezifische Funktionen und Änderungen
- **Erscheinungsbild des Flag-Blocks**: In Designer Version 5 wurde das Erscheinungsbild des Flag-Blocks aktualisiert.
- **Strategieanpassungen**: Ebenfalls in Version 5 wurde die Strategie so angepasst, dass sie zwei Blöcke sowohl für Verkaufs- als auch für Kaufsignale enthält. Diese Anpassung ist auf eine Änderung in der Art und Weise zurückzuführen, wie Signale Aktionen in der neueren Version des Designers auslösen.

Dieses Diagramm bietet einen Rahmen für die Implementierung und das Testen von Strategien, die auf signifikante Preisbewegungen reagieren, indem kurzfristige Preisaktionen mit langfristigen Preisrekorden verglichen werden. Der Mehrfachzweig-Ansatz ermöglicht es Händlern, mit verschiedenen strategischen Reaktionen auf der Grundlage derselben zugrundeliegenden Daten zu experimentieren.
