# Diagramm der Cumulative-Delta-Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Diagramm bildet aus abgeschlossenen Ein-Minuten-Kerzen ein gerichtetes Volumendelta. Ein rollierender Sum(100) liefert das Ausbruchsmaß, SMA(20) filtert Einstiege, und die entgegengesetzte Deltaschwelle beendet eine offene Position.

![schema](schema.svg)

## Strategieübersicht

- Jede abgeschlossene Kerze wird in Open, Close und TotalVolume zerlegt.
- Steigende und unveränderte Kerzen liefern positives Volumen, fallende Kerzen negatives Volumen.
- Sum(100) aggregiert die letzten hundert gerichteten Volumenwerte, während SMA(20) den Schlusskursen folgt; beide Indikatoren geben nur gebildete Werte aus.
- Positionsprüfungen erlauben einen Einstieg nur ohne Position und leiten ein entgegengesetztes Deltaereignis an den passenden reduzierenden Ausstieg weiter.
- Alle vier Aktionen sind Marktoperationen über eine Einheit, und Strategy trades sendet jede Ausführung an das Chart.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Wenn das rollierende Delta mindestens +2 beträgt, Close über SMA(20) liegt und keine Position besteht, eine Einheit zum Markt kaufen.
- **Short-Einstieg**: Wenn das rollierende Delta höchstens -2 beträgt, Close unter SMA(20) liegt und keine Position besteht, eine Einheit zum Markt verkaufen.
- **Ausstieg**: Eine Long-Position um eine Einheit reduzieren, wenn Delta -2 oder weniger erreicht, und eine Short-Position um eine Einheit reduzieren, wenn Delta +2 oder mehr erreicht. Die Ausstiegsgatter verwenden den SMA nicht. Das Diagramm besitzt weder Stop-Loss noch Take-Profit.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles Series | 00:01:00 | Abgeschlossene Ein-Minuten-Kerzen für sämtliche Berechnungen und Entscheidungen. |
| Delta Sum Length | 100 | Anzahl der gerichteten Volumenwerte im Fenster des rollierenden Sum-Indikators. |
| Delta Sum Source | Not set | Kein alternatives Indikator-Eingabefeld ist gewählt; die Formel für gerichtetes Volumen ist direkt verbunden. |
| SMA Length | 20 | Anzahl der Schlusskurse im gleitenden Durchschnitt für den Einstiegsfilter. |
| SMA Source | Not set | Kein alternatives Indikator-Eingabefeld ist gewählt; Candle close ist direkt verbunden. |
| Delta Threshold | 2 | Absoluter Deltawert: +2 für bullische und -2 für bärische Ereignisse. |
| Order Volume | 1 | Festes Marktvolumen für Einstiege und reduzierende Ausstiege. |

## Diagrammdetails

- Die Formel für gerichtetes Volumen ist positiv, wenn Close größer oder gleich Open ist; ein Doji liefert daher +TotalVolume. Nur bei Close unter Open wechselt das Vorzeichen.
- Das Delta ist eine rollierende Summe aus hundert Kerzen: Nach dem Füllen des Fensters ersetzt jeder neue Wert den ältesten.
- Positive und negative Schwelle entstehen aus einem einzigen sichtbaren Wert; eine Formel versieht den bärischen Zweig mit dem Minuszeichen.
- Der abschließende Auswertungstakt erreicht die vier AND-Gatter nach der Aktualisierung von Kerzenfeldern, Indikatoren, Vergleichen und Positionsabbild.
- Es gibt keine Wartezeit nach einer bestimmten Kerzenzahl. Gatter für keine, lange und kurze Position verhindern Aufstockungen und wählen je Kerze die gültige Aktion.
- Das Chart erhält sechs Ströme: Kerzen, rollierendes Delta, positive Schwelle, negative Schwelle, SMA(20) sowie alle Ein- und Ausstiegsausführungen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
