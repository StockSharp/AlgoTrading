# Boring-EA2-Alarm
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Boring EA2 Alert bildet die Benachrichtigungslogik des MetaTrader-4-Expert-Advisors `boring-ea2` nach. Die Strategie hört auf abgeschlossene Kerzen, berechnet drei einfache gleitende Durchschnitte (SMA 3, SMA 20, SMA 150) und gibt informative Logs aus, wenn ein Crossover zwischen den Durchschnitten auftritt. Die Implementierung vermeidet bewusst Orderplatzierung: Ziel ist es, Tradern rechtzeitige Alarme zu geben, die sie mit diskretionärer Ausführung oder anderen automatisierten Strategien kombinieren können.

## Strategielogik
### Verfolgung gleitender Durchschnitte
* **Kurzfristiger Bias:** Eine 3-Perioden-SMA reagiert auf unmittelbare Preisänderungen.
* **Mittlerer Trend:** Eine 20-Perioden-SMA glättet den Preis über den kurzfristigen Swing-Horizont.
* **Langer Trend:** Eine 150-Perioden-SMA repräsentiert den dominanten Hintergrundtrend.

### Crossover-Erkennung
* **SMA3 vs SMA20:** meldet "crossed up", wenn SMA3 über SMA20 steigt, und "crossed down", wenn sie darunter fällt. Interne Flags stellen sicher, dass jeder Übergang einmal gemeldet wird.
* **SMA3 vs SMA150:** spiegelt dieselbe Logik gegenüber dem langfristigen Durchschnitt, um Momentum-Schübe oder Umkehrungen gegen den vorherrschenden Trend zu erkennen.
* **SMA20 vs SMA150:** fügt eine mittel-/langfristige Bestätigungsebene hinzu, sodass Verschiebungen in der höheren Zeitrahmenstruktur eigene Alarme auslösen.
* **Initialisierungsschutz:** Die erste abgeschlossene Kerze setzt nur den Anfangszustand. Alarme beginnen mit der zweiten abgeschlossenen Kerze, sobald eine echte Beziehungsänderung beobachtet wird.

### Benachrichtigungsformat
* Alarme spiegeln die ursprüngliche EA-Nachricht: `Alert!!! - SYMBOL - TF - description`.
* Der Zeitrahmencode wird aus dem konfigurierten Kerzentyp abgeleitet. Standard-Labels im MetaTrader-Stil (M1, M5, H1 usw.) werden verwendet, wenn verfügbar; andere Zeitrahmen fallen auf kompakte Notation zurück (z. B. `M45` oder `D2`).
* Nachrichten werden mit `AddInfoLog` geschrieben und können an Log-Viewer, Skripte oder GUI-Dashboards weitergeleitet werden.

## Parameter
* **Short SMA Length:** Anzahl Perioden für den schnellen gleitenden Durchschnitt (Standard `3`).
* **Medium SMA Length:** Anzahl Perioden für den mittleren gleitenden Durchschnitt (Standard `20`).
* **Long SMA Length:** Anzahl Perioden für den langsamen gleitenden Durchschnitt (Standard `150`).
* **Candle Type:** Zeitrahmen zur Berechnung der gleitenden Durchschnitte. Standard sind 1-Minuten-Kerzen, passend zu den tickbasierten Prüfungen des EA mit hoher Reaktivität.

## Zusätzliche Hinweise
* Die Strategie sendet, ändert oder storniert keine Orders. Sie ist rein informativ.
* Da `Bind` finalisierte Werte liefert, wird jedes Crossover auf abgeschlossenen Kerzen bewertet. Dies vermeidet die lauten Intrabar-Wechsel, die der ursprüngliche EA durch Tick-Zählung abmilderte.
* Logging-basierte Benachrichtigungen können mit eigenen Handlern integriert werden, indem innerhalb einer Host-Anwendung Strategie-Logereignisse abonniert werden.
* Derzeit wird keine Python-Übersetzung bereitgestellt; nur die C#-Version ist im API-Paket enthalten.
