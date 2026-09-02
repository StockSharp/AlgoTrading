# Diagramm der Volatilitätsausbruchstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein von Hand gebauter Kanal: Der einfache gleitende Durchschnitt liefert die Mitte, die Average True Range die Breite, und ein Schlusskurs außerhalb von SMA plus oder minus einem Vielfachen der ATR gilt als Bewegung, der man sich anschließen sollte. Weil der Kanal mit der Volatilität atmet, bleibt derselbe Multiplikator in ruhigen wie in schnellen Märkten sinnvoll.

![schema](schema.svg)

## Strategieübersicht

- SMA und ATR laufen mit derselben Periode über abgeschlossene Kerzen, sodass der Kanal um den Durchschnittspreis zentriert und an der jüngsten True Range skaliert ist.
- Zwei Formelbausteine setzen die Ränder zusammen: der obere ist SMA plus Multiplikator mal ATR, der untere SMA minus derselben Größe.
- Die Strategie ist stets im Markt: Der Gegenausbruch dreht die Position, und ein Schutzstop schließt sie früher, wenn die Bewegung scheitert.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die Kerze schließt über SMA plus Multiplikator mal ATR und die Position ist nicht long. Die Order kauft das Basisvolumen zuzüglich des Positionsbetrags: Ein Short dreht auf Long, aus der Neutralstellung entsteht ein Long.
- **Short-Einstieg**: Die Kerze schließt unter SMA minus Multiplikator mal ATR und die Position ist nicht short. Die Order verkauft das Basisvolumen zuzüglich des Positionsbetrags: Ein Long dreht auf Short, aus der Neutralstellung entsteht ein Short.
- **Ausstieg**: Einen indikatorbasierten Ausstieg gibt es nicht. Die Position wird vom Gegenausbruch gedreht oder früher vom Schutzbaustein mit Stop-Loss geschlossen, der an den Trades beider Einstiege hängt.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Indicator period | 20 | Gemeinsame Periode für die SMA, die den Kanal zentriert, und für die ATR, die seine Breite bestimmt. |
| ATR multiplier | 2 | Um wie viele ATR der Ausbruchsrand vom gleitenden Durchschnitt entfernt liegt. |
| Stop loss, % | 2 | Schützender Stop-Loss in Prozent des Einstiegskurses. |
| Volume | 1 | Basisordervolumen in Lots; beim Drehen kommt der Betrag der Position hinzu. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist beide Indikatoren und über einen Konverter den Schlusskurs, der sowohl für die Vergleiche als auch als Preisquelle des Schutzbausteins dient.
- Eine Konstante hält den Multiplikator, und zwei Formelbausteine berechnen aus SMA, Multiplikator und ATR den oberen und den unteren Rand.
- Zwei Vergleichsbausteine prüfen den Schlusskurs gegen die Ränder, zwei weitere vergleichen die Position mit null, und jedes logische UND fügt je eine Bedingung zu einem Einstieg zusammen.
- Ein Formelbaustein berechnet das Drehvolumen als Basisvolumen plus Positionsbetrag und speist beide Bausteine zur Positionsänderung.
- Das Original sichert die Position mit einem Stop von zwei absoluten Kurseinheiten ab, der auf ein anderes Instrument abgestimmt ist und bei einem Kryptokurs sofort ausgelöst würde; das Diagramm verwendet stattdessen einen Zwei-Prozent-Stop, der sich auf jedem Instrument so verhält, wie es gemeint war.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
