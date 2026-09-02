# Diagramm der MFI-Level-Cross-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Money Flow Index gewichtet jede Preisbewegung mit dem dahinterstehenden Volumen und zeigt so, wie viel Geld den Markt tatsächlich treibt. Dieses Diagramm handelt gegen die Extreme: Es kauft auf der Kerze, auf der MFI durch das untere Level in die überverkaufte Zone fällt, und verkauft auf der Kerze, auf der er durch das obere Level in die überkaufte Zone steigt. Ein prozentualer Take-Profit und Stop-Loss beenden jeden Trade.

![schema](schema.svg)

## Strategieübersicht

- Der Money Flow Index mit der Länge 14 wird auf abgeschlossenen Stundenkerzen berechnet, die der Tester aus der mitgelieferten Fünf-Minuten-Historie aufbaut.
- Die Level 30 und 70 werden als Durchbruch gelesen, nicht als Zone: Nur die Kerze, die in eine Zone eintritt, erzeugt ein Signal, nicht die Kerzen, die darin verweilen.
- Die ursprüngliche Strategie hat einen Trend-Schalter, der beide Signale spiegeln kann; das Diagramm behält den Standardmodus Direct, also kauft der Eintritt in die überverkaufte Zone und verkauft der Eintritt in die überkaufte Zone.
- Die aktuelle Position geht in beide Entscheidungen ein, sodass das Schema niemals eine zweite Order auf eine bestehende Position legt.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der vorherige MFI-Wert lag über dem unteren Level und der aktuelle liegt auf oder unter ihm, und die Position ist nicht long. Die Order kauft ein Lot: aus der Neutralstellung ein Long-Einstieg, aus einem Short dessen Schließung.
- **Short-Einstieg**: Der vorherige MFI-Wert lag unter dem oberen Level und der aktuelle liegt auf oder über ihm, und die Position ist nicht short. Die Order verkauft ein Lot: aus der Neutralstellung ein Short-Einstieg, aus einem Long dessen Schließung.
- **Ausstieg**: Der Schutzbaustein schließt den Trade bei 2 Prozent Take-Profit oder 1 Prozent Stop-Loss zum Einstiegspreis; davor stellt die gegenläufige Level-Durchquerung die Position glatt, da alle Orders dasselbe Volumen verwenden.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| MFI Length | 14 | Glättungsperiode des Money Flow Index. |
| Low Level | 30 | Level, das der Indikator nach unten durchqueren muss, um einen Long-Einstieg freizugeben. |
| High Level | 70 | Level, das der Indikator nach oben durchqueren muss, um einen Short-Einstieg freizugeben. |
| Take profit, % | 2 | Abstand des Take-Profits zum Einstiegspreis in Prozent. |
| Stop loss, % | 1 | Abstand des Stop-Loss zum Einstiegspreis in Prozent. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 01:00:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den Indikatorbaustein mit dem Money Flow Index, ein Baustein für den vorherigen Wert hält den Stand einer Kerze zuvor.
- Vier Vergleichsbausteine bilden die beiden Durchquerungen: vorher über dem Level und aktuell auf oder unter ihm für die Long-Seite, vorher darunter und aktuell auf oder darüber für die Short-Seite.
- Zwei weitere Vergleichsbausteine prüfen die Position gegen eine Nullkonstante, und jedes logische UND verbindet eine Durchquerung mit ihrer Positionsprüfung.
- Beide Bausteine zur Positionsänderung senden Marktorders mit dem Volumen einer gemeinsamen Konstante, und ihre eigenen Trades laufen in den Schutzbaustein mit Take-Profit und Stop-Loss.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
