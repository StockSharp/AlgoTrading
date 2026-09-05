# Diagramm der Choppiness-Index-Ausbruchsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Choppiness Index sagt nicht, wohin der Markt läuft, sondern nur, ob er überhaupt irgendwohin läuft. Das Diagramm nutzt ihn als Schalter: Solange der Index niedrig ist, trendet der Markt, und es wird auf der Seite eröffnet, auf der der Schlusskurs zu einem einfachen gleitenden Durchschnitt steht; steigt der Index zurück in die Seitwärtszone, wird die Position aufgegeben, wie sie gerade steht.

![schema](schema.svg)

## Strategieübersicht

- Der Choppiness Index wird über vierzehn abgeschlossene Kerzen berechnet und als Prozentwert gelesen: niedrige Werte stehen für einen gerichteten Markt, hohe für eine Spanne.
- Der einfache gleitende Durchschnitt über zwanzig Perioden liefert nur die Richtung; er filtert nicht selbst, denn über die Handelserlaubnis hat bereits der Regimetest entschieden.
- Eingestiegen wird nur aus der Neutralstellung, sodass eine Trendphase einen Trade ergibt und nicht einen wachsenden Stapel davon.
- Es gibt weder Stop noch Ziel: Derselbe Index, der den Trade eröffnet hat, beendet ihn auch.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Choppiness Index liegt unter der Trendschwelle, die Kerze hat über dem einfachen gleitenden Durchschnitt geschlossen und die Position ist neutral. Die Order kauft ein Lot und eröffnet einen Long.
- **Short-Einstieg**: Der Choppiness Index liegt unter der Trendschwelle, die Kerze hat unter dem einfachen gleitenden Durchschnitt geschlossen und die Position ist neutral. Die Order verkauft ein Lot und eröffnet einen Short.
- **Ausstieg**: Sobald der Choppiness Index über die Seitwärtsschwelle steigt, wird die offene Position geschlossen: ein Long durch einen Verkauf im Schließmodus, ein Short durch einen Kauf im Schließmodus. Es gibt weder Stop-Loss noch Take-Profit. Das Diagramm verwendet die kanonischen Schwellen 38.2 und 61.8 aus der Indikatordokumentation.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| SMA Length | 20 | Glättungsperiode des einfachen gleitenden Durchschnitts, der dem Einstieg die Richtung gibt. |
| Choppiness Length | 14 | Glättungsperiode des Choppiness Index. |
| Trending Threshold | 38.2 | Indexwert, unter dem ein Einstieg erlaubt ist. |
| Choppy Threshold | 61.8 | Indexwert, oberhalb dessen der Markt als seitwärts gilt und die Position geschlossen wird. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Fünf-Minuten-Zeiteinheit der Kerzen für das gesamte Diagramm. |

## Diagrammdetails

- Der Kerzenbaustein speist den Choppiness Index, den gleitenden Durchschnitt und einen Konverter, der den Schlusskurs aus der Kerze holt.
- Zwei Vergleiche machen aus dem Index zwei Regimekennzeichen — Trend unterhalb der einen Schwelle, Seitwärtsbewegung oberhalb der anderen — und zwei weitere vergleichen den Schlusskurs mit dem Durchschnitt.
- Der Positionsbaustein wird dreimal mit einer Nullkonstante verglichen: das ergibt die Neutralprüfung für die Einstiege sowie eine Long- und eine Short-Prüfung für die Ausstiege.
- Vier logische UND speisen vier Bausteine zur Positionsänderung: zwei eröffnen eine Position und beziehen ihr Volumen aus der gemeinsamen Konstante, zwei schließen nur, was bereits vorhanden ist.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
