# Mathematische Cubes und Formeln Diagramm
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Schema-Datei demonstriert die Verwendung von mathematischen Cubes und Formeln aus dem Abschnitt "Mathematik" im Designer-Tool, wobei der Schwerpunkt darauf liegt, wie diese Elemente in Handelsstrategien eingesetzt werden können.

## Übersicht

Das Schema untersucht die Verwendung von Formeln für Handelsentscheidungen basierend auf dem Vergleich des Schlusskurses eines Wertpapiers mit seinen statistischen Parametern, die über den Simple Moving Average (SMA) und die Standardabweichung berechnet werden.

## Strategiedetails

- **Verkaufsbedingung**: Die Strategie erteilt eine Verkaufsorder, wenn der Schlusskurs der vorherigen Kerze größer ist als der SMA-Wert der letzten 20 Perioden zuzüglich des Dreifachen der Standardabweichung desselben Zeitraums.
- **Kaufbedingung**: Eine Kauforder wird ausgeführt, wenn der Schlusskurs der vorherigen Kerze kleiner ist als der SMA-Wert der letzten 20 Perioden abzüglich des Dreifachen der Standardabweichung.

## Änderungen in Version 5

- **Mathematik-Abschnitt**: In Version 5 der Designer-Software wurde der Abschnitt "Mathematik" entfernt. Alle Cubes, die zuvor in diesem Abschnitt zu finden waren, wurden in einem einzigen "Formel"-Cube zusammengeführt, wodurch der Design- und Implementierungsprozess vereinfacht wurde.
- **Positionseröffnungs-Cube**: Der "Position öffnen"-Cube wurde in Version 5 durch den "Order registrieren"-Cube ersetzt, was Änderungen bei der Verarbeitung von Orders innerhalb der Plattform widerspiegelt.

Dieses Schema zeigt effektiv, wie fortgeschrittene mathematische Berechnungen genutzt werden können, um dynamische und statistisch fundierte Handelsstrategien zu erstellen. Die Integration dieser Elemente in ein Handelsschema kann den Entscheidungsprozess durch quantitative Analyse erheblich verbessern.
