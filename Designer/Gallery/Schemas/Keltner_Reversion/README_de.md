# Diagramm der Keltner-Kanal-Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Keltner-Kanal ist ein gleitender Durchschnitt mit einer Volatilitätshülle: Die Breite stammt aus der Average True Range, sodass die Bänder mit dem Markt atmen, statt in festem Abstand zu liegen. Dieses Diagramm behandelt einen Schlusskurs außerhalb des Kanals als Überschießen, stellt sich dagegen und gibt den Trade an der Mittellinie zurück.

![schema](schema.svg)

## Strategieübersicht

- Der Kanal wird von Hand gebaut statt aus dem fertigen Indikator KeltnerChannels übernommen, denn dieser Baustein bindet Durchschnitt und ATR an eine einzige Länge, während das Original 20 für die EMA und 14 für die ATR verwendet.
- Zwei Formelbausteine bilden die Bänder wörtlich ab: EMA plus und minus ATR mal Multiplikator, wobei der Multiplikator als Parameter herausgeführt ist und den Kanal ohne Eingriff ins Diagramm weitet oder verengt.
- Die Mittellinie ist die gesamte Ausstiegsregel: Der Trade wird zurückgegeben, sobald der Kurs auf die andere Seite der EMA wechselt, das Ziel wandert also mit dem Durchschnitt.
- Das Original läuft auf Minutenkerzen und sperrt den Handel nach jedem Trade für 500 Bars, was die Position praktisch auch hält. Die mitgelieferte Historie besteht aus Fünf-Minuten-Daten, daher arbeitet das Diagramm auf Fünf-Minuten-Kerzen; die Sperre ist nicht nachgebildet, weil der Designer keinen zustandsbehafteten Bar-Zähler hat, und das Diagramm handelt deshalb häufiger und hält kürzer.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Schlusskurs liegt unter dem unteren Band, also mehr als ATR mal Multiplikator unter der EMA, und die Position ist neutral. Die Order kauft das eingestellte Volumen.
- **Short-Einstieg**: Der Schlusskurs liegt über dem oberen Band, also mehr als ATR mal Multiplikator über der EMA, und die Position ist neutral. Die Order verkauft das eingestellte Volumen.
- **Ausstieg**: Ein Long wird geschlossen, sobald der Schlusskurs wieder über der EMA steht, ein Short, sobald er wieder darunter liegt. Das Original deklariert einen Stop-Multiplikator, verwendet ihn aber nie, daher hat auch das Diagramm weder Stop-Loss noch Take-Profit.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| EMA Length | 20 | Glättungsperiode des exponentiellen gleitenden Durchschnitts, der die Mittellinie bildet. |
| ATR Length | 14 | Glättungsperiode der Average True Range, die die Kanalbreite bestimmt. |
| ATR multiplier | 2 | Wie viele ATR die Bänder von der Mittellinie entfernt liegen. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den Konverter für den Schlusskurs und beide Indikatorbausteine; die ATR braucht die ganze Kerze und hängt deshalb direkt an der Kerzenquelle.
- Jedes Band ist ein Formelbaustein über drei Eingänge: EMA, ATR und die gemeinsame Multiplikatorkonstante.
- Vier Vergleichsbausteine prüfen den Schlusskurs gegen beide Bänder und gegen die Mittellinie, drei weitere vergleichen die Position mit null.
- Jedes logische UND verbindet eine Kursbedingung mit einer Positionsbedingung; die Einstiegsbausteine tragen die Bedingung Position eröffnen und eine gemeinsame Volumenkonstante, die Schließbausteine die Bedingung Position schließen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
