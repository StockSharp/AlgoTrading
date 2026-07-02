# Schema der Paarhandels-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Schema präsentiert eine Paarhandels-Strategie basierend auf dem relativen Wert zweier Wertpapiere. Es beinhaltet einen einzigartigen Ansatz zur Identifizierung und Nutzung von Preisabweichungen zwischen zwei korrelierten Vermögenswerten.

## Überblick

Paarhandel ist eine marktneutrale Strategie, bei der ein Vermögenswert gekauft und gleichzeitig ein anderer verkauft wird, wenn ihr Preisverhältnis von der historischen Norm abweicht. Dieses Schema verwendet das Beispiel zweier spezifischer Wertpapiere: SBER@TQBR und GAZP@TQBR.

## Strategielogik

- **Indexberechnung**: Die Strategie berechnet einen Index basierend auf der Formel `SBER@TQBR / GAZP@TQBR`. Dieser Index hilft, die relative Stärke oder Schwäche einer Aktie im Vergleich zur anderen zu bestimmen.
- **Kaufbedingung**: Wenn der Index steigt und signalisiert, dass SBER@TQBR relativ zu GAZP@TQBR teurer wird, kauft die Strategie den günstigeren Vermögenswert (GAZP@TQBR) und verkauft den teureren (SBER@TQBR).
- **Verkaufsbedingung**: Wenn der Index fällt und andeutet, dass SBER@TQBR relativ zu GAZP@TQBR günstiger wird, kauft die Strategie den teureren Vermögenswert (SBER@TQBR) und verkauft den günstigeren (GAZP@TQBR).

## Schlüsselmerkmale

- **Gerundete Werte**: Verwendet den `round`-Operator, um berechnete Indexwerte in ganze Zahlen umzuwandeln. Diese Vereinfachung unterstützt die Entscheidungsfindung durch klarere, handlungsorientierte Signale.
- **Marktneutralität**: Zielt darauf ab, von der Konvergenz des Preisverhältnisses zu seinem historischen Durchschnitt zu profitieren, unabhängig von der allgemeinen Marktrichtung.

## Anwendung und Vorteile

- **Risikominderung**: Durch den Handel mit historisch korrelierten Paaren minimiert die Strategie das Marktrisiko, da Gewinne auf einer Seite häufig Verluste auf der anderen Seite ausgleichen.
- **Nutzung von Preisinefiizienzen**: Die Strategie nutzt temporäre Ineffizienzen in den Preisen der gekoppelten Wertpapiere, von denen erwartet wird, dass sie schließlich zu ihrem Mittelwert zurückkehren.

## Ausführung

- **Einrichtungsbedingungen**: Stellen Sie vor der Implementierung der Strategie sicher, dass beide Wertpapiere sorgfältig auf signifikante Abweichungen überwacht werden, die Trades auslösen könnten.
- **Operative Dynamik**: Kontinuierliche Überwachung und Neukalibrierung der Schwellenwerte für Kauf und Verkauf auf der Grundlage historischer Daten und Marktbedingungen sind für den Erfolg der Strategie entscheidend.

Das vorgestellte Schema beschreibt nicht nur einen robusten Rahmen für den Paarhandel, sondern unterstreicht auch die Bedeutung mathematischer Werkzeuge wie der Rundung bei der Vereinfachung komplexer Handelsentscheidungen.
