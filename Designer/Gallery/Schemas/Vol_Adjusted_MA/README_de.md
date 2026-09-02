# Diagramm der volatilitätsangepassten Gleitenden Durchschnitts-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Diagramm legt um einen einfachen gleitenden Durchschnitt einen Kanal, dessen halbe Breite ein Vielfaches der Average True Range beträgt: Wird der Markt unruhig, laufen die Ränder auseinander, beruhigt er sich, rücken sie zusammen. Ein Schlusskurs jenseits eines Randes gilt als echter Ausbruch, und die Position wird zurückgegeben, sobald der Kurs zum Durchschnitt zurückkehrt.

![schema](schema.svg)

## Strategieübersicht

- SimpleMovingAverage zeichnet die Mittellinie, AverageTrueRange bestimmt den Abstand der Ränder, sodass sich der Kanal an die aktuelle Schwankungsbreite anpasst.
- Zwei Formelbausteine setzen die Ränder aus denselben drei Quellen als SMA + Multiplikator * ATR und SMA - Multiplikator * ATR zusammen.
- Eingestiegen wird nur aus der Neutralstellung, und der einzige Ausstieg ist der Schlusskurs, der die Mittellinie wieder durchquert; Stopps und Ziele gibt es wie im C#-Original nicht.
- Zwei Abweichungen vom Original: Die Pause von 500 Bars nach jedem Trade wird nicht nachgebildet, das Diagramm handelt also häufiger, und die Arbeitskerze ist fünf statt einer Minute, denn genau solche Daten liegen bei.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Schlusskurs liegt über dem oberen Rand SMA + Multiplikator * ATR und die Position ist neutral. Der Baustein kauft das gemeinsame Volumen zum Marktpreis.
- **Short-Einstieg**: Der Schlusskurs liegt unter dem unteren Rand SMA - Multiplikator * ATR und die Position ist neutral. Der Baustein verkauft das gemeinsame Volumen zum Marktpreis.
- **Ausstieg**: Ein Long wird auf der ersten Kerze zurückgegeben, die unter der SMA schließt, ein Short auf der ersten, die darüber schließt; die Schließbausteine werden nur tätig, wenn es etwas zu schließen gibt.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| SMA Length | 20 | Periode des einfachen gleitenden Durchschnitts, der Mittellinie und Ausstiegsniveau bildet. |
| ATR Length | 14 | Periode der Average True Range, die die aktuelle Schwankungsbreite misst. |
| ATR multiplier | 2 | Wie viele ATR die Kanalränder von der Mittellinie entfernt liegen. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist beide Indikatoren und einen Konverter, der den Schlusskurs aus der Kerze zieht.
- Zwei Formelbausteine verbinden Durchschnitt, Spanne und Multiplikatorkonstante zum oberen und unteren Rand.
- Vier Vergleichsbausteine bilden die Signale: zwei gegen die Kanalränder für die Einstiege, zwei gegen die Mittellinie für die Ausstiege.
- Der Positionsbaustein wird mit einer Nullkonstante verglichen und geht in jedes logische UND ein, sodass keine Order eine bereits offene Position vergrößert.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
