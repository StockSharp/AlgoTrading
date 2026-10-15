# Last ZZ50-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Last ZZ50-Strategie reproduziert Vladimir Karputovs "Last ZZ50"-Expertenberater für MetaTrader.
Sie verwendet den ZigZag-Indikator, um die drei neuesten Wendepunkte zu verfolgen, und platziert ausstehende Orders am Mittelpunkt der letzten beiden ZigZag-Abschnitte.
Der Ansatz versucht, Ausbrüche vom letzten Swing mitzunehmen, und storniert oder repositioniert Orders, wenn sich die ZigZag-Struktur ändert.

## Handelslogik
- **Pivot-Erkennung** – Ein ZigZag-Indikator (Tiefe 12, Abweichung 5, Backstep 3 standardmäßig) liefert die neuesten Pivots, beschriftet als A (neueste), B und C.
- **BC-Abschnitt-Order** – Wenn Pivot C sich von B unterscheidet und der neue Pivot A die Abschnittsrichtung nicht ungültig macht, platziert die Strategie eine ausstehende Order bei `(B + C) / 2`.
  - Wenn der BC-Abschnitt steigt, ist die Order long, andernfalls short.
  - Limit versus Stop-Typ wird je nach aktuellem Kurs relativ zum Mittelpunkt ausgewählt.
- **AB-Abschnitt-Order** – Dieselbe Mittelpunktlogik wird auf den AB-Abschnitt angewendet, wiederum mit Limit- oder Stop-Orders je nach aktuellem Kurs.
- **Sessionfilter** – Der Handel ist auf einen konfigurierbaren Wochentag und ein Intraday-Fenster beschränkt (Standard Montag 09:01 bis Freitag 21:01). Außerhalb des Fensters storniert die Strategie ausstehende Orders und kann optional eine bestehende Position glätten.
- **Trailing-Ausstieg** – Sobald eine Position mehr als die Summe aus Trailing-Stop- und Trailing-Step-Schwellenwerten gewinnt, wird eine schützende Stop-Order hinter dem Kurs nachgezogen, um Gewinne zu sichern.

## Risikomanagement
- Das Volumen der ausstehenden Orders entspricht dem Multiplikatorparameter mal dem minimalen handelbaren Volumen des Instruments.
- Sowohl AB- als auch BC-Orders werden storniert und neu erstellt, wenn sich die ZigZag-Pivots ändern, um zu verhindern, dass veraltete Orders im Buch verbleiben.
- Trailing Stops werden erst aktiviert, wenn die Position komfortabel im Gewinn ist, und reduzieren vorzeitige Ausstiege in volatilen Bedingungen.

## Parameter
- `LotMultiplier` – Multiplikator, der beim Senden von Orders auf das minimale handelbare Volumen angewendet wird.
- `ZigZagDepth`, `ZigZagDeviation`, `ZigZagBackstep` – Konfigurationswerte für den ZigZag-Indikator.
- `TrailingStopPips`, `TrailingStepPips` – Abstand und Aktivierungsschwelle für den Trailing Stop, gemessen in Pips.
- `StartDay`, `EndDay`, `StartTime`, `EndTime` – Handelssessiongrenzen.
- `CloseOutsideSession` – Ob Positionen geglättet werden sollen, wenn der Zeitfilter inaktiv ist.
- `CandleType` – Kerzenserie für ZigZag-Berechnungen (Standard 1 Stunde).

## Indikatoren
- **ZigZag** – Liefert Pivot-Punkte, die die Order-Platzierung und Strukturvalidierung steuern.
