# Diagramm der ADX-gefilterten Durchschnittsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Diagramm handelt die Kerze, die über einen langen einfachen gleitenden Durchschnitt tritt, aber nur solange der ADX bestätigt, dass der Markt wirklich trendet. Eine Kerze gilt als Kreuzung, wenn sie auf der einen Seite des Durchschnitts eröffnet und auf der anderen geschlossen hat; die Position wird dann auf die Seite des Schlusskurses gedreht. Das Original läuft auf Minutenkerzen, dieses Diagramm auf den Fünf-Minuten-Kerzen der mitgelieferten Historie.

![schema](schema.svg)

## Strategieübersicht

- Die SMA(200) ist die Bezugslinie, und ein Baustein für den Vorwert hält ihren Wert von einer Kerze zuvor, sodass der Eröffnungskurs am Durchschnitt seiner eigenen Kerze und der Schlusskurs am aktuellen gemessen wird.
- Das exklusive ODER dieser beiden Vergleiche ist genau auf den Kerzen wahr, die den Durchschnitt überspannen — so definiert der Originalcode die Kreuzung, nicht als Schnitt zweier Indikatorlinien.
- Der ADX mit Länge fünfzig prüft jeden Einstieg: Eine Kerze, die den Durchschnitt in einem ruhigen Markt kreuzt, wird ignoriert.
- Es gibt weder Stop noch Ziel — die Position wird nur von der Gegenkreuzung gedreht, und das Ordervolumen ist das gemeinsame Volumen plus die bereits gehaltene Position.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der ADX liegt über der Schwelle, die Kerze hat den Durchschnitt gekreuzt, der Schlusskurs liegt über der aktuellen SMA und die Position ist nicht long. Die Order kauft das gemeinsame Volumen plus die Größe eines offenen Shorts, sodass eine Order den Short schließt und den Long eröffnet.
- **Short-Einstieg**: Der ADX liegt über der Schwelle, die Kerze hat den Durchschnitt gekreuzt, der Schlusskurs liegt auf oder unter der aktuellen SMA und die Position ist nicht short. Die Order verkauft das gemeinsame Volumen plus die Größe eines offenen Longs.
- **Ausstieg**: Einen eigenen Ausstieg gibt es nicht: Die Position wird gehalten, bis die Gegenkreuzung sie dreht — genau wie im Originalcode, der weder Stop-Loss noch Take-Profit umsetzt.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| ADX Length | 50 | Glättungsperiode des Average Directional Index. |
| ADX Threshold | 25 | ADX-Wert, den der Markt überschreiten muss, damit ein Einstieg erlaubt ist. |
| SMA Length | 200 | Periode des einfachen gleitenden Durchschnitts, an dem die Kerzen gemessen werden. |
| Volume | 1 | Ordervolumen in Lots, bevor die offene Position addiert wird. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Zwei Konverter lesen Eröffnung und Schluss jeder abgeschlossenen Kerze, während gleitender Durchschnitt und ADX auf der Kerze selbst berechnet werden.
- Ein Baustein für den Vorwert verzögert die SMA um eine Kerze; die beiden Vergleiche mit altem und aktuellem Wert verbindet ein exklusives ODER — das ist der Kreuzungstest.
- Ein logisches NICHT macht aus der Bedingung 'Schluss über dem Durchschnitt' die Short-Bedingung, sodass ein Vergleich beide Richtungen bedient.
- Ein Formelbaustein addiert den Betrag der Position zum gemeinsamen Volumen, damit eine Marktorder die alte Seite schließt und die neue in einem Schritt eröffnet.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
