# Diagramm der False-Breakout-Trap-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Diagramm handelt die Rückkehr in die vorherige Spanne aus zwanzig Kerzen, nachdem der Kurs eine Grenze kurz überschritten hat. Zwei richtungsbezogene Sperren begrenzen wiederholte Einstiege, während ein SMA offene Positionen unmittelbar beendet.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Ein-Minuten-Kerzen werden in High-, Low- und Close-Ströme aufgeteilt.
- Highest(20) und Lowest(20) mit jeweils einem Previous value und Shift = 1 definieren eine Spanne ohne die aktuelle Kerze.
- Ein High über dem vorherigen Hoch mit Close darunter ist ein gescheiterter Ausbruch nach oben; die gespiegelte Low-Bedingung gilt nach unten.
- Sell-Flag und Buy-Flag teilen sich einen N-values-Block für 500 abgeschlossene Kerzen; jede Seite lässt vor dem nächsten gemeinsamen Reset ein Ereignis durch.
- Markteinstiege erfolgen aus flacher Position mit festem Volumen eins, und SMA(20)-Bedingungen reduzieren die Position um dasselbe Volumen.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Low liegt unter dem Tief der vorherigen zwanzig Kerzen, Close kehrt über diese Grenze zurück und die Buy-Sperre akzeptiert das Ereignis. Nur aus flacher Position eine Einheit zum Markt kaufen.
- **Short-Einstieg**: High liegt über dem Hoch der vorherigen zwanzig Kerzen, Close kehrt unter diese Grenze zurück und die Sell-Sperre akzeptiert das Ereignis. Nur aus flacher Position eine Einheit zum Markt verkaufen.
- **Ausstieg**: Eine Long-Position um eine Einheit reduzieren, wenn Close unter SMA(20) liegt, oder eine Short-Position um eine Einheit reduzieren, wenn Close über SMA(20) liegt. Ausstiege erfolgen sofort und umgehen die Einstiegspause. Das Diagramm besitzt weder Stop-Loss noch Take-Profit.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles Series | 00:01:00 | Abgeschlossene Ein-Minuten-Kerzen für sämtliche Spannen-, Signal- und Ausstiegsberechnungen. |
| Highest Length | 20 | Anzahl der Kerzenhochs in der rollierenden oberen Grenze. |
| Highest Source | Not set | Kein alternatives Indikator-Eingabefeld ist gewählt; Candle high ist direkt verbunden. |
| Lowest Length | 20 | Anzahl der Kerzentiefs in der rollierenden unteren Grenze. |
| Lowest Source | Not set | Kein alternatives Indikator-Eingabefeld ist gewählt; Candle low ist direkt verbunden. |
| SMA Length | 20 | Anzahl der Schlusskurse im gleitenden Durchschnitt für Ausstiege. |
| SMA Source | Not set | Kein alternatives Indikator-Eingabefeld ist gewählt; Candle close ist direkt verbunden. |
| Cooldown N | 500 | Anzahl abgeschlossener Kerzen bis zur Ausgabe des gemeinsamen Pausen-Resets. |
| Entry Volume | 1 | Festes Marktvolumen für jeden Einstieg und reduzierenden Ausstieg. |

## Diagrammdetails

- Highest und Lowest erhalten numerische High- und Low-Werte, SMA erhält Close; alle drei Indikatoren geben nur gebildete Werte aus.
- Previous value verschiebt beide Spannenindikatoren um eine Aktualisierung, sodass die geprüfte Kerze ihre eigene Grenze nicht beeinflusst.
- Der abschließende Auswertungstakt erreicht beide False-Breakout-AND-Gatter erst nach der Aktualisierung von Kerzenfeldern, Indikatoren, Position und Vergleichen.
- Jedes ungefilterte False-Breakout-Ereignis aktiviert den gemeinsamen N-values-Block. Nach 500 folgenden abgeschlossenen Kerzen setzt dessen Ausgabe beide Flags zurück; bis dahin sperrt jedes Flag Wiederholungen seiner Seite.
- OpenPosition verhindert eine neue Order bei bestehender Position. Ein Ereignis kann die Pause dennoch aktivieren, weil der Pausenzweig vor der Positionsaktion liegt.
- Die beiden SMA-Ausstiege prüfen das Positionsvorzeichen und verwenden ReduceOnly-Marktaktionen mit Volumen eins. Chart erhält Kerzen, beide vorherigen Spannengrenzen, SMA und alle Ausführungen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
