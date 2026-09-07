# Strategiediagramm einer Orderleiter an runden Kursmarken
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Diagramm verwaltet den vollständigen Lebenszyklus passiver Einstiegsorders an runden Kursmarken. Abgeschlossene Fünf-Minuten-Kerzen steuern einen adaptiven Kaufman-Durchschnitt, Richtungskreuzungen, berechnete Kauf- und Verkaufsmarken, Orderersetzung, zeitgesteuerte Stornierung und einen absoluten nachlaufenden Stopp.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Fünf-Minuten-Kerzen speisen KAMA(15) mit schneller Periode 2 und langsamer Periode 30; nur gebildete Indikatorwerte gelangen in den Signalpfad.
- Ein Previous value-Block verschiebt die gebildete KAMA um eine Aktualisierung, und Crossing vergleicht jeden Schlusskurs mit diesem vorherigen Wert des adaptiven Durchschnitts.
- Der nächstgelegene gerundete Mittelpunkt wird mit floor(close / step + 0.5) berechnet. Das Kauflimit liegt einen Schritt von 200 Kurseinheiten darunter, das Verkaufslimit einen Schritt darüber.
- Getrennte Kauf- und Verkaufsbusse halten die jeweils aktive Order. Ändert sich ihre berechnete Marke, verschiebt Order replacing das aktive Limit auf den neuen Kurs.
- Jede Seite besitzt einen eigenen Zyklusmerker und einen Timer über zwölf Kerzen. Eine Gegenkreuzung oder der Timer storniert ein noch aktives Limit; eine Ausführung aktiviert den nachlaufenden Schutz.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Wenn der Schlusskurs die vorherige gebildete KAMA nach oben kreuzt, die erfasste Position null ist und der Kaufzyklusmerker frei ist, ein Kauflimit über 0.1 Einheiten bei (floor(close / step + 0.5) - 1) × step registrieren.
- **Short-Einstieg**: Wenn der Schlusskurs die vorherige gebildete KAMA nach unten kreuzt, die erfasste Position null ist und der Verkaufszyklusmerker frei ist, ein Verkaufslimit über 0.1 Einheiten bei (floor(close / step + 0.5) + 1) × step registrieren.
- **Ausstieg**: Eine ausgeführte Einstiegsorder aktiviert einen absoluten nachlaufenden Stopp mit Abstand 10. Er wird anhand abgeschlossener Kerzenschlusskurse geprüft und mit einer Marktorder ausgeführt. Der Take-Profit ist deaktiviert. Offene Einstiegsorders werden bei einer Gegenkreuzung oder nach zwölf abgeschlossenen Kerzen storniert.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles Series | 00:05:00 | Zeitrahmen der abgeschlossenen Kerzen für Signale, Preise, Timer, Schutzaktualisierungen und Chart. |
| KAMA Fast SC Period | 2 | Schnelle Glättungsperiode des adaptiven Kaufman-Durchschnitts. |
| KAMA Slow SC Period | 30 | Langsame Glättungsperiode des adaptiven Kaufman-Durchschnitts. |
| KAMA Length | 15 | Berechnungslänge des adaptiven Kaufman-Durchschnitts. |
| KAMA Source | Not set | Es ist kein alternatives Indikator-Eingabefeld gewählt; die Kerzen sind direkt verbunden. |
| Round Level Step | 200 | Abstand zwischen benachbarten runden Kursmarken in Kurseinheiten. |
| Order Volume | 0.1 | Festes Volumen jeder registrierten und ersetzten Einstiegsorder. |
| Buy Order Life (N) | 12 | Zahl abgeschlossener Kerzen im Kauforderzyklus bis zur zeitgesteuerten Stornierung und Rücksetzung des Merkers. |
| Sell Order Life (N) | 12 | Zahl abgeschlossener Kerzen im Verkaufsorderzyklus bis zur zeitgesteuerten Stornierung und Rücksetzung des Merkers. |
| Take Profit | 0 | Ein absoluter Wert von null deaktiviert den Take-Profit. |
| Stop Loss | 10 | Absoluter Abstand des nachlaufenden Stopps vom besten beobachteten geschützten Kurs. |
| Trailing Stop Loss | true | Verschiebt die Stoppgrenze, wenn sich der Kurs zugunsten der Position bewegt. |
| Use Market Orders | true | Sendet den ausgelösten nachlaufenden Ausstieg als Marktorder. |

## Diagrammdetails

- Der Kerzenschluss erreicht Crossing, bevor der neu gebildete KAMA-Wert verschoben wird. Crossing erhält dadurch den aktuellen Schlusskurs und den adaptiven Durchschnittswert der vorherigen gebildeten Aktualisierung.
- Beide Markenformeln verwenden denselben Schlusskurs und Schritt. Previous value-Blöcke halten die vorherigen Kauf- und Verkaufsmarken; NotEqual-Vergleiche geben nur bei einer Markenänderung einen Ersetzungsimpuls aus.
- Jeder Combination-Block übernimmt die bei der Registrierung ausgegebene Order und jede nach einer Ersetzung ausgegebene Order. Sein Ausgang liefert die neueste Order an Order replacing und Order cancellation.
- Die N values-Blöcke werden durch eine erfolgreiche Registrierung aktiviert und zählen zwölf abgeschlossene Kerzen. Ihre Ausgänge fordern die Stornierung an und geben den jeweiligen Zyklusmerker für eine spätere Einrichtung frei.
- Das Chart zeigt Fünf-Minuten-Kerzen, KAMA, beide runden Kursmarken, die aktuellen Kauf- und Verkaufslimits, nachlaufende Stopporders und alle Ausführungen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
