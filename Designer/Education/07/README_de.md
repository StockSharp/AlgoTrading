# Strategiebeispiel mit mathematischen Formeln und Ausdrücken
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Datei enthält ein detailliertes Beispiel einer Handelsstrategie, die mit der Designer-Plattform von StockSharp erstellt wurde. Die Strategie integriert mathematische Ausdrücke und Formeln, um Trades basierend auf spezifischen Bedingungen auszuführen, die durch technische Indikatoren erfüllt werden.

## Strategie-Übersicht

Dieses Schema demonstriert die Anwendung von zwei populären technischen Indikatoren für Handelsentscheidungen:

### Bollinger Bands Strategie
- **Kaufbedingung**: Eine Kauforder wird ausgelöst, wenn die Preiskerze die obere Kurve des Bollinger Bands Indikators nach oben durchbricht.
- **Verkaufsbedingung**: Eine Verkaufsorder wird ausgeführt, wenn die Preiskerze die untere Kurve des Bollinger Bands Indikators nach unten durchbricht.

### MACD Indikator Strategie
- **Kaufbedingung**: Initiiert eine Kauforder, wenn die MACD-Kurve ihr Vorzeichen von negativ zu positiv ändert.
- **Verkaufsbedingung**: Löst eine Verkaufsorder aus, wenn die MACD-Kurve ihr Vorzeichen von positiv zu negativ ändert.

## Zusätzliche Funktionen

- **Visueller Vergleich**: Das Schema ermöglicht einen visuellen Nebeneinandervergleich der Ergebnisse beider Strategien.
- **Ergebnisexport**: Es enthält Funktionalität zum Exportieren der Testergebnisse in eine Datei zur weiteren Analyse.

Dieses Schema bietet einen praktischen Rahmen zum Verstehen und Anwenden mathematischer Werkzeuge in Handelsstrategien unter Nutzung der Möglichkeiten der Designer-Plattform.
