# Diagramm der Stochastik-Strategie mit drei Zeitebenen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Diagramm kombiniert gebildetes Stochastic(5,3)-Momentum aus 60-, 15- und 5-Minuten-Kerzen. Einmal pro Stunde synchronisiert es die drei %K-%D-Differenzen und handelt eine Wende des Fünf-Minuten-Momentums im Einklang mit beiden höheren Zeitebenen.

![schema](schema.svg)

## Strategieübersicht

- Drei Ströme abgeschlossener Kerzen liefern 60-, 15- und 5-Minuten-Daten und können aus kleineren Zeitebenen aufgebaut werden.
- Jeder Strom berechnet Stochastic %K(5), glättet es mit SMA(3) zu %D und zieht %D von %K ab.
- Die stündliche Differenz erfasst die letzten Werte aller drei Ströme; Sync gibt bei jedem Stundenschluss eine vollständige Dreiergruppe frei.
- Die vorherige synchronisierte Fünf-Minuten-Differenz erkennt eine Wende an der Nulllinie, während die aktuelle Position eine Aufstockung über eine Einheit verhindert.
- Beide Aktionen sind Marktoperationen über eine Einheit, und Strategy trades sendet jede Ausführung an das Chart.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Wenn die vorherige Einstiegsdifferenz über null liegt, die aktuelle höchstens null ist, beide höheren Differenzen positiv sind und keine Long-Position besteht, eine Einheit zum Markt kaufen.
- **Short-Einstieg**: Wenn die vorherige Einstiegsdifferenz unter null liegt, die aktuelle mindestens null ist, beide höheren Differenzen negativ sind und keine Short-Position besteht, eine Einheit zum Markt verkaufen.
- **Ausstieg**: Es gibt keinen separaten Ausstiegszweig. Eine gültige Gegenaktion über eine Einheit stellt eine entgegengesetzte Einheitsposition glatt; ein späteres gültiges Signal kann die andere Richtung eröffnen. Das Diagramm besitzt weder Stop-Loss, Take-Profit noch Warteperiode.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Higher Candles Series | 01:00:00 | Abgeschlossene 60-Minuten-Kerzen für die höhere Berechnung und den stündlichen Entscheidungstakt. |
| Higher Stochastic %K Length | 5 | Länge des Stochastic %K der höheren Zeitebene. |
| Higher Stochastic %K Source | Not set | Kein alternatives Indikator-Eingabefeld ist gewählt; die höheren Kerzen sind direkt verbunden. |
| Higher Stochastic %D SMA Length | 3 | Glättungslänge für das höhere %K zur Ermittlung von %D. |
| Higher Stochastic %D SMA Source | Not set | Kein alternatives Indikator-Eingabefeld ist gewählt; das höhere %K ist direkt verbunden. |
| Middle Candles Series | 00:15:00 | Abgeschlossene 15-Minuten-Kerzen für die Berechnung der mittleren Zeitebene. |
| Middle Stochastic %K Length | 5 | Länge des Stochastic %K der mittleren Zeitebene. |
| Middle Stochastic %K Source | Not set | Kein alternatives Indikator-Eingabefeld ist gewählt; die mittleren Kerzen sind direkt verbunden. |
| Middle Stochastic %D SMA Length | 3 | Glättungslänge für das mittlere %K zur Ermittlung von %D. |
| Middle Stochastic %D SMA Source | Not set | Kein alternatives Indikator-Eingabefeld ist gewählt; das mittlere %K ist direkt verbunden. |
| Entry Candles Series | 00:05:00 | Abgeschlossene 5-Minuten-Kerzen für die Einstiegsberechnung und das Chart. |
| Entry Stochastic %K Length | 5 | Länge des Stochastic %K der Einstiegszeitebene. |
| Entry Stochastic %K Source | Not set | Kein alternatives Indikator-Eingabefeld ist gewählt; die Einstiegskerzen sind direkt verbunden. |
| Entry Stochastic %D SMA Length | 3 | Glättungslänge für das Einstiegs-%K zur Ermittlung von %D. |
| Entry Stochastic %D SMA Source | Not set | Kein alternatives Indikator-Eingabefeld ist gewählt; das Einstiegs-%K ist direkt verbunden. |
| Order Volume | 1 | Festes Marktvolumen für beide Aktionen. |

## Diagrammdetails

- Alle Indikatoren geben nur gebildete Werte aus. SMA(3) erhält das jeweilige %K, daher entspricht jede Differenz genau %K minus seinem Mittelwert über drei Werte.
- Die stündliche Differenz löst drei numerische Speicher aus, bevor deren Werte Sync erreichen; dadurch liefern der mittlere und der Einstiegsspeicher ihre zuletzt verfügbaren Messwerte.
- Sync leert jede vollständige Gruppe und gibt drei ausgerichtete Werte aus. Der Block Previous value speichert eine synchronisierte Einstiegsdifferenz für den nächsten Stundenvergleich.
- Die Position wird zusammen mit der synchronisierten Entscheidung erfasst. Ein Wert bis null erlaubt den Kauf, ein Wert ab null den Verkauf; so wird gleichgerichtetes Aufstocken verhindert.
- Das Chart erhält fünf Ströme: Fünf-Minuten-Kerzen, die drei laufenden %K-%D-Differenzen und alle Strategieausführungen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
