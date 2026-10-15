# Diagramm der KDJ-Expert-Advisor-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein KDJ-Diagramm, in dem die J-Linie als Differenz der Linien %K und %D des Stochastik-Oszillators entsteht. Diese Differenz bestimmt die Richtung: gekauft wird, wenn sie positiv wird oder wenn %K bei bereits positiver Differenz weiter steigt, verkauft spiegelbildlich. Stundenkerzen liefern in einem Monat genügend Balken, und Prozentabstände für Stopp und Ziel funktionieren auf jedem Instrument.

![schema](schema.svg)

## Strategieübersicht

- Der Stochastik-Oszillator mit einem 30-Balken-%K und einem 6-Balken-%D vertritt KDJ, die Differenz K - D übernimmt die Rolle der J-Linie.
- Es gibt zwei Wege in eine Position: die Differenz kreuzt die Nulllinie, oder die %K-Linie bewegt sich in die Richtung, die das Vorzeichen der Differenz bereits vorgibt.
- Eröffnet wird nur aus der Neutralstellung, die Strategie stockt also nie auf und dreht nie um; beendet wird der Trade vom Schutzbaustein.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: K - D ist positiv, und entweder war die Differenz auf der Vorkerze negativ, diese Kerze bringt also den Nulldurchgang, oder %K liegt höher als auf der Vorkerze. Die Position muss neutral sein; ein Lot wird zum Marktpreis gekauft.
- **Short-Einstieg**: K - D ist negativ, und entweder war die Differenz auf der Vorkerze positiv, diese Kerze bringt also den Nulldurchgang, oder %K liegt tiefer als auf der Vorkerze. Die Position muss neutral sein; ein Lot wird zum Marktpreis verkauft.
- **Ausstieg**: Es gibt kein Ausstiegssignal: Der Schutzbaustein schließt den Trade mit Marktorders bei 2% Gewinnziel oder 1% Verlustbegrenzung.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| %K Length (KDJ period) | 30 | Länge der %K-Linie, die als KDJ-Periode dient. |
| %D Smoothing | 6 | Glättungslänge der %D-Linie. |
| Take profit, % | 2 | Abstand des Gewinnziels in Prozent des Einstiegskurses. |
| Stop loss, % | 1 | Abstand der Verlustbegrenzung in Prozent des Einstiegskurses. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 01:00:00 | Stunden-Zeiteinheit der Kerzen im gesamten Diagramm. |

## Diagrammdetails

- Zwei Konverterbausteine zerlegen den Stochastik-Oszillator in die Linien %K und %D, ein Formelbaustein zieht die eine von der anderen ab.
- Bausteine für den vorherigen Wert halten K - D und %K eine Kerze zurück, so werden Nulldurchgang und Steigung ohne einen Kreuzungsbaustein erkannt.
- Vier logische UND-Bausteine bilden die je zwei Einstiegswege einer Richtung und tragen das Flag der neutralen Position bereits mit; ein ODER führt das Paar zu einem Auslöser je Seite zusammen.
- Beide Einstiegsbausteine geben ihre eigenen Trades an den Schutzbaustein weiter, sodass jede Ausführung sofort Stopp und Ziel erhält.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
