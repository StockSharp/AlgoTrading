# Diagramm der Strategie Dark Cloud Cover / Piercing Line mit CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Zwei klassische Umkehrmuster aus je zwei Kerzen bestimmen die Seite, und der Commodity Channel Index entscheidet, ob die Umkehr überhaupt handelbar ist. Eine Piercing Line wird nur gekauft, solange der CCI tief im negativen Bereich steht, ein Dark Cloud Cover nur verkauft, solange der CCI nach oben gedehnt ist. Kein Signal schließt eine Position: das übernehmen Take Profit und Stop Loss, die beim Einstieg gesetzt werden.

![schema](schema.svg)

## Strategieübersicht

- Zwei Bausteine des Kerzenmuster-Indikators tragen von Hand geschriebene Ausdrücke, die die Figur ausbuchstabieren: Richtung der vorigen Kerze, Richtung der aktuellen, wo sie eröffnet hat und ob sie jenseits der Mitte des vorigen Körpers geschlossen hat.
- Der Commodity Channel Index über vierzehn Kerzen ist die Bestätigung: Der Markt muss bereits in die Richtung gedehnt sein, die das Muster umkehrt, sonst wird die Figur verworfen.
- Eine einzige Konstante für das Einstiegsniveau bedient beide Seiten, denn eine Formel dreht ihr Vorzeichen für den Long-Vergleich um.
- Eingestiegen wird nur aus der Neutralstellung, sodass ein Muster, das sich auf der nächsten Kerze wiederholt, die Position nicht verdoppelt.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die vorige Kerze ist bärisch, die aktuelle bullisch, sie eröffnete unter dem vorigen Schlusskurs und schloss über der Mitte des vorigen Körpers, der CCI liegt unter dem negativen Einstiegsniveau und die Position ist neutral. Die Order kauft ein Lot zum Markt.
- **Short-Einstieg**: Die vorige Kerze ist bullisch, die aktuelle bärisch, sie eröffnete über dem vorigen Schlusskurs und schloss unter der Mitte des vorigen Körpers, der CCI liegt über dem Einstiegsniveau und die Position ist neutral. Die Order verkauft ein Lot zum Markt.
- **Ausstieg**: Nur der Baustein zum Positionsschutz: Take Profit zwei Prozent vom Einstiegskurs entfernt, Stop Loss ein Prozent. Auch die Originalstrategie kennt keinen Ausstieg per Signal, hier fehlt also nichts.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| CCI Length | 14 | Glättungsperiode des Commodity Channel Index. |
| Entry Level | 50 | Wie weit der CCI von null entfernt sein muss, damit ein Muster als bestätigt gilt; die Long-Seite verwendet diesen Wert negativ. |
| Take Profit % | 2 | Abstand des Take Profit vom Einstiegskurs in Prozent. |
| Stop Loss % | 1 | Abstand des Stop Loss vom Einstiegskurs in Prozent. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist beide Musterbausteine, den Commodity Channel Index und den Konverter, der dem Schutzbaustein den Schlusskurs liefert.
- Eine Konstante hält das Einstiegsniveau, eine Formel kehrt ihr Vorzeichen um, sodass eine einzige optimierbare Zahl beide CCI-Vergleiche steuert.
- Jedes logische UND verbindet ein Muster, seine CCI-Bestätigung und die Prüfung auf Neutralstellung und löst einen Baustein zur Positionsänderung im Modus "nur eröffnen" aus.
- Zwei Punkte des Originals sind vereinfacht: Dort wird zusätzlich eine echte Kurslücke über das Hoch oder unter das Tief der vorigen Kerze verlangt, die ein durchgehend gehandeltes Instrument praktisch nie zeigt, und eine Pause von sechs Kerzen zwischen den Trades, für die es keinen Zählerbaustein gibt. Deshalb muss die Eröffnung hier nur auf der anderen Seite des vorigen Schlusskurses liegen, und jedes bestätigte Muster wird gehandelt.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
