# Diagramm der Strategie ADX + MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Zwei klassische Indikatoren teilen sich die Arbeit: Der MACD gegenüber seiner Signallinie zeigt, wohin der Markt neigt, und der ADX sagt, ob die Bewegung stark genug ist, um sie zu handeln. Für den Einstieg braucht es beides, der Ausstieg hört dagegen allein auf den MACD, sodass die Position bereits beim Kippen des Momentums verlassen wird, auch wenn der Trend noch als stark gilt.

![schema](schema.svg)

## Strategieübersicht

- Die ADX-Linie wird aus dem zusammengesetzten Wert des Average Directional Index gelesen und mit einer einzigen Stärkeschwelle verglichen.
- Die Richtung ergibt sich aus der Lage der MACD-Linie zu ihrer Signallinie, nicht aus dem Moment der Kreuzung — eine neue Position kann also jederzeit entstehen, solange der MACD auf einer Seite bleibt.
- Der Stärkefilter schützt nur die Einstiege: Der Ausstieg erfolgt allein durch den Seitenwechsel des MACD, Stop-Loss und Take-Profit gibt es im Diagramm nicht.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der ADX liegt über der Schwelle, die MACD-Linie über ihrer Signallinie und die Position ist neutral. Der Positionsbaustein kauft das gemeinsame Volumen zum Marktpreis.
- **Short-Einstieg**: Der ADX liegt über der Schwelle, die MACD-Linie unter ihrer Signallinie und die Position ist neutral. Der Positionsbaustein verkauft das gemeinsame Volumen zum Marktpreis.
- **Ausstieg**: Ein Long wird geschlossen, wenn die MACD-Linie unter ihre Signallinie fällt, ein Short, wenn sie darüber steigt; der ADX-Filter wird beim Ausstieg nicht geprüft.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| ADX Length | 14 | Periode des Average Directional Index, die sowohl den Richtungsindex als auch dessen Glättung bestimmt. |
| ADX Threshold | 25 | Stärkeniveau, das die ADX-Linie überschreiten muss, damit ein Einstieg erlaubt ist. |
| Fast EMA length | 12 | Periode der schnellen EMA innerhalb des MACD. |
| Slow EMA length | 26 | Periode der langsamen EMA innerhalb des MACD. |
| Signal EMA length | 9 | Periode der Signal-EMA, die auf der MACD-Linie gerechnet wird. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist beide Indikatoren; Konverter holen die ADX-Linie aus dem Average Directional Index sowie MACD- und Signallinie aus dem MACD-Indikator.
- Drei Vergleiche liefern die Marktbedingungen — Trendstärke, MACD über der Signallinie und MACD darunter —, drei weitere vergleichen die Position mit null.
- Die UND-Bausteine der Einstiege verbinden Stärke, Richtung und neutrale Position; die der Ausstiege verbinden die Richtung mit einer offenen Gegenposition.
- Die Pause von 100 Kerzen, die die C#-Strategie zwischen Trades hält, lässt sich aus Designer-Bausteinen nicht nachbauen, daher steigt dieses Diagramm häufiger ein und aus.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
