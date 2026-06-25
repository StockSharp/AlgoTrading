# GreenTrade Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die GreenTrade-Strategie ist eine Konvertierung des ursprünglichen MQL5-Experten. Sie folgt mittelfristigen Trends durch Kombination eines geglätteten gleitenden Durchschnitt (SMMA)-Steigungsfilters mit Momentum-Bestätigung vom Relative Strength Index (RSI). Signale werden auf abgeschlossenen Kerzen des konfigurierten Zeitrahmens berechnet, und die Strategie kann bis zu einer konfigurierbaren Anzahl von Positionseinheiten pyramidisieren, während sie feste Risikokontrollen und einen schrittweisen Trailing-Stop anwendet.

## Handelslogik
1. **Indikatorvorbereitung**
   - Die SMMA wird auf dem Median-Preis `((High + Low) / 2)` mit dem `MaPeriod`-Parameter berechnet.
   - Der RSI wird auf dem Schlusskurs mit dem `RsiPeriod`-Rückblick berechnet.
2. **Trendformfilter**
   - Vier historische SMMA-Proben werden gemäß den Bar-Shift-Parametern (`ShiftBar`, `ShiftBar1`, `ShiftBar2`, `ShiftBar3`) untersucht.
   - Ein bullischer Trend erfordert `SMMA(shift0) > SMMA(shift1) > SMMA(shift2) > SMMA(shift3)`.
   - Ein bärischer Trend erfordert `SMMA(shift0) < SMMA(shift1) < SMMA(shift2) < SMMA(shift3)`.
3. **Momentum-Bestätigung**
   - Der RSI muss über `RsiBuyLevel` für Long-Einstiege und unter `RsiSellLevel` für Short-Einstiege liegen. Der RSI-Wert wird `ShiftBar` Bars zurück genommen, um die MQL5-Logik zu spiegeln, die die sich bildende Kerze ignoriert.
4. **Orderausführung**
   - Wenn ein Signal bestätigt ist und das Positionslimit nicht überschritten wird, sendet die Strategie eine Marktorder für `TradeVolume`.
   - Wenn eine Position in der entgegengesetzten Richtung existiert, neutralisiert die Strategie sie zuerst und eröffnet dann eine neue Position mit dem konfigurierten Volumen.
   - Wenn eine Position in dieselbe Richtung existiert, wird das Handelsvolumen bis zu `MaxPositions * TradeVolume` zur Nettoexposition hinzugefügt.

## Risikomanagement
- **Initialer Stop-Loss / Take-Profit**: Jeder neue Einstieg setzt Preisziele basierend auf `StopLossPips` und `TakeProfitPips`. Pip-Abstände werden in Preiseinheiten über den `PriceStep` des Instruments umgerechnet. Instrumente mit fraktionalen Schritten (z. B. fünfstellige Forex-Symbole) erhalten einen zusätzlichen Faktor von 10, genau wie der ursprüngliche Experte.
- **Trailing-Stop**: Wenn der Gewinn `TrailingStopPips + TrailingStepPips` überschreitet, wird der Stop bewegt, um einen Abstand von `TrailingStopPips` zu halten. Weitere Bewegungen erfordern eine weitere `TrailingStepPips`-Preisverbesserung, was das schrittweise Trailing-Verhalten aus dem MQL-Code reproduziert.
- **Positionslimit**: Der `MaxPositions`-Parameter begrenzt die maximale Anzahl von Volumeneinheiten. Signale, die dieses Limit überschreiten würden, werden ignoriert.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|--------------|---------|
| `MaPeriod` | Länge des geglätteten gleitenden Durchschnitts angewendet auf den Median-Preis. | 67 |
| `ShiftBar`, `ShiftBar1`, `ShiftBar2`, `ShiftBar3` | Offsets (in Bars) zum Zugriff auf historische SMMA-Proben für den Trendformfilter. | 1, 1, 2, 3 |
| `RsiPeriod` | Rückblickperiode für den RSI-Indikator. | 57 |
| `RsiBuyLevel` | RSI-Schwellenwert, der bullische Setups bestätigt. | 60 |
| `RsiSellLevel` | RSI-Schwellenwert, der bärische Setups bestätigt. | 36 |
| `TradeVolume` | Volumen angewendet auf jeden Einstieg oder jede Ergänzung. | 0.1 |
| `StopLossPips` | Abstand für den initialen Stop-Loss in Pips (0 deaktiviert ihn). | 300 |
| `TakeProfitPips` | Abstand für den initialen Take-Profit in Pips (0 deaktiviert ihn). | 300 |
| `TrailingStopPips` | Abstand zwischen Preis und Trailing-Stop nach Aktivierung (0 deaktiviert Trailing). | 12 |
| `TrailingStepPips` | Zusätzlicher Fortschritt erforderlich, bevor der Trailing-Stop erneut bewegt wird. | 5 |
| `MaxPositions` | Maximale Anzahl von Volumeneinheiten (`TradeVolume`-Vielfachen), die aktiv sein können. | 7 |
| `CandleType` | Kerzendatenserie für Indikatoraktualisierungen. | 1-Stunden-Zeitrahmen |

## Hinweise
- Alle Berechnungen werden nur auf abgeschlossenen Kerzen durchgeführt; unfertige Kerzen werden ignoriert, um rauschige Signale zu vermeiden.
- Der Positionsstatus wird intern verfolgt, damit Stop-Loss-, Take-Profit- und Trailing-Ausstiege gehandhabt werden, auch wenn keine Schutzorders an der Börse platziert sind.
- Die Konvertierung behält das ursprüngliche Verhalten für Pip-Konvertierung und Trailing-Schritt-Logik bei, während die StockSharp-High-Level-API für Abonnements und Orderausführung genutzt wird.
