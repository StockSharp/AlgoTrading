# Diagramm der Ausbruchsstrategie mit Bollinger-Bändern und ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Ausbruch lohnt sich nur, wenn der Markt tatsächlich irgendwohin läuft. Dieses Diagramm wartet auf einen Schlusskurs außerhalb eines Bollinger-Bandes — ein Zeichen, dass die Bewegung für die jüngste Schwankungsbreite ungewöhnlich groß ist — und fragt den ADX, ob ein Trend dahintersteht. Sind beide einverstanden, wird in Richtung des Ausbruchs eröffnet und die Position wieder abgegeben, sobald der Kurs zum mittleren Band zurückkehrt.

![schema](schema.svg)

## Strategieübersicht

- Die Bollinger-Bänder werden auf abgeschlossenen Kerzen eines einzelnen Instruments berechnet: Oberes und unteres Band markieren die Ausbruchsniveaus, das mittlere Band — der gleitende Durchschnitt derselben Periode — markiert den Ausstieg.
- Der ADX misst die Trendstärke, ohne etwas über die Richtung zu sagen, und dient deshalb rein als Filter: unterhalb der Schwelle wird jeder Ausbruch übergangen.
- Die aktuelle Position geht in beide Einstiege ein, und die zwei schließenden Bausteine stehen auf Schließen statt Eröffnen, sodass jeder nur auf seiner Seite wirken kann.
- Die Ursprungsstrategie sperrt sich nach jedem Trade für hundert Balken, Ausstiege eingeschlossen. Für diesen Zähler gibt es unter den Bausteinen keine Entsprechung, deshalb lässt das Diagramm ihn weg: Der Ausstieg am mittleren Band greift damit immer, was ohnehin sinnvoller ist.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Schlusskurs liegt über dem oberen Bollinger-Band, der ADX über seiner Schwelle und die Position ist neutral. Ein Lot wird zum Markt gekauft.
- **Short-Einstieg**: Der Schlusskurs liegt unter dem unteren Bollinger-Band, der ADX über seiner Schwelle und die Position ist neutral. Ein Lot wird zum Markt verkauft.
- **Ausstieg**: Ein Long wird beim ersten Schlusskurs unter dem mittleren Band geschlossen, ein Short beim ersten darüber. Stop und Ziel gibt es nicht, genau wie in der Ursprungsstrategie.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Bollinger Length | 20 | Glättungsperiode der Bollinger-Bänder und ihrer Mittellinie. |
| Bollinger Width | 2.0 | Multiplikator der Standardabweichung, der die Bandbreite festlegt. |
| ADX Length | 14 | Periode des Average Directional Index (ADX). |
| ADX Threshold | 25 | Marke, oberhalb derer der ADX als stark genug für den Ausbruchshandel gilt. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist zwei Indikatorbausteine und einen Konverter für den Schlusskurs; drei weitere Konverter holen oberes, unteres und mittleres Band aus dem einen Bollinger-Wert, einer die ADX-Linie aus ihrem eigenen.
- Fünf Vergleichsbausteine erledigen die Arbeit: zwei für den Ausbruch, zwei für die Rückkehr zum mittleren Band und einer für den Trendfilter gegen eine Schwellenkonstante.
- Jedes logische UND verbindet eine Ausbruchsbedingung, den Trendfilter und die Positionsprüfung und löst dann einen auf Eröffnen gestellten Baustein aus, der sein Volumen aus der gemeinsamen Konstante bezieht.
- Die beiden Ausstiegsvergleiche steuern auf Schließen gestellte Bausteine, die kein eigenes Volumen brauchen, weil der Baustein schließt, was offen ist.
- Der Originalcode berechnet die Trendstärke von Hand als ungeglätteten DX. Das Diagramm nimmt stattdessen den Standard-ADX, die nach Wilder geglättete Fassung derselben Größe, weshalb die Zeitpunkte des Schwellenübertritts leicht abweichen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
