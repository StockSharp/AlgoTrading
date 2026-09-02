# Diagramm der Strategie Larry Connors 3 Day High/Low
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Larry Connors' 3 Day High/Low kauft einen kurzen Rücksetzer in einem steigenden Markt. Der Kurs muss über einer langsamen SimpleMovingAverage bleiben, unter eine schnelle rutschen und drei Kerzen in Folge zeigen, deren Hoch und Tief jeweils unter denen der Vorkerze liegen. Der Trade wird beim ersten Schluss über der schnellen Linie abgegeben. Das Original zählt Tagesbalken; dieses Diagramm arbeitet mit Fünf-Minuten-Kerzen, damit es zur mitgelieferten Intraday-Historie passt.

![schema](schema.svg)

## Strategieübersicht

- Ein Kerzenmuster-Baustein trägt die ganze Vier-Kerzen-Figur: drei aufeinanderfolgende Kerzen mit jeweils tieferem Hoch und tieferem Tief als die vorherige.
- Eine SimpleMovingAverage über 50 Perioden stellt fest, dass der Markt steigt, sodass der Rücksetzer nur in Richtung der größeren Bewegung gekauft wird.
- Eine SimpleMovingAverage über 5 Perioden ist zugleich Eintrittstor – ein Kurs darunter heißt, der Rücksetzer läuft noch – und Ausstiegsauslöser.
- Die Strategie handelt nur long. Das Original begrenzt zusätzlich die Zahl der Einstiege und wartet fünfzehn Balken zwischen den Trades; für beide Zähler gibt es keinen Baustein, deshalb handelt dieses Diagramm häufiger als die Vorlage.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Musterbaustein meldet drei tiefere Hochs und Tiefs, der Schlusskurs liegt über der langsamen SMA, unter der schnellen SMA und die Position ist neutral. Die Order kauft das gemeinsame Volumen zu Markt und eröffnet den Long.
- **Short-Einstieg**: Es gibt keine Short-Seite. Connors' Regelwerk kauft ausschließlich Rücksetzer in einem steigenden Markt, daher besitzt das Diagramm keinen Verkaufseinstieg.
- **Ausstieg**: Der erste Schluss über der schnellen SMA schließt den Long. Der Schließen-Baustein sendet eine Marktorder über die offene Größe; Stop-Loss und Take-Profit fehlen, genau wie im Originalcode.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Slow SMA Length | 50 | Periode der langsamen SimpleMovingAverage, der Filter für den steigenden Markt. |
| Fast SMA Length | 5 | Periode der schnellen SimpleMovingAverage: ein Kurs darunter eröffnet den Trade, der erste Schluss darüber beendet ihn. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den Musterindikator, beide gleitenden Durchschnitte und einen Konverter, der den Schlusskurs liest.
- Zwei Vergleichsbausteine stellen den Schlusskurs den beiden Durchschnitten gegenüber, der Positionsbaustein wird mit einer Nullkonstante verglichen.
- Ein logisches UND verbindet das Mustersignal, beide Durchschnittsbedingungen und die Prüfung auf neutrale Position und löst einen Positionsbaustein im Eröffnungsmodus aus.
- Ein zweiter Positionsbaustein im Schließmodus wird ausgelöst, wenn der Schlusskurs wieder über den schnellen Durchschnitt steigt; er braucht kein Volumen, da er die offene Position glattstellt.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
