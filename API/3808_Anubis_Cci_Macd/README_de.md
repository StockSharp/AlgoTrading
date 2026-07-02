# Anubis CCI MACD Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung
- Wandelt den MetaTrader 4 Expertenberater „Anubis“ in den StockSharp hohen Level API um.
- Verwendet einen 4-stündigen Commodity Channel Index (CCI)-Filter zusammen mit einem 15-minütigen MACD-Crossover.
- Wendet adaptive Positionsgröße, Stop-Loss, Breakeven-Schutz, ATR-gesteuerte Exits und einen auf der Standardabweichung basierenden Take-Profit an.

## Strategielogik
1. **Daten**
   - Primärer Zeitrahmen: 15-Minuten-Kerzen (`SignalCandleType`), verwendet für MACD- und ATR-Berechnungen.
   - Höherer Zeitrahmen: 4-Stunden-Kerzen (`TrendCandleType`), verwendet für CCI-Filterung und Standardabweichungsmessung.
2. **Indikatoren**
   - `CommodityChannelIndex` mit konfigurierbarem Zeitraum bei der 4H-Serie.
   - `StandardDeviation` (Länge 30) bei 4H schließt, um die Take-Profit-Distanz abzuschätzen.
   - `MovingAverageConvergenceDivergenceSignal` (schnell/langsam/Signal konfigurierbar) bei 15 Millionen Kerzen.
   - `AverageTrueRange` (Länge 12) auf 15 Mio. Kerzen für volatilitätsbasierte Exits.
3. **Einträge**
   - **Short**: Wenn 4H CCI über `CciThreshold` liegt, zeigen die beiden vorherigen MACD-Werte einen bearischen Crossover (MACD kreuzt sein Signal), MACD war positiv, es gibt keine offenen Long-Positionen und der Preis hat sich seit dem letzten Short-Einstieg um mindestens `PriceFilterPoints` bewegt.
   - **Long**: symmetrische Bedingung mit CCI unter `-CciThreshold`, MACD nach oben kreuzend, während negativ, keine offenen Kurzschlüsse und der Mindestabstandsfilter erfüllt.
4. **Risikomanagement**
   - Das Basisvolumen wird durch `VolumeValue` definiert und wird durch das Kontokapital (2× über 14.000, 3,2× über 22.000) und durch `LossFactor` nach einem Verlusthandel skaliert.
   - Die maximale Anzahl gleichzeitiger Trades pro Richtung ist durch `MaxLongTrades` und `MaxShortTrades` begrenzt.
   - Harter Stop-Loss platziert praktisch bei `StopLossPoints * PriceStep` vom durchschnittlichen Einstiegspreis.
   - Breakeven wird aktiviert, sobald der Preis um `BreakevenPoints` steigt, und die Position wird sofort geschlossen, wenn der Preis zum Einstiegsniveau zurückkehrt.
5. **Ausgänge**
   - Der Take-Profit der Standardabweichung schließt die Position, sobald sich der Preis um `StdDevMultiplier * StdDev` zu seinen Gunsten bewegt.
   - Aggressive Exits werden ausgelöst, wenn der vorherige Kerzenbereich `CloseAtrMultiplier * ATR` überschreitet.
   - MACD-Verlangsamungsausgänge erfordern sowohl einen ausreichenden Gewinn (`ProfitThresholdPoints`) als auch eine Umkehr der MACD-Steigung (vorheriges MACD vor weniger als oder mehr als zwei Balken, je nach Richtung).
   - Der Schutzstopp schließt den Handel, wenn der Preis die Stop-Loss-Distanz durchbricht oder nach der Breakeven-Aktivierung auf den Einstiegspunkt zurückfällt.

## Parameter
| Name | Beschreibung |
| ---- | ----------- |
| `VolumeValue` | Grundauftragsvolumen. |
| `CciThreshold` | Absoluter Schwellenwert für den 4H CCI-Filter. |
| `CciPeriod` | Zeitraum des 4H CCI-Indikators. |
| `StopLossPoints` | Stop-Loss-Distanz in Punkten. |
| `BreakevenPoints` | Profitieren Sie von den erforderlichen Punkten, um die Gewinnschwelle zu erreichen. |
| `MacdFastPeriod` | Schneller Zeitraum von EMA für MACD. |
| `MacdSlowPeriod` | Langsamer Zeitraum von EMA für MACD. |
| `MacdSignalPeriod` | Signalisieren Sie einen Zeitraum von EMA für MACD. |
| `LossFactor` | Der Volumenmultiplikator wird nach einem Verlusthandel angewendet. |
| `MaxShortTrades` | Maximale Anzahl gleichzeitiger kurzer Einträge. |
| `MaxLongTrades` | Maximale Anzahl gleichzeitiger langer Einträge. |
| `CloseAtrMultiplier` | ATR-Multiplikator für vorzeitige Ausstiege. |
| `ProfitThresholdPoints` | Zusätzlicher Gewinnpuffer (Punkte), bevor MACD beendet wird. |
| `StdDevMultiplier` | Standardabweichungsmultiplikator für den Take-Profit. |
| `PriceFilterPoints` | Minimale Preisbewegung zwischen aufeinanderfolgenden Einträgen. |
| `SignalCandleType` | Primärer Zeitrahmen für MACD und ATR. |
| `TrendCandleType` | Längerer Zeitrahmen für CCI und Standardabweichung. |

## Notizen
- Die Strategie basiert auf gültigen `Security.PriceStep`-Metadaten, um punktbasierte Parameter in Preisentfernungen zu übersetzen.
- Die Schutzlogik wird über explizite Prüfungen anstelle ausstehender Stop-/Limit-Orders implementiert und spiegelt das ursprüngliche EA-Verhalten mit virtuellen Stops wider.
- Die Python-Version wird gemäß den Aufgabenanweisungen absichtlich weggelassen.
