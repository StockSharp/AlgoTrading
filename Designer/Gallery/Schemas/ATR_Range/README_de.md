# Diagramm der ATR-Range-Ausbruchstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Hier entscheidet eine einzige Zahl: wie weit der Schlusskurs über die letzten Kerzen gelaufen ist, gemessen an der Average True Range. Eine Bewegung von mindestens einer ATR gilt als Ausbruch, dem man sich anschließt, und die Seite ist schlicht die Richtung, in die der Kurs gelaufen ist. Der einfache gleitende Durchschnitt spielt beim Einstieg gar keine Rolle - er ist der Ausstieg, und die Position wird aufgegeben, sobald der Schlusskurs wieder durch ihn hindurchfällt.

![schema](schema.svg)

## Strategieübersicht

- Ein Vorwert-Baustein hält den Schlusskurs von vier Kerzen zuvor, und ein Formelbaustein zieht ihn vom aktuellen Schlusskurs ab und nimmt den Betrag: das ist die zurückgelegte Strecke.
- Die Average True Range ist der Maßstab. Erreicht die Strecke sie, ist der Markt in diesen vier Kerzen weiter gelaufen als sonst in einer, und das Diagramm nennt das einen Ausbruch.
- Für die Richtung braucht es keinen Indikator: Schlusskurs über dem früheren Schlusskurs bedeutet long, darunter short.
- Der gleitende Durchschnitt hat nur eine Aufgabe, das Schließen der Position: Ein Long endet beim ersten Schlusskurs darunter, ein Short beim ersten darüber.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die über die letzten vier Kerzen zurückgelegte Strecke beträgt mindestens eine ATR, der Schlusskurs liegt über dem Schlusskurs von vier Kerzen zuvor und die Position ist neutral. Die Order kauft das gemeinsame Volumen zum Markt.
- **Short-Einstieg**: Die über die letzten vier Kerzen zurückgelegte Strecke beträgt mindestens eine ATR, der Schlusskurs liegt unter dem Schlusskurs von vier Kerzen zuvor und die Position ist neutral. Die Order verkauft das gemeinsame Volumen zum Markt.
- **Ausstieg**: Ein Long wird bei der ersten Kerze geschlossen, die unter dem einfachen gleitenden Durchschnitt schließt, ein Short bei der ersten, die darüber schließt. Beide Ausstiegsbausteine tragen die Schließbedingung, sodass jeder nur auf seiner Seite handeln kann. Einen Stop-Loss oder Take-Profit gibt es nicht, wie im Original.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| ATR Period | 14 | Glättungsperiode der Average True Range, die die Mindestbreite eines Ausbruchs festlegt. |
| MA Period | 20 | Periode des einfachen gleitenden Durchschnitts, der die Position schließt. |
| Lookback shift | 4 | Wie viele Kerzen zurück der Kurs verglichen wird; das Original misst über das Rückschaufenster minus eins, also standardmäßig vier Kerzen. |
| Volume | 1 | Ordervolumen in Lots, gemeinsam für beide Einstiegsbausteine. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist die ATR, den gleitenden Durchschnitt und einen Konverter für den Schlusskurs; der Vorwert-Baustein hängt an eben diesem Konverter.
- Der Formelbaustein berechnet den Betrag der Differenz beider Schlusskurse, und ein Vergleich stellt ihn der ATR gegenüber, um zu entscheiden, ob die Bewegung breit genug war.
- Zwei weitere Vergleiche desselben Kurspaares liefern die Richtung, und ein Vergleich der Position mit einer Nullkonstante verhindert, dass sich Einstiege stapeln.
- Jedes logische UND verbindet Breite, Richtung und neutrale Position und löst einen Eröffnungsbaustein aus; die beiden Vergleiche mit dem gleitenden Durchschnitt lösen die Schließbausteine direkt aus, denn deren Richtung bestimmt bereits, welche Seite sie schließen dürfen.
- Das C#-Original misst nur jede fünfte Kerze über nicht überlappende Fenster und friert den Referenzkurs auf der Kerze dazwischen ein. Für diesen Modulo-Zähler gibt es keinen Baustein, daher verwendet das Diagramm ein gleitendes Fenster und prüft auf jeder Kerze, was mehr Signale ergibt als im Original.
- Die Pause von fünfhundert Kerzen nach jedem Trade entfällt aus demselben Grund, und das Diagramm läuft auf den Fünf-Minuten-Kerzen der mitgelieferten Historie statt auf den Minutenkerzen des C#-Codes.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
