# Diagramm der Strategie mit ATR-Stop und -Ziel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine kurze Lektion über Risiko, das an der Schwankungsbreite gemessen wird. Ein Schlusskurs, der die 50er-EMA kreuzt, eröffnet den Trade, der Schlusskurs eben dieser Kerze wird als Einstiegspreis gespeichert, und von da an misst das Diagramm den Abstand des Kurses dazu in Einheiten der Average True Range. Ein ATR-Vielfaches schließt den Trade mit Verlust, ein anderes mit Gewinn — die Ausstiegsdistanz wächst also in ruhigen Märkten und schrumpft in bewegten, statt eine feste Tickzahl zu sein.

![schema](schema.svg)

## Strategieübersicht

- Es werden nur ein Instrument und eine Kerzenreihe verwendet: Die 50er-EMA gibt die Richtung vor, die 14er-ATR liefert den Maßstab für die Ausstiege.
- Den Einstiegspreis halten zwei Variablenbausteine: Der erste übernimmt den Schlusskurs der Signalkerze, der zweite gibt ihn auf jeder folgenden Kerze erneut aus, damit die Ausstiegsbedingungen durchgehend geprüft werden können.
- Zwei Formelbausteine rechnen den Abstand zum Einstiegspreis in ATR-Vielfache um, einmal zugunsten eines Long und einmal zugunsten eines Short, sodass dieselben zwei Schwellen beide Richtungen bedienen.
- Der Ausstieg ist eine Marktorder auf abgeschlossener Kerze, genau wie in der Ursprungsstrategie: An der Börse liegt keine Stop-Order, ein Ausschlag innerhalb der Kerze wirft die Position also nicht heraus.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Schlusskurs kreuzt die EMA nach oben, während die Position neutral ist. Ein Lot wird gekauft, und der Schlusskurs dieser Kerze wird zum Einstiegspreis.
- **Short-Einstieg**: Der Schlusskurs kreuzt die EMA nach unten, während die Position neutral ist. Ein Lot wird verkauft, und der Schlusskurs dieser Kerze wird zum Einstiegspreis.
- **Ausstieg**: Die Position wird auf der ersten abgeschlossenen Kerze geschlossen, auf der sich der Kurs um StopFactor ATR gegen den Einstiegspreis oder um TakeFactor ATR zu seinen Gunsten bewegt hat. Beide Bausteine zur Positionsänderung stehen auf Schließen, sodass jeder nur auf seiner Seite auslöst.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| EMA Length | 50 | Periode des exponentiellen gleitenden Durchschnitts, den der Schlusskurs kreuzen muss. |
| ATR Length | 14 | Periode der Average True Range, die Stop und Ziel skaliert. |
| Stop, ATR | 1.5 | Stopdistanz in ATR: der Verlust, der den Trade schließt. |
| Take, ATR | 2 | Zieldistanz in ATR: der Gewinn, der den Trade schließt. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:15:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist einen Konverter für den Schlusskurs, die EMA und die ATR; ein Kreuzungsbaustein vergleicht Schlusskurs und EMA, ein logisches NICHT macht aus der Abwärtskreuzung das Short-Signal.
- Die aktuelle Position wird gegen eine Nullkonstante geprüft, und jedes logische UND verbindet diese Prüfung mit einer Kreuzung, sodass nur aus der Neutralstellung eröffnet wird.
- Den Einstiegspreis halten zwei Variablenbausteine; der zweite wird von der Kerzenreihe ausgelöst — deshalb ist diese Verbindung die letzte, die der Kerzenbaustein bedient, und deshalb rechnet der Ausstieg schon auf der Einstiegskerze mit dem richtigen Preis.
- Vier Vergleichsbausteine prüfen die beiden ATR-Distanzen gegen die Stop- und Zielkonstanten, zwei logische ODER fassen sie zusammen, und zwei auf Schließen gestellte Bausteine senden die Ausstiegsorders.
- Die Ursprungsstrategie wartet sechs Kerzen zwischen zwei Trades. Für einen solchen Zähler gibt es unter den Bausteinen keine Entsprechung, deshalb lässt das Diagramm ihn weg und nimmt die nächste Kreuzung sofort.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
