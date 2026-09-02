# Diagramm der Strategie MA + Parabolic SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein einfacher gleitender Durchschnitt sagt, auf welcher Seite des Marktes man stehen sollte, und ein Parabolic SAR sagt, wann: Das Diagramm wartet, bis der Schlusskurs die SAR-Linie in die Richtung kreuzt, in die der Durchschnitt bereits zeigt. Der Gegenkreuzung derselben Linie gibt die Position zurück, sodass die Strategie entweder in einem Trend läuft oder auf den nächsten wartet.

![schema](schema.svg)

## Strategieübersicht

- SimpleMovingAverage ist der Richtungsfilter: Long nur, solange der Schlusskurs darüber liegt, Short nur, solange er darunter liegt.
- ParabolicSar liefert das Timing, und ein einziger Kreuzungsbaustein macht aus dem Durchgang des Kurses durch diese Linie einen einzelnen Impuls: wahr für die Kreuzung nach oben, falsch für die nach unten.
- Die Einstiege sind durch die aktuelle Position abgesichert, die Ausstiege laufen über Schließbausteine, die nur bei einer Position des passenden Vorzeichens tätig werden.
- Zwei Abweichungen vom C#-Original: Dort ersetzt eine schnelle EMA den SAR und die deklarierten SAR-Einstellungen werden nie gelesen, während das Diagramm einen echten ParabolicSar verwendet; außerdem wird die Pause von 20 Bars zwischen Einstiegen nicht nachgebildet.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Schlusskurs kreuzt die ParabolicSar-Linie nach oben, liegt dabei über der SMA, und die Position ist nicht long. Der Baustein kauft das gemeinsame Volumen zum Marktpreis.
- **Short-Einstieg**: Der Schlusskurs kreuzt die ParabolicSar-Linie nach unten, liegt dabei unter der SMA, und die Position ist nicht short. Der Baustein verkauft das gemeinsame Volumen zum Marktpreis.
- **Ausstieg**: Ein Long wird bei der ersten Kreuzung der SAR-Linie nach unten geschlossen, ein Short bei der ersten nach oben, ohne den gleitenden Durchschnitt zu fragen; Stopps und Ziele gibt es wie im Original nicht.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| SMA Length | 20 | Periode des einfachen gleitenden Durchschnitts, der die Trendrichtung bestimmt. |
| SAR Acceleration | 0.02 | Anfänglicher Beschleunigungsfaktor des Parabolic SAR. |
| SAR Max acceleration | 0.2 | Obergrenze, bis zu der der Beschleunigungsfaktor des Parabolic SAR wächst. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist beide Indikatoren und einen Konverter, der den Schlusskurs aus der Kerze liest.
- Der Kreuzungsbaustein vergleicht den Schlusskurs mit der SAR-Linie; ein logisches NICHT macht daraus die Abwärtskreuzung für den Short-Einstieg und den Long-Ausstieg.
- Vergleichsbausteine prüfen den Schlusskurs gegen die SMA und die Position gegen eine Nullkonstante, vier logische UND setzen daraus die Ein- und Ausstiegssignale zusammen.
- Zwei Bausteine zur Positionsänderung eröffnen mit der gemeinsamen Volumenkonstante, zwei weitere schließen mit der Bedingung Position schließen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
