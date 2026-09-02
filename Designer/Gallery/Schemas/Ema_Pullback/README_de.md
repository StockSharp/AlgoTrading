# Diagramm der EMA-Pullback-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Trenddiagramm, das den Ausbruch bewusst nicht kauft. Die beiden exponentiellen gleitenden Durchschnitte bestimmen die Richtung, und der Einstieg wartet, bis der Schlusskurs an den schnellen Durchschnitt zurückkommt - so wird die Position zu einem besseren Kurs innerhalb einer bereits laufenden Bewegung eröffnet. Den Ausstieg entscheidet der Trend selbst: Die Position wird geschlossen, sobald die Durchschnitte die Plätze tauschen.

![schema](schema.svg)

## Strategieübersicht

- Zwei exponentielle gleitende Durchschnitte des Schlusskurses, ein schneller mit 8 und ein langsamer mit 21, legen fest, auf welcher Seite das Diagramm überhaupt handeln darf.
- Ein Kreuzungsbaustein beobachtet den Schlusskurs gegen den schnellen Durchschnitt, sodass der Rücklauf genau auf der Kerze erfasst wird, auf der der Kurs an den Durchschnitt zurückkehrt, und nicht auf jeder Kerze in dessen Nähe.
- Ein- und Ausstiege laufen über getrennte Zweige: Zwei Bausteine zur Positionsänderung eröffnen mit dem Ordervolumen, zwei weitere schließen nur, was gehalten wird.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der schnelle EMA liegt über dem langsamen, der Schlusskurs fällt zurück auf den schnellen EMA und die Position ist nicht long. Die Order kauft Volume plus den Betrag der aktuellen Position: aus der Neutralstellung ein Long-Einstieg, aus einem Short die direkte Umkehr in einen Long.
- **Short-Einstieg**: Der schnelle EMA liegt unter dem langsamen, der Schlusskurs steigt zurück auf den schnellen EMA und die Position ist nicht short. Die Order verkauft Volume plus den Betrag der aktuellen Position: aus der Neutralstellung ein Short-Einstieg, aus einem Long die direkte Umkehr in einen Short.
- **Ausstieg**: Ein Long wird geschlossen, wenn der schnelle EMA unter den langsamen fällt, ein Short, wenn der schnelle darüber steigt; beide Schließbausteine arbeiten auf der gesamten offenen Position, ein wiederholtes Signal ohne Position bewirkt daher nichts. Einen Schutzstopp gibt es nicht - so ist die Originalstrategie geschrieben.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Fast EMA length | 8 | Periode des schnellen exponentiellen gleitenden Durchschnitts, an den der Kurs zurückläuft. |
| Slow EMA length | 21 | Periode des langsamen exponentiellen gleitenden Durchschnitts, der die Trendrichtung vorgibt. |
| Volume | 1 | Basisvolumen der Order in Lots; bei einer Umkehr wird die offene Position addiert. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist beide Durchschnitte und einen Konverter, der den Schlusskurs liest.
- Der Kreuzungsbaustein bekommt den schnellen EMA auf den oberen und den Schlusskurs auf den unteren Eingang, sodass sein wahrer Ausgang den Rücklauf des Schlusskurses nach unten an den Durchschnitt bedeutet und ein logisches NICHT davon den Rücklauf nach oben.
- Zwei Vergleichsbausteine stellen die Durchschnitte gegenüber, vier weitere vergleichen die Position mit einer gemeinsamen Null-Konstante und liefern so die Filter für Ein- und Ausstiege.
- Der Einstiegszweig bezieht sein Volumen aus einer Formel, die den Betrag der Position zur Volumenkonstante addiert, während die beiden Schließbausteine auf Position schließen gestellt sind und gar kein Volumen brauchen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
