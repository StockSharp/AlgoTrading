# BrainTrend2 + AbsolutelyNoLagLWMA MMRec-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie rekonstruiert den MetaTrader-Experten `Exp_BrainTrend2_AbsolutelyNoLagLwma_MMRec`, indem zwei unabhängige Signalblöcke kombiniert werden: die Trendfolgemaschine BrainTrend2 und der adaptive Filter AbsolutelyNoLagLWMA. Jeder Block kann Trades gemäß seinen eigenen Berechtigungen öffnen und schließen, wodurch die Money-Management-Schalter der ursprünglichen MMRec-Vorlage nachgeahmt werden. Orders werden mit der StockSharp High-Level-API über Marktausführungen und dem konfigurierbaren Standardvolumen ausgeführt.

## Handelslogik
### BrainTrend2-Block
* Erstellt ein dynamisches Trailing-Level basierend auf einem ATR-ähnlichen gewichteten True Range.
* Die Richtung (`river`) wechselt, wenn die Kerze den Trailing-Puffer um mehr als `0.7 * ATR` durchbricht.
* Aufwärtskerzen innerhalb eines Aufwärts-Rivers lösen Long-Einstiege aus (wenn aktiviert) und schließen Short-Positionen.
* Abwärtskerzen innerhalb eines Abwärts-Rivers lösen Short-Einstiege aus (wenn aktiviert) und schließen Long-Positionen.
* Signale können durch den Parameter `Brain Signal Shift` verzögert werden, um mit älteren Bars zu arbeiten.

### AbsolutelyNoLagLWMA-Block
* Wendet einen zweistufigen linear gewichteten gleitenden Durchschnitt auf die ausgewählte Preisquelle an.
* Farben werden **aufwärts (2)**, wenn der doppelte LWMA steigt, **abwärts (0)**, wenn er fällt, und **neutral (1)** andernfalls.
* Ein Übergang zu Farbe 2 öffnet Longs und schließt optional Shorts; ein Wechsel zu Farbe 0 öffnet Shorts und schließt optional Longs.
* Signale können auch um eine benutzerdefinierte Anzahl von Bars zurückversetzt werden.

### Positionsmanagement
* Die Strategie betreibt eine einzelne Nettoposition. Wenn beide Blöcke Trades auf derselben Bar anfordern, werden Schließsignale vor neuen Einstiegen ausgeführt.
* Wenn ein Block einen Trade öffnen möchte, aber die entgegengesetzte Position offen und die entsprechende Schließberechtigung deaktiviert ist, wird der Einstieg übersprungen (spiegelt die Unmöglichkeit wider, Hedge-Positionen mit einem einzelnen Nettoportfolio zu halten).

## Parameter
| Gruppe | Name | Beschreibung |
| --- | --- | --- |
| BrainTrend2 | Brain Candle | Kerzentyp für den BrainTrend2-Indikator. |
| BrainTrend2 | Brain ATR | ATR-Periode für die internen BrainTrend2-Berechnungen. |
| BrainTrend2 | Brain Signal Shift | Anzahl der Bars zur Verzögerung von BrainTrend2-Signalen. |
| BrainTrend2 | Brain Buy / Sell | BrainTrend2 erlauben, Long/Short-Trades zu öffnen. |
| BrainTrend2 | Brain Close Buys / Close Sells | BrainTrend2-Signalen erlauben, bestehende Positionen zu schließen. |
| AbsolutelyNoLag | Abs Candle | Kerzentyp für den LWMA-Indikator. |
| AbsolutelyNoLag | Abs Length | LWMA-Periode. |
| AbsolutelyNoLag | Abs Price | Angewandter Preis für den LWMA. Entspricht dem MQL `Applied_price_`-Enum. |
| AbsolutelyNoLag | Abs Signal Shift | Anzahl der Bars zur Verzögerung von LWMA-Signalen. |
| AbsolutelyNoLag | Abs Buy / Sell | LWMA-Block erlauben, Long/Short-Trades zu öffnen. |
| AbsolutelyNoLag | Abs Close Buys / Close Sells | LWMA-Block erlauben, Positionen zu schließen. |
| AbsolutelyNoLag | Abs Shift | Fügt dem LWMA-Ausgabewert einen konstanten Preisversatz hinzu. |
| General | Order Volume | Standard-Marktorder-Volumen. |

## Hinweise
* Die ATR- und LWMA-Berechnungen folgen den ursprünglichen MQL-Implementierungen, einschließlich der triangulären ATR-Gewichtung und der umfangreichen Liste angewandter Preise.
* Spread-Informationen sind in StockSharp-Kerzen nicht verfügbar, daher verwendet der True Range nur Kerzenpreise. Dies spiegelt das Indikatorverhalten wider, wenn der Spread gleich null ist.
* Mehrere gleichzeitige Positionen mit unterschiedlichen Magic Numbers werden zu einer einzigen Nettoposition konsolidiert, was Standard in StockSharp-Strategien ist.
