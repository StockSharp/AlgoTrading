# Diagramm der Momentum-Nulllinien-Impulsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das ganze Diagramm ruht auf einer Zahl: der Differenz zwischen dem aktuellen Schlusskurs und dem Schlusskurs von vor zwölf Kerzen. Solange sie positiv ist, hat der Markt den Kurs über das Fenster hinweg nach oben getragen, solange sie negativ ist nach unten, und im Moment des Vorzeichenwechsels dreht das Diagramm die Position. Trotz des Ordnernamens verwendet das Original Momentum, eine absolute Kursdifferenz, und keine prozentuale Änderungsrate.

![schema](schema.svg)

## Strategieübersicht

- Das Momentum über 12 Kerzen wird mit der Nulllinie verglichen, und der Vorwert desselben Indikators sagt, von welcher Seite es kam – zwei Vergleiche ergeben so einen vollständigen Durchbruch.
- Jedes Signal ist eine Umkehr: das Ordervolumen ist das gemeinsame Volumen plus der Betrag der aktuellen Position, sodass eine Order die alte Seite schließt und die neue eröffnet.
- Die Position geht in beide Zweige ein: ein Durchbruch nach oben wird nur gekauft, solange das Buch nicht schon long ist, ein Durchbruch nach unten nur verkauft, solange es nicht schon short ist.
- Das Original friert den Handel zusätzlich für 55 Kerzen nach jeder Ausführung ein; einen Balkenzähler gibt es als Baustein nicht, also entfällt diese Pause und das Diagramm reagiert auf jeden Durchbruch.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Auf der Vorkerze stand das Momentum auf oder unter null, jetzt steht es darüber, und die Position ist nicht long. Die Order kauft das Umkehrvolumen zu Markt und schließt damit einen Short und eröffnet den Long in einem Schritt.
- **Short-Einstieg**: Auf der Vorkerze stand das Momentum auf oder über null, jetzt steht es darunter, und die Position ist nicht short. Die Order verkauft das Umkehrvolumen zu Markt und schließt damit einen Long und eröffnet den Short in einem Schritt.
- **Ausstieg**: Einen eigenen Ausstiegsbaustein gibt es nicht. Die Position wird gehalten, bis der entgegengesetzte Nulldurchgang sie dreht, und das Original kennt weder einen Stop-Loss noch den ATR-Stop, den seine README erwähnt.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Momentum Length | 12 | Anzahl der Kerzen, über die das Momentum zurückblickt: der Wert ist der aktuelle Schlusskurs minus der Schlusskurs von so vielen Kerzen zuvor. |
| Volume | 1 | Basisordervolumen in Lots; die Umkehrorder addiert den Betrag der offenen Position dazu. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den Momentum-Indikator, dessen Ausgang sowohl zu den Vergleichsbausteinen als auch zu einem Vorwert-Baustein für den Stand der letzten Kerze führt.
- Vier Vergleichsbausteine teilen sich eine Nullkonstante, die zugleich als Bezug für die beiden Positionsprüfungen dient.
- Jedes logische UND verbindet die aktuelle Seite der Null, die vorherige Seite und die Positionsbedingung und löst einen Positionsbaustein aus.
- Ein Formelbaustein berechnet die Umkehrgröße als gemeinsames Volumen plus Betrag der Position und speist das Volumen beider Orders.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
