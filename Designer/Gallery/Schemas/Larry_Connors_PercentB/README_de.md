# Diagramm der Bollinger-%B-Strategie von Larry Connors
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein reines Long-Diagramm zur Rückkehr zum Mittelwert, gebaut auf Bollinger %B — der Lage des Schlusskurses innerhalb der Bollinger-Bänder, ausgedrückt als Prozentsatz der Bandbreite. Larry Connors' Gedanke ist, dass eine einzelne schwache Kerze nichts beweist; deshalb wartet das Diagramm, bis %B zwei Kerzen hintereinander im unteren Teil des Bandes bleibt, und hält, bis sich %B in den oberen Teil erholt.

![schema](schema.svg)

## Strategieübersicht

- Der Indikator BollingerPercentB erledigt in einem Baustein, was die Ursprungsstrategie von Hand aus den Bändern rechnet; seine Skala reicht von 0 bis 100, weshalb die klassischen Schwellen 0.35 und 0.8 als 35 und 80 geschrieben sind.
- Ein Baustein für den vorherigen Wert hält den Messwert der letzten Kerze fest — er macht aus einer einzelnen schwachen Kerze eine Bedingung über zwei Kerzen.
- Die Strategie ist nur long: Sie kauft die Schwäche und verkauft denselben Long wieder, einen Short eröffnet sie nie.
- Die Position geht in beide Entscheidungen ein, sodass der Einstieg nicht aufstockt und der Ausstieg nicht ohne Position feuert.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: %B der vorherigen und %B der aktuellen Kerze liegen beide unter der unteren Schwelle, und die Position ist nicht long. Die Order kauft ein Lot.
- **Short-Einstieg**: Das Diagramm verkauft niemals leer. Der Verkaufsbaustein dient allein als Ausstieg aus einem offenen Long.
- **Ausstieg**: %B steigt über die obere Schwelle, während die Position long ist. Die Order verkauft dasselbe eine Lot und stellt die Position glatt.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Bollinger Period | 20 | Periode der Bollinger-Bänder, auf denen %B beruht. |
| Bollinger Deviation | 2 | Multiplikator der Standardabweichung der Bollinger-Bänder. |
| Low %B | 35 | Schwelle, unter der %B als unterer Teil des Bandes gilt; sie muss zwei Kerzen lang halten. |
| High %B | 80 | Schwelle, über der %B als erholt gilt, was den Long schließt. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den Indikatorbaustein, dessen Wert sowohl in die Vergleiche als auch in den Baustein für den vorherigen Wert läuft.
- Zwei Vergleiche gegen dieselbe untere Konstante liefern die Bedingung für die aktuelle und die vorige Kerze; ein dritter vergleicht %B für den Ausstieg mit der oberen Konstante.
- Zwei weitere Vergleiche prüfen die Position gegen null: nicht long für den Einstieg, long für den Ausstieg.
- Die beiden logischen UND lösen die Bausteine zur Positionsänderung aus, die ihr Volumen aus einer einzigen gemeinsamen Konstante beziehen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
