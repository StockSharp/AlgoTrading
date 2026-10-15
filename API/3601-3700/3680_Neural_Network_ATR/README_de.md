# Strategie für neuronale Netze ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Strategie repliziert den „Neurotest“-Expertenberater durch die Kombination eines leichten Neuronalen
Netzwerkschicht mit ATR-basierter Geldverwaltung innerhalb von StockSharp. Das Modell verbraucht die
letzte abgeschlossene M15-Kerze und wandelt sie in fünf normalisierte Merkmale um: nah an
Schlussmomentum, Intraday-Range, Kerzenkörper, Volumenexpansion und Volatilität (ATR bis
Preisverhältnis). Eine einzelne verborgene Schicht mit einer Sigmoid-Ausgabe erzeugt einen Wahrscheinlichkeitswert
die durch eine dynamische Lernrate skaliert wird. Die Punktzahl wird mit benutzerdefinierten Werten verglichen
Kauf- und Verkaufsschwellenwerte zum Öffnen oder Umdrehen von Positionen.

## Handelsregeln

1. Abonnieren Sie 15-Minuten-Kerzen (konfigurierbar) und berechnen Sie ATR des gleichen Zeitraums.
2. Erstellen Sie die fünf normalisierten Features aus der vorherigen Kerze und der aktuellen Kerze
Kerze, dann bewerten Sie das neuronale Netzwerk.
3. Wenn die angepasste Vorhersage über dem Kaufschwellenwert liegt und die aktuelle Position über dem Kaufschwellenwert liegt
Wenn Sie nicht lange sind, gehen Sie einen Long-Trade ein (schließen Sie bei Bedarf ein Short-Engagement).
4. Wenn die angepasste Prognose unter der Verkaufsschwelle liegt und die aktuelle Position darunter liegt
nicht short, gehen Sie einen Short-Trade ein.
5. Jedem Eintrag sind ATR-basierte Stop-Loss- und Take-Profit-Orders beigefügt. Wenn ATR nicht gebildet wird,
Es wird eine Rückfalldistanz in Punkten verwendet.
6. Wenn der aktuelle Spread das konfigurierte Limit überschreitet, wird die Kerze ignoriert.

## Risikomanagement

- Die Positionsgröße wird aus dem Portfolioeigenkapital und der Stop-Distanz ATR berechnet, sodass die
Der Verlust am Stop entspricht `Max Risk %` des Eigenkapitals.
- Schutzaufträge verwenden einen konfigurierbaren Risiko-Ertrags-Multiplikator.
- Der Handel stoppt automatisch, wenn der tägliche oder gesamte Drawdown die Limits überschreitet.
- Ein Strafsystem verringert die Lernrate bei täglicher Prüfung um 10 % (bis auf ein Minimum).
Das Gewinnziel wird nicht erreicht, was zukünftige Signale bis zum nächsten Handelstag dämpft.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| **Maximales Risiko %** | Maximales Risiko pro Trade als Prozentsatz des Eigenkapitals. |
| **Täglicher Verlust %** | Täglicher Drawdown-Schwellenwert, der den Handel stoppt. |
| **Gesamtverlust %** | Globaler Drawdown-Schwellenwert, der den Handel stoppt. |
| **Tagesgewinn %** | Tägliches Gewinnziel, bevor die Strafe übersprungen wird. |
| **Lernrate** | Auf die neuronale Ausgabe angewendeter Skalierungsfaktor. |
| **Versteckte Ebene** | Anzahl der Neuronen in der verborgenen Schicht. |
| **Kaufschwelle / Verkaufsschwelle** | Triggerniveaus für Long- und Short-Einstiege. |
| **Kerzentyp** | Kerzentyp und Zeitrahmen für Signale. |
| **ATR Zeitraum** | Zeitraum des ATR-Indikators. |
| **Maximaler Spread** | Maximal zulässige Spanne in Preisschritten. |
| **Risiko-Belohnung** | Take-Profit-Multiplikator relativ zur Stop-Distanz. |
| **Fallback-Stopp** | Stoppdistanz in Punkten, wenn ATR nicht verfügbar ist. |

## Notizen

- Zur Überwachung der Geld-/Briefspanne vor jeder Entscheidung ist ein Level1-Abonnement erforderlich.
- Die Gewichte des neuronalen Netzwerks werden zufällig initialisiert, sind aber deterministisch (Seed 42). Die
Die Lernratenmodulation emuliert das adaptive Verhalten des ursprünglichen MQL-Experten.
- Stellen Sie sicher, dass das verbundene Portfolio `CurrentValue`, `StepPrice` und Volumenlimits bietet
damit die Positionsgrößenbestimmung und die Schutzanordnungen korrekt funktionieren.
