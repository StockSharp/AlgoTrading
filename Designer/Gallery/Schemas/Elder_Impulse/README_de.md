# Diagramm der Strategie Elder Impulse System
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Alexander Elder färbt jeden Balken nach zwei Dingen gleichzeitig: nach der Steigung eines exponentiellen gleitenden Durchschnitts, der den Trend zeigt, und nach der Steigung des MACD-Histogramms, das die Dynamik dahinter zeigt. Zeigen beide nach oben, ist der Balken grün und das Diagramm kauft; zeigen beide nach unten, ist er rot und es verkauft. Die Ordergröße ist Volume plus offene Position, sodass jedes Signal die bestehende Position dreht.

![schema](schema.svg)

## Strategieübersicht

- EMA und MACD-Linien werden auf abgeschlossenen Kerzen eines einzelnen Instruments berechnet; das Histogramm entsteht im Diagramm selbst als MACD minus Signal.
- Zwei Bausteine für den Vorwert halten EMA und Histogramm der vorigen Kerze fest, sodass das Diagramm den aktuellen Wert dagegen stellen und die Richtung beider ablesen kann.
- Die Balkenfarbe ist das Paar der Steigungen: EMA steigend und Histogramm steigend ergibt grün, EMA fallend und Histogramm nicht steigend ergibt rot, alles andere gilt als neutral und wird übergangen.
- Die Ursprungsstrategie pausiert nach jedem Trade 65 Balken. Diese Pause ist ein Zähler, und die Designer-Bausteine halten keinen solchen Zustand, deshalb lässt das Diagramm sie weg; die Positionsprüfung verhindert ohnehin eine Wiederholung derselben Seite.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der EMA liegt über seinem Wert von vor einer Kerze, das Histogramm ebenfalls, und die Position ist noch nicht long. Die Order kauft Volume plus die absolute Position: aus der Neutralstellung ein Long-Einstieg, aus einem Short die Drehung in einem Zug.
- **Short-Einstieg**: Der EMA liegt unter seinem Wert von vor einer Kerze, das Histogramm liegt auf oder unter seinem Vorwert, und die Position ist noch nicht short. Die Order verkauft Volume plus die absolute Position und dreht damit einen Long oder eröffnet einen Short.
- **Ausstieg**: Es gibt keinen eigenen Ausstieg: Die Gegenfarbe dreht die Position, und weil die Ordergröße die offene Position enthält, schließt die Drehung den alten Trade und eröffnet zugleich den neuen. Auch die Ursprungsstrategie kennt weder Stop noch Ziel.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| EMA Length | 13 | Periode des exponentiellen gleitenden Durchschnitts, dessen Steigung den Balken färbt. |
| MACD Fast Length | 12 | Schneller gleitender Durchschnitt des MACD. |
| MACD Slow Length | 26 | Langsamer gleitender Durchschnitt des MACD. |
| MACD Signal Length | 9 | Periode der Signallinie; das Histogramm ist der MACD abzüglich dieser Linie. |
| Volume | 1 | Basisvolumen der Order in Lots; beim Drehen kommt die offene Position hinzu. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist zwei Indikatorbausteine, EMA und MACD mit Signallinie; zwei Konverter holen MACD- und Signalwert heraus, ein Formelbaustein zieht sie voneinander ab und liefert das Histogramm.
- Zwei Bausteine für den Vorwert, einer als Indikatorwert und einer als Zahl typisiert, reichen die Werte der vorigen Kerze an vier Vergleichsbausteine weiter, die beide Steigungen bestimmen.
- Jedes logische UND verbindet eine EMA-Bedingung, eine Histogramm-Bedingung und eine Positionsbedingung, sodass nur eingestiegen wird, wenn die Order die gehaltene Seite nicht vergrößert.
- Ein Formelbaustein addiert die absolute Position zur gemeinsamen Volumenkonstante und speist beide Bausteine zur Positionsänderung — genau das macht aus jedem Signal eine Drehung.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
