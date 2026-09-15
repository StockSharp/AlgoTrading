# Strategiediagramm: MACD-Übereinstimmung über zwei Zeitrahmen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein MACD ist eine Meinung; zwei MACD auf verschiedenen Zeitrahmen, die sich einig sind, sind ein Signal. Das Diagramm misst auf Halbstunden- und auf Vier-Stunden-Kerzen, wie weit der MACD von seiner eigenen Signallinie entfernt liegt, und eröffnet eine Position nur dann, wenn beide Messwerte in dieselbe Richtung zeigen und der Spread im Orderbuch eng genug zum Handeln ist.

![schema](schema.svg)

## Strategieübersicht

- Zwei Kerzen-Bausteine arbeiten auf demselben Instrument mit zwei Zeitrahmen: eine halbe Stunde für den Handel und vier Stunden für die Bestätigung.
- Jede Reihe speist ihren eigenen MACD, und zwei Konverter holen aus jedem Indikator die MACD-Linie und die Signallinie heraus.
- Eine Formel zieht die Signallinie von der MACD-Linie ab, sodass jeder Zeitrahmen auf eine einzige Zahl zusammenschrumpft: positiv heißt, die schnelle Seite führt, negativ heißt, sie hinkt hinterher.
- Market depth wird auf die beste Ebene beschnitten, zwei Konverter lesen den besten Ask und den besten Bid aus, und eine dritte Formel macht daraus den Spread.
- Sync hält alle drei Zahlen fest und gibt sie gemeinsam im Vier-Stunden-Takt frei. Genau das macht den Vergleich ehrlich: Das Orderbuch aktualisiert sich hunderte Male pro Kerze, und ohne Sync würden die Messwerte nie zum selben Moment gehören.
- Nach Sync werden die beiden Abstände mit null und der Spread mit seinem Limit verglichen, und eine logische Bedingung fasst die drei Antworten zusammen mit einer flachen Position zusammen.
- Beide Einstiege sind Market-Orders mit festem Volumen und werden nur aus einer flachen Position heraus ausgelöst.
- Position protection trägt den Trade: Sie beobachtet die Einstiegs-Fills, liest den aktuellen Kurs aus dem Orderbuch und schließt bei einem Take-Profit oder einem Stop-Loss, die in Prozent angegeben sind.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Im Vier-Stunden-Takt sind beide Abstände positiv — der MACD liegt im Handels- wie im Bestätigungszeitrahmen über seiner Signallinie —, der Spread liegt innerhalb seines Limits und die Position ist flach. Position modify kauft das Ordervolumen zum Marktpreis.
- **Short-Einstieg**: Im selben Takt sind beide Abstände negativ, unter denselben Bedingungen für Spread und flache Position. Position modify verkauft das Ordervolumen zum Marktpreis.
- **Ausstieg**: Im Diagramm gibt es kein Ausstiegssignal: Sobald eine Position offen ist, gehört sie Position protection, die sie bei 1.5% Gewinn oder 1% Verlust gegenüber dem Einstiegskurs schließt. Entgegengesetzte Messwerte werden ignoriert, solange die Position läuft, sodass ein Trade nie mitten im Lauf gedreht wird.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Trading Candles | 00:30:00 | Zeitrahmen, auf dem der handelnde MACD arbeitet. |
| Confirming Candles | 04:00:00 | Zeitrahmen des bestätigenden MACD und zugleich der Takt, in dem Sync alles freigibt. |
| Maximum Spread | 50 | Größter Spread in Kurseinheiten, bei dem ein Einstieg noch erlaubt ist. |
| Order Volume | 1 | Ordergröße in Lots. |
| Take Profit, % | 1.5 | Abstand des Take-Profit, in Prozent des Einstiegskurses. |
| Stop Loss, % | 1 | Abstand des Stop-Loss, in Prozent des Einstiegskurses. |

## Diagrammdetails

- Beide MACD-Bausteine sind so eingestellt, dass sie nur ausgeformte und endgültige Werte liefern; eine noch offene Kerze kann die Entscheidung also nicht verschieben.
- Der bestätigende MACD ist bewusst kürzer eingestellt als der handelnde: Vier-Stunden-Kerzen sind in einem Monat Historie rar, und die Standardwerte 12/26/9 würden den größten Teil davon mit dem Warmlaufen verbringen.
- Sync benennt jede Linie, die er hält, und beide Enden jeder Linie sind verbunden — ein Wert, der hineingeschickt, aber nie herausgenommen wird, ließe den Baustein warten, und die Strategie würde nicht starten.
- Der Spread wird nach Sync verglichen und nicht dort, wo er ankommt; nur deshalb kann ein vom Orderbuch getriebener Filter überhaupt in einer von Kerzen getriebenen Bedingung stehen.
- Position protection wird aus dem Orderbuch gespeist und nicht aus einem Kerzenkurs, sodass sie den Ausstieg am aktuellen besten Kursniveau bepreist.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
