# Diagramm der Williams-%R-Level-Cross-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Williams %R zeigt, wo der Schlusskurs innerhalb der Spanne der letzten Kerzen liegt, von 0 ganz oben bis -100 ganz unten. Dieses Diagramm handelt den Moment, in dem der Oszillator in eine Zone hineinläuft, und nicht den, in dem er sie verlässt: ein Fall durch das untere Level kauft, ein Anstieg durch das obere Level verkauft. Die prozentuale Absicherung nimmt den Trade wieder heraus.

![schema](schema.svg)

## Strategieübersicht

- Williams %R mit der Länge 14 wird auf abgeschlossenen Stundenkerzen berechnet, die der Tester aus der mitgelieferten Fünf-Minuten-Historie aufbaut.
- Das Signal ist die Durchquerung selbst: der vorherige Wert auf der einen Seite des Levels, der aktuelle auf der anderen, sodass ein langer Aufenthalt in der Zone nur einmal auslöst.
- Das ist der Eintritt in die Zone, das Spiegelbild der klassischen Lesart, die auf den Rückweg des Oszillators wartet, und entspricht dem Direct-Modus der ursprünglichen Strategie.
- Das Original hat zusätzlich getrennte Freigaben für Long- und Short-Einstiege; beide sind standardmäßig aktiv, deshalb verdrahtet das Diagramm beide Seiten, und eine Seite lässt sich durch Abhängen ihres Zweigs abschalten.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der vorherige %R lag über dem unteren Level und der aktuelle liegt auf oder unter ihm, und die Position ist nicht long. Die Order kauft ein Lot: aus der Neutralstellung ein Long-Einstieg, aus einem Short dessen Schließung.
- **Short-Einstieg**: Der vorherige %R lag unter dem oberen Level und der aktuelle liegt auf oder über ihm, und die Position ist nicht short. Die Order verkauft ein Lot: aus der Neutralstellung ein Short-Einstieg, aus einem Long dessen Schließung.
- **Ausstieg**: Der Schutzbaustein schließt den Trade bei 2 Prozent Take-Profit oder 1 Prozent Stop-Loss zum Einstiegspreis; davor stellt die gegenläufige Durchquerung die Position glatt, da alle Orders dasselbe Volumen verwenden.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Williams %R Length | 14 | Berechnungslänge des Williams %R. |
| Low Level | -80 | Level, das der Oszillator nach unten durchqueren muss, um einen Long-Einstieg freizugeben. |
| High Level | -20 | Level, das der Oszillator nach oben durchqueren muss, um einen Short-Einstieg freizugeben. |
| Take profit, % | 2 | Abstand des Take-Profits zum Einstiegspreis in Prozent. |
| Stop loss, % | 1 | Abstand des Stop-Loss zum Einstiegspreis in Prozent. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 01:00:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den Indikatorbaustein mit Williams %R, ein Baustein für den vorherigen Wert hält den Stand einer Kerze zuvor.
- Vier Vergleichsbausteine bilden die beiden Durchquerungen aus vorherigem und aktuellem Wert gegen die beiden Level-Konstanten.
- Zwei weitere Vergleichsbausteine prüfen die Position gegen eine Nullkonstante, und jedes logische UND verbindet eine Durchquerung mit ihrer Positionsprüfung.
- Beide Bausteine zur Positionsänderung senden Marktorders mit dem Volumen einer gemeinsamen Konstante, und ihre Trades laufen in den Schutzbaustein mit Take-Profit und Stop-Loss.
- Das Original sichert mit absoluten Preisabständen ab; das Diagramm verwendet stattdessen Prozente des Einstiegspreises, damit dieselben Zahlen auf jedem Instrument passen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
