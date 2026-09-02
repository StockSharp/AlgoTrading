# Diagramm der Kombination EMA + MACD + RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Drei unabhängige Prüfungen müssen übereinstimmen, bevor dieses Diagramm handelt. Die Lage von EMA 50 zu EMA 200 bestimmt die erlaubte Seite, das Kreuzen der MACD-Linie mit ihrer Signallinie den Zeitpunkt, und der RSI muss im mittleren Band liegen - Schwung ist da, die Bewegung aber noch nicht ausgelaufen. Jedes angenommene Signal dreht die Position mit einer einzigen Marktorder.

![schema](schema.svg)

## Strategieübersicht

- Der Trendfilter ist ein Niveauvergleich zweier exponentieller Durchschnitte: Solange EMA 50 unter EMA 200 liegt, wird nicht gekauft, solange sie darüber liegt, nicht verkauft.
- Der Einstieg ist ein Ereignis und kein Zustand: Nur die Kerze, auf der die MACD-Linie ihre Signallinie kreuzt, darf einen Trade eröffnen, deshalb feuert das Diagramm nicht dauerhaft, solange der Trend hält.
- Der RSI-Korridor macht die Kombination vorsichtig. Ein Long braucht den RSI über der Kaufmarke und noch unter der Obergrenze, ein Short unter der Verkaufsmarke und noch über der Untergrenze, sodass ausgelaufene Bewegungen ausgelassen werden.
- Das Original arbeitet mit 30-Minuten-Kerzen; das Diagramm ist auf Fünf-Minuten-Kerzen skaliert, passend zur mitgelieferten Beispielhistorie. Die Pause von zehn Bars nach einem Trade hat keinen Baustein-Gegenpart und entfällt, wodurch Wiedereinstiege häufiger sind als im Code.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: EMA 50 liegt über EMA 200, die MACD-Linie kreuzt ihre Signallinie nach oben, der RSI steht über der Kaufmarke und noch unter der Obergrenze, und die Position ist nicht bereits long. Die Order kauft das Grundvolumen plus einen offenen Short und dreht ihn mit einer Marktorder auf long.
- **Short-Einstieg**: EMA 50 liegt unter EMA 200, die MACD-Linie kreuzt ihre Signallinie nach unten, der RSI steht unter der Verkaufsmarke und noch über der Untergrenze, und die Position ist nicht bereits short. Die Order verkauft das Grundvolumen plus einen offenen Long und dreht ihn mit einer Marktorder auf short.
- **Ausstieg**: Es gibt weder einen Ausstiegsbaustein noch eine Absicherung, genau wie im Original: Die Position wird bis zum Spiegelsignal gehalten, und dieselbe Order schließt den alten Trade und eröffnet den neuen.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Fast EMA length | 50 | Periode des schnellen exponentiellen Durchschnitts, der den kurzfristigen Trend trägt. |
| Slow EMA length | 200 | Periode des langsamen exponentiellen Durchschnitts, an dem der schnelle gemessen wird. |
| MACD fast length | 12 | Periode der schnellen EMA im MACD. |
| MACD slow length | 26 | Periode der langsamen EMA im MACD. |
| MACD signal length | 9 | Periode der EMA, die den MACD zur Signallinie glättet. |
| RSI length | 14 | Glättungsperiode des Relative-Stärke-Index. |
| RSI buy level | 40 | Über dieser Marke muss der RSI stehen, damit ein Long akzeptiert wird. |
| RSI sell level | 60 | Unter dieser Marke muss der RSI stehen, damit ein Short akzeptiert wird. |
| RSI upper bound | 70 | Obergrenze des RSI-Korridors; darüber gilt ein Long als zu spät. |
| RSI lower bound | 30 | Untergrenze des RSI-Korridors; darunter gilt ein Short als zu spät. |
| Volume | 1 | Grundvolumen der Order in Lots; beim Drehen kommt die offene Position hinzu. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Ein Kerzenbaustein speist vier Indikatorbausteine: die beiden exponentiellen Durchschnitte, den MACD samt Signallinie und den Relative-Stärke-Index.
- Zwei Konverter zerlegen den MACD-Wert in die Linien Macd und Signal; ein Kreuzungsbaustein macht daraus den bullischen Auslöser, ein NICHT-Baustein invertiert ihn zum bärischen.
- Acht Vergleichsbausteine bilden die Filter: ein Paar für die Durchschnitte, vier für den RSI-Korridor und zwei für den Vergleich der Position mit null.
- Jedes logische UND verbindet fünf Bedingungen, bevor es einen Baustein zur Positionsänderung auslöst, und ein Formelbaustein addiert das Grundvolumen zum Betrag der Position, sodass eine Marktorder die gesamte Umkehr ausführt.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
