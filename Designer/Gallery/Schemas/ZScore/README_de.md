# Diagramm der Z-Score-Mean-Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Schlusskurs wird in einen Z-Score verwandelt: den Abstand zu einem gleitenden Durchschnitt, gemessen in Standardabweichungen. So beschreibt eine einzige Zahl, wie weit der Markt gelaufen ist, unabhängig vom Kursniveau des Instruments. Das Diagramm stellt sich gegen die Übertreibung und gibt den Trade zurück, sobald der Score wieder nahe null liegt.

![schema](schema.svg)

## Strategieübersicht

- Der Z-Score wird von Hand aus SimpleMovingAverage und StandardDeviation gebaut: (Close - SMA) / StandardDeviation in einem einzigen Formelbaustein.
- Eine gespiegelte Formel liefert denselben Score mit umgekehrtem Vorzeichen, sodass ein Einstiegs- und ein Ausstiegsniveau beide Seiten abdecken statt vier getrennter Konstanten.
- Eingestiegen wird nur aus der Neutralstellung; die Einstiegsbausteine tragen zusätzlich die Bedingung Position eröffnen, sodass das Diagramm nie in eine bestehende Position nachlegt.
- Das Original arbeitet auf Minutenkerzen und sperrt den Handel nach jedem Trade für 500 Bars. Die mitgelieferte Historie besteht aus Fünf-Minuten-Daten, daher läuft das Diagramm auf Fünf-Minuten-Kerzen; die Sperre lässt sich nicht nachbilden, weil der Designer keinen zustandsbehafteten Bar-Zähler kennt. Das Diagramm handelt deshalb häufiger und hält kürzer als das Original.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Z-Score liegt unter dem negativen Einstiegsniveau, der Schlusskurs steht also mehr als die eingestellte Zahl an Standardabweichungen unter dem Durchschnitt, und die Position ist neutral. Die Order kauft das eingestellte Volumen.
- **Short-Einstieg**: Der Z-Score liegt über dem Einstiegsniveau, der Schlusskurs steht also mehr als die eingestellte Zahl an Standardabweichungen über dem Durchschnitt, und die Position ist neutral. Die Order verkauft das eingestellte Volumen.
- **Ausstieg**: Ein Long wird geschlossen, sobald der Z-Score wieder über das Ausstiegsniveau steigt, ein Short, sobald er unter dessen negativen Wert fällt. Es gibt weder Stop-Loss noch Take-Profit, genau wie in der Originalstrategie.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| SMA Length | 10 | Glättungsperiode des gleitenden Durchschnitts, von dem aus gemessen wird. |
| StandardDeviation Length | 10 | Periode der Standardabweichung, durch die der Abstand geteilt wird. |
| Entry z-score | 1.5 | Abstand zum Durchschnitt in Standardabweichungen, der einen Trade eröffnet. |
| Exit z-score | 0.5 | Abstand zum Durchschnitt in Standardabweichungen, bei dem ein offener Trade zurückgegeben wird. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den Konverter für den Schlusskurs und beide Indikatorbausteine, die Werte erst nach ihrer Ausbildung senden.
- Zwei Formelbausteine bilden den Score und sein Negativ aus denselben drei Eingängen, sodass die gespiegelten Vergleiche ohne zusätzliche Konstanten auskommen.
- Vier Vergleichsbausteine prüfen beide Scores gegen Einstiegs- und Ausstiegsniveau, drei weitere vergleichen die Position mit null.
- Jedes logische UND verbindet eine Score-Bedingung mit einer Positionsbedingung; die Einstiegsbausteine beziehen ihr Volumen aus einer gemeinsamen Konstante, die Schließbausteine arbeiten mit der Bedingung Position schließen und brauchen keines.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
