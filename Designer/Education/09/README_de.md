# Arbeitszeit- und Zeitbasiertes Strategie-Diagramm
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Schemadatei demonstriert die Anwendung des "Arbeitszeit"-Blocks zusammen mit anderen relevanten Blöcken in der Designer-Plattform zur Implementierung zeitbasierter Handelsstrategien.

## Überblick

Das Schema erkundet verschiedene Konfigurationen mit dem "Arbeitszeit"-Block, der es Händlern ermöglicht, Strategien auf der Grundlage spezifischer Zeitbedingungen auszuführen.

## Schlüsselkomponenten

- **Arbeitszeit-Block**: Wird verwendet, um die aktiven Handelszeiten oder spezifische Zeiten für die Ausführung von Trades zu definieren.
- **Variablen-Block**: Benannt "Strategie", wird dieser Block verwendet, um strategiespezifische Variablen zu speichern und zu verarbeiten.
- **Konverter-Block**: Wird zum Konvertieren und Abrufen zeitbezogener Daten verwendet, um zeitbasierte Entscheidungen zu unterstützen.

## Strategiedetails

### Strategie mit Arbeitszeitbedingung
- **Kauf vor Schließung**: Die Strategie initiiert eine Kauforder eine Minute vor dem Ende der definierten Arbeitszeit, um potenzielle Preisbewegungen am Ende der Handelssitzung zu nutzen.

### Spezifischer Zeitauslöser
- **Festzeitkauf**: Implementiert einen Kauf genau um 18:00 Uhr, wobei die Trade-Ausführung mit bedeutenden Marktereignissen oder typischen Schließzeiten abgestimmt wird.

### Erweiterter Zeitbasierter Abschluss aus Lektion 7
- **Positionsschließung**: Schließt alle offenen Positionen fünf Minuten vor Ende der Arbeitszeit — eine Strategie, die entwickelt wurde, um das Halten von Übernachtpositionen oder das Reagieren auf Preisschwankungen am Tagesende zu vermeiden.

## Hinweis zu den Änderungen in Version 5

In der fünften Version der Designer-Software wurden die Zeitberechnungen und die gemeinsame Funktionsweise des "Arbeitszeit"-Blocks verbessert. Nach dem Import von Strategien, die diese Funktionen nutzen, wird empfohlen, diese innerhalb der Plattform neu zu erstellen, um korrekte Funktionalität sicherzustellen und von den aktualisierten Zeitberechnungsformeln zu profitieren.

Dieses Schema bietet einen umfassenden Rahmen für die Entwicklung und das Testen von Strategien, die stark auf präzises Timing bei der Trade-Ausführung angewiesen sind, und ist damit ein unverzichtbares Werkzeug für Händler, die sich auf Intraday-Handelsstrategien konzentrieren oder bestimmte Marktzeiten einhalten müssen.
