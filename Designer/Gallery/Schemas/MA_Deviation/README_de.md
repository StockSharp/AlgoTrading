# Diagramm der Strategie zur Abweichung vom gleitenden Durchschnitt
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der einfache gleitende Durchschnitt gilt als fairer Preis, und das gesamte Signal ist der in Prozent gemessene Abstand des Schlusskurses davon. Ist der Kurs zu weit vom Durchschnitt weggelaufen, stellt sich das Diagramm dagegen und gibt den Trade zurück, sobald der Kurs den Durchschnitt wieder berührt.

![schema](schema.svg)

## Strategieübersicht

- Die Abweichung wird wörtlich in einem einzigen Formelbaustein berechnet: (Close - SMA) / SMA * 100.
- Eine Schwelle bedient beide Seiten: Die Abweichung wird mit dem Plus und dem Minus derselben Zahl verglichen, Long und Short sind damit symmetrisch.
- Eingestiegen wird nur aus der Neutralstellung, und beide Einstiegsbausteine tragen zusätzlich die Bedingung Position eröffnen, sodass nie nachgekauft wird.
- Das Original arbeitet mit Ein-Minuten-Kerzen, einer Schwelle von 2% und einer Pause von 500 Kerzen nach jedem Trade. Die mitgelieferte Historie besteht aus Fünf-Minuten-Daten, deshalb läuft das Diagramm auf Fünf-Minuten-Kerzen mit einer Schwelle von 1%, was etwa zwei Standardabweichungen dieser Reihe entspricht; die Pause wird nicht nachgebildet, da Designer keinen Sperrzähler kennt, und das Diagramm handelt daher häufiger als das Original.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die Abweichung liegt unter der negativen Schwelle, der Schlusskurs also um mehr als den eingestellten Prozentsatz unter dem Durchschnitt, und die Position ist neutral. Die Order kauft das eingestellte Volumen.
- **Short-Einstieg**: Die Abweichung liegt über der positiven Schwelle, der Schlusskurs also um mehr als den eingestellten Prozentsatz über dem Durchschnitt, und die Position ist neutral. Die Order verkauft das eingestellte Volumen.
- **Ausstieg**: Ein Long wird geschlossen, sobald der Schlusskurs den Durchschnitt wieder erreicht oder überschreitet; ein Short, sobald der Schlusskurs den Durchschnitt wieder erreicht oder unterschreitet. Stop-Loss und Take-Profit gibt es wie im Original nicht.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| SMA Length | 20 | Glättungsperiode des einfachen gleitenden Durchschnitts. |
| Deviation, % | 1 | Abstand vom Durchschnitt in Prozent, der einen Trade eröffnet. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist sowohl den Konverter für den Schlusskurs als auch den Indikatorbaustein mit dem gleitenden Durchschnitt.
- Ein Formelbaustein macht daraus die prozentuale Abweichung; eine zweite, winzige Formel dreht das Vorzeichen der Schwellenkonstante um, damit eine einzige veröffentlichte Zahl beide Seiten abdeckt.
- Zwei Vergleichsbausteine prüfen die Abweichung gegen die Schwellen, zwei weitere vergleichen den Schlusskurs mit dem Durchschnitt für die Ausstiege.
- Der Positionsbaustein wird dreimal mit null verglichen und liefert die Merkmale neutral, long und short, die die logischen UND mit den Kursbedingungen verknüpfen.
- Die Einstiege laufen in Bausteine zur Positionsänderung mit der Bedingung Position eröffnen und einer gemeinsamen Volumenkonstante, die Ausstiege in Bausteine mit der Bedingung Position schließen, die kein Volumen benötigen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
