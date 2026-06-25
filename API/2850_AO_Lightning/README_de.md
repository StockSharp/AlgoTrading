# AO-Blitz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

AO Blitz reproduziert den MT5-Expertenberater "AO_Lightning" mithilfe der High-Level-API von StockSharp. Das System überwacht die Steigung des Awesome Oscillators (AO), der aus Median-Preisen aufgebaut ist. Wenn der Oszillator sinkt, sammelt die Strategie Long-Exposure an, und wenn der Oszillator steigt, baut sie eine Short-Position auf. Positionen werden bis zu einer konfigurierbaren Obergrenze pyramidiert, während entgegengesetzte Positionen vor einem Richtungswechsel geschlossen werden.

## Handelslogik

1. Die gewählte Kerzenserie abonnieren und den Awesome Oscillator mit Kurzperiode 5 und Langperiode 34 berechnen (die Standardwerte aus dem ursprünglichen MQL-Code).
2. Nur auf abgeschlossene Kerzen warten; die Strategie ignoriert Zwischenupdates, um Doppelzählungen zu vermeiden.
3. Bei der ersten abgeschlossenen Kerze wird der AO-Wert als Referenz gespeichert.
4. Wenn der aktuelle AO-Wert **niedriger** als der vorherige Wert ist:
   - Wenn eine offene Short-Position vorhanden ist, eine Market-Buy-Order senden, die darauf ausgelegt ist, den gesamten Short zu schließen und sofort eine Long-Schicht hinzuzufügen.
   - Wenn kein Short vorhanden ist und das Long-Exposure unter dem Limit liegt, eine zusätzliche Schicht kaufen.
5. Wenn der aktuelle AO-Wert **höher** als der vorherige Wert ist:
   - Wenn eine offene Long-Position vorhanden ist, eine Market-Sell-Order senden, die das Long-Exposure schließt und gleichzeitig eine Short-Schicht eröffnet.
   - Wenn kein Long vorhanden ist und das Short-Exposure unter dem Limit liegt, eine zusätzliche Schicht verkaufen.
6. AO-Werte, die dem vorherigen Wert entsprechen, lassen die Position unverändert.
7. Das integrierte `StartProtection()` wird einmalig beim Start aktiviert, damit Designer-Benutzer Stops oder andere Risikomodule anschließen können.

Die Logik spiegelt den ursprünglichen Expertenberater wider: Die AO-Steigung definiert die Handelsrichtung, entgegengesetzte Trades werden vor einem neuen Einstieg abgeflacht, und inkrementelle Orders akkumulieren sich bis zum Erreichen der Obergrenze.

## Positionsmanagement

- **Handelsvolumen** definiert die Größe jeder zusätzlichen Schicht und entspricht dem MT5-Parameter `LotFixed`.
- **Max. Positionen** entspricht dem MT5-`Orders`-Input. Es begrenzt, wie viele Schichten auf beiden Seiten angehäuft werden können.
- **Pyramidisierung** ist linear: Jedes gültige Signal fügt genau eine lot-große Schicht hinzu, sofern die Obergrenze nicht erreicht wurde.
- **Abflachung** sendet kombinierte Orders (Schließen + neue Richtung), um Zwischenzustände ohne Position beim Wechsel von Short zu Long oder umgekehrt zu vermeiden.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `TradeVolume` | Ordergröße für jede neue Schicht. | 1 |
| `MaxPositions` | Maximale Anzahl von Long- oder Short-Schichten, die gleichzeitig aktiv sein können. | 10 |
| `AoShortPeriod` | Schneller SMA-Zeitraum des Awesome Oscillators (Median-Preis-SMA). | 5 |
| `AoLongPeriod` | Langsamer SMA-Zeitraum des Awesome Oscillators. | 34 |
| `CandleType` | Von der Strategie verarbeitete Kerzendatenquelle. | 5-Minuten-Zeitrahmen |

## Hinweise

- Der ursprüngliche MT5-Experte benennt die Eingaben `Period_sma_slow` und `Period_sma_fast`, tauscht jedoch die Werte (5 und 34). Der StockSharp-Port behält das funktionale Mapping bei, indem er intuitive `AoShortPeriod`/`AoLongPeriod`-Parameter bereitstellt.
- Es gibt noch keine Python-Version, entsprechend der Aufgabenanforderung.
- Keine Tests enthalten; führen Sie notwendige Validierungen über Designer oder Ihr eigenes Backtesting-Framework durch, bevor Sie in die Produktion deployen.
