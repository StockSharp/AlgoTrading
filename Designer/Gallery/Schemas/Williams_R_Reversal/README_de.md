# Diagramm der Williams-%R-Levelkreuzung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Williams %R zeigt, wo der letzte Schlusskurs zwischen dem Hoch und dem Tief des jüngsten Fensters liegt, auf einer Skala von -100 ganz unten bis 0 ganz oben. Das Diagramm handelt nicht die Zeit in einer Extremzone, sondern den Moment des Verlassens: die Rückkehr über -80 kauft, die Rückkehr unter -20 verkauft.

![schema](schema.svg)

## Strategieübersicht

- Der Williams %R wird auf abgeschlossenen Kerzen eines einzelnen Instruments berechnet und entspricht vollständig der Hoch-Tief-Formel, die die Originalstrategie von Hand rechnet.
- Zwei Marken teilen die Skala: unter -80 gilt der Markt als überverkauft, über -20 als überkauft.
- Ein Baustein für den Vorwert hält den Wert der vorangegangenen Kerze fest, sodass jede Marke zweimal geprüft wird und nur die Kerze der Kreuzung ein Signal liefert.
- Die aktuelle Position geht in beide Entscheidungen ein, sodass keine Order eine bestehende Position vergrößert.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der vorige %R-Wert lag unter der unteren Marke, der aktuelle liegt auf oder über ihr und die Position ist nicht long. Die Order kauft ein Lot: aus der Neutralstellung ein Long-Einstieg, aus einem Short die Rückkehr auf null.
- **Short-Einstieg**: Der vorige %R-Wert lag über der oberen Marke, der aktuelle liegt auf oder unter ihr und die Position ist nicht short. Die Order verkauft ein Lot: aus der Neutralstellung ein Short-Einstieg, aus einem Long die Rückkehr auf null.
- **Ausstieg**: Es gibt keinen eigenen Ausstiegsbaustein: Die Gegenkreuzung schickt eine Marktorder desselben Volumens und stellt die Position glatt, genau wie im Original. Dieses pausiert nach jedem Trade zusätzlich fünfzig Kerzen lang; einen Balkenzähler gibt es hier als Baustein nicht, daher trägt die Levelkreuzung diese Aufgabe allein und das Diagramm handelt etwas häufiger als die Vorlage.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Williams %R Length | 14 | Fenster aus Hoch und Tief, über das der Williams %R gemessen wird. |
| Lower Level | -80 | Marke, die der Indikator für ein Kaufsignal wieder nach oben durchlaufen muss. |
| Upper Level | -20 | Marke, die der Indikator für ein Verkaufssignal wieder nach unten durchlaufen muss. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den Williams-%R-Indikatorbaustein, dessen Ausgang sowohl in die Vergleichsbausteine als auch in den Vorwertbaustein läuft.
- Vier Vergleichsbausteine bilden die beiden Kreuzungen: der frühere Wert gegen eine Marke und der aktuelle Wert gegen dieselbe Marke.
- Der Positionsbaustein wird zweimal mit einer Nullkonstante verglichen und liefert so den Schutz «nicht long» für die Kaufseite und «nicht short» für die Verkaufsseite.
- Jedes logische UND verbindet die beiden Hälften einer Kreuzung mit ihrem Positionsschutz und löst einen Baustein zur Positionsänderung aus; beide beziehen ihr Volumen aus einer gemeinsamen Konstante.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
