# Diagramm der CCI-Ausbruchsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Commodity Channel Index verbringt die meiste Zeit zwischen -100 und +100, daher gilt das Verlassen dieses Bandes als Beginn einer Bewegung und nicht als Übertreibung. Das Diagramm vergleicht den Index mit seinem eigenen Wert eine Kerze zuvor — genau das macht aus einer Marke einen Ausbruch — und ist stets im Markt: Jedes Signal dreht die Position, statt sie nur zu schließen.

![schema](schema.svg)

## Strategieübersicht

- Ein Indikatorbaustein rechnet den Commodity Channel Index, ein Baustein für den Vorwert hält den Stand der vorherigen Kerze, sodass das Paar ein Kreuzen der Marke beschreibt und nicht bloß ein Verweilen darüber.
- Beide Marken sind gewöhnliche Konstanten, das Ausbruchsband lässt sich also verbreitern, verengen und wie jeder andere Parameter optimieren.
- Das Ordervolumen ergibt sich aus dem Grundvolumen plus dem Betrag der aktuellen Position, sodass eine einzige Marktorder die Gegenposition schließt und die neue eröffnet.
- Die Originalstrategie überspringt nach jedem Signal zwei Kerzen; für diesen Zähler gibt es keinen Baustein, er entfällt, weshalb dieses Diagramm ein bis zwei Kerzen früher drehen kann als der Quellcode.
- Das Original arbeitet mit Stundenkerzen; das Diagramm ist auf Fünf-Minuten-Kerzen skaliert, passend zur mitgelieferten Beispielhistorie.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der CCI schloss die vorherige Kerze auf oder unter der oberen Marke und liegt jetzt darüber, und die Position ist noch nicht long. Die Order kauft das Grundvolumen plus einen offenen Short und dreht die Position auf long.
- **Short-Einstieg**: Der CCI schloss die vorherige Kerze auf oder über der unteren Marke und liegt jetzt darunter, und die Position ist noch nicht short. Die Order verkauft das Grundvolumen plus einen offenen Long und dreht die Position auf short.
- **Ausstieg**: Einen eigenen Ausstieg gibt es nicht: Die Strategie bleibt im Markt, und der Gegenausbruch schließt den laufenden Trade und eröffnet zugleich den neuen. Auch der Originalcode kennt weder Stop-Loss noch Take-Profit.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| CCI Length | 20 | Glättungsperiode des Commodity Channel Index. |
| Upper level | 100 | Marke, die der Index für einen Long-Ausbruch nach oben kreuzen muss. |
| Lower level | -100 | Marke, die der Index für einen Short-Ausbruch nach unten kreuzen muss. |
| Volume | 1 | Grundvolumen der Order in Lots; beim Drehen kommt die offene Position hinzu. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den Commodity Channel Index, dessen Ausgang sowohl zu den Vergleichsbausteinen als auch zum Baustein für den Vorwert führt.
- Je Seite prüfen zwei Vergleichsbausteine den aktuellen und den vorherigen Stand gegen dieselbe Marken-Konstante, was die Ausbruchsbedingung des Quellcodes exakt nachbildet.
- Jedes logische UND verbindet aktuellen Stand, Vorwert und eine Positionsprüfung, bevor es einen Baustein zur Positionsänderung auslöst.
- Ein Formelbaustein addiert das Grundvolumen zum Betrag der Position und versorgt beide Orderbausteine, sodass eine Marktorder die gesamte Umkehr ausführt.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
