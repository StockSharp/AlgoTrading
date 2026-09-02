# Diagramm der Strategie Simple Multiple Time Frame Moving Average
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Name verspricht zwei Zeiteinheiten, doch die zugrunde liegende C#-Strategie abonniert eine einzige Vier-Stunden-Reihe und berechnet darauf zwei ExponentialMovingAverage unterschiedlicher Länge. Gehandelt wird in Wahrheit die Übereinstimmung ihrer Steigungen: Zeigen der kurze und der lange Durchschnitt nach oben, ist das Diagramm long; zeigen beide nach unten, ist es short; widersprechen sie sich, bleibt die Position unangetastet.

![schema](schema.svg)

## Strategieübersicht

- Zwei ExponentialMovingAverage-Bausteine, ein kurzer und ein langer, arbeiten auf derselben Kerzenreihe; das Diagramm behält dieses eine Abonnement bei, statt eine zweite Zeiteinheit zu erfinden.
- Die Steigung jedes Durchschnitts wird aus dem Vergleich seines aktuellen Werts mit einem Baustein für den vorherigen Wert eine Kerze zurück gelesen: Ein steigender Durchschnitt ist schlicht einer, der über seinem früheren Stand liegt.
- Alle Orders verwenden das feste gemeinsame Volumen, deshalb stellt das Gegensignal die Position nur glatt; für den Einstieg in die andere Richtung braucht es auf der nächsten Kerze ein zweites Signal derselben Richtung, genau wie im Quellcode.
- Die Bedingung ist ein Zustand und kein Ereignis: Sie wird auf jeder abgeschlossenen Kerze neu geprüft, weshalb hier Vergleiche und logische UND genügen und kein Kreuzungsbaustein nötig ist.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die schnelle ExponentialMovingAverage liegt über ihrem eigenen Wert eine Kerze zuvor, die langsame ebenso, und die Position ist nicht bereits long. Der Baustein kauft das gemeinsame Volumen zum Marktpreis: aus der Neutralstellung ein Long-Einstieg, aus einem Short dessen Schließung.
- **Short-Einstieg**: Die schnelle ExponentialMovingAverage liegt unter ihrem eigenen Wert eine Kerze zuvor, die langsame ebenso, und die Position ist nicht bereits short. Der Baustein verkauft das gemeinsame Volumen zum Marktpreis: aus der Neutralstellung ein Short-Einstieg, aus einem Long dessen Schließung.
- **Ausstieg**: Eine eigene Ausstiegsregel gibt es nicht: Die Position wird vom Gegensignal geschlossen, also dann, wenn beide Durchschnitte gedreht haben. Die Ursprungsstrategie kennt weder Stop-Loss noch Take-Profit noch eine Pause zwischen Trades, und dieses Diagramm ebenfalls nicht.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Fast EMA length | 5 | Periode der schnellen ExponentialMovingAverage. |
| Slow EMA length | 20 | Periode der langsamen ExponentialMovingAverage. |
| Volume | 1 | Ordervolumen in Lots; dieselbe Konstante speist beide Bausteine zur Positionsänderung. |
| Candles | 04:00:00 | Zeiteinheit der Kerzen für das gesamte Diagramm; das Original nutzt vier Stunden, die hier beibehalten werden, was auf dem mitgelieferten Monat Historie rund zweihundert Kerzen ergibt. |

## Diagrammdetails

- Der Kerzenbaustein speist beide Indikatorbausteine, und jeder Indikator speist einen Baustein für den vorherigen Wert vom Typ Indikatorwert.
- Vier Vergleichsbausteine machen aus den beiden Durchschnitten und ihren verzögerten Kopien Flaggen für Steigen und Fallen.
- Der Positionsbaustein, zweimal mit einer Nullkonstante verglichen, liefert die Prüfung, die einen Einstieg davon abhält, eine bestehende Position zu vergrößern.
- Jedes logische UND verbindet eine Bedingung des schnellen Durchschnitts, eine des langsamen und eine der Position und löst einen Baustein zur Positionsänderung aus, der seine Größe aus der gemeinsamen Volumenkonstante bezieht.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
