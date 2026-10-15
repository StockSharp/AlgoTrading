# Ilan iMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Ilan iMA-Strategie** ist ein StockSharp-Port des MetaTrader 5 Expert Advisors `Ilan iMA.mq5`. Der Advisor kombiniert einen verschobenen gleitenden Durchschnitt als Trendfilter mit einem Martingal-Mittelungsraster. Die StockSharp-Version implementiert dieselben Ideen mit der High-Level-API: Wenn der gewichtete gleitende Durchschnitt einen Trend bestätigt, öffnet die Strategie eine Marktorder und fügt jedes Mal neue Trades hinzu, wenn der Preis um einen konfigurierbaren Schritt gegen die Position läuft. Der gesamte Korb wird geschlossen, wenn ein Gewinnziel, Trailing-Stop oder expliziter Stop-Loss erreicht wird, was das Geldmanagement-Modell des ursprünglichen EA reproduziert.

## Trading-Logik
1. Abonnieren des ausgewählten Zeitrahmens (`CandleType`) und Fütterung eines konfigurierbaren gleitenden Durchschnitts (`MaMethod`, `MaPeriod`, `PriceMode`). Ein positiver `MaShift` verschiebt den Indikator vorwärts, sodass die Strategie historische Werte auswertet, um das MT5-Verhalten nachzubilden.
2. Warten auf den Kerzenabschluss. Nur abgeschlossene Bars generieren Signale und aktualisieren die Trailing/Stop-Logik.
3. Trend durch Vergleich von vier aufeinanderfolgenden gleitenden Durchschnittswerten, verschoben um `MaShift` Bars, erkennen:
   - strikt abnehmende Werte signalisieren einen Abwärtstrend;
   - strikt zunehmende Werte signalisieren einen Aufwärtstrend.
4. Wenn kein Korb offen ist:
   - im Abwärtstrend, wenn der Schlusskurs über dem gleitenden Durchschnittswert liegt, Short mit `StartVolume` eröffnen;
   - im Aufwärtstrend, wenn der Schlusskurs unter dem gleitenden Durchschnittswert liegt, Long mit `StartVolume` eröffnen.
5. Wenn ein Korb existiert:
   - wenn der Preis sich mindestens `GridStepPips` gegen die Position bewegt, eine weitere Order öffnen, deren Größe mit `LotExponent` wächst, aber durch `LotMaximum` und die Börsenvolumenlimits begrenzt wird;
   - der durchschnittliche Einstandspreis, der niedrigste Kaufpreis und der höchste Verkaufspreis werden intern verfolgt, um das Verhalten nah an der MT5-Logik zu halten.
6. Schließbedingungen:
   - sobald der schwebende Gewinn eines Korbs mit mehr als einem Trade `ProfitMinimum` (in Kontowährung) erreicht, alle Orders in dieser Richtung schließen;
   - wenn der schwebende Gewinn `TakeProfitPips` erreicht oder der Verlust `StopLossPips` trifft, den Korb schließen;
   - der Trailing-Schutz wird nach `TrailingStopPips + TrailingStepPips` Punkten günstiger Bewegung aktiv und bewegt sich in Schritten von `TrailingStepPips`.

## Risikomanagement und Größenanpassung
- `StartVolume` repliziert den MT5-Parameter `StartLots`. Jede zusätzliche Order multipliziert die vorherige Größe mit `LotExponent` unter Einhaltung von `LotMaximum` und den Börsengrenzwerten (`Security.MinVolume`, `Security.VolumeStep`, `Security.MaxVolume`).
- `ProfitMinimum` bewahrt das "Sperrfreigabe"-Verhalten der MT5-Version: Sobald das Raster von einer Absicherung erholt hat und den angeforderten Gewinn druckt, werden alle Trades in dieser Richtung geschlossen.
- Stop-Loss- und Take-Profit-Abstände werden in Pips gemessen (`StopLossPips`, `TakeProfitPips`). Die Hilfsmethode konvertiert Pips in Exchange-Preisschritte mit `Security.PriceStep`.
- Der Trailing-Block emuliert die MT5-Implementierung: Trailing beginnt erst, wenn der Preis `TrailingStopPips + TrailingStepPips` überschreitet, und wird in diskreten Schritten aktualisiert, um vorzeitige Stop-Anpassungen zu vermeiden.

## Parameter
| Name | Typ | Standard | MT5-Entsprechung | Beschreibung |
| --- | --- | --- | --- | --- |
| `MaPeriod` | `int` | `15` | `Inp_MA_ma_period` | Periode des Trendfilter-gleitenden Durchschnitts. |
| `MaShift` | `int` | `5` | `Inp_MA_ma_shift` | Vorwärtsverschiebung der gleitenden Durchschnittslinie in Bars. |
| `MaMethod` | `MovingAverageMethod` | `Weighted` | `Inp_MA_ma_method` | Glättungsalgorithmus (SMA, EMA, SMMA, LWMA). |
| `PriceMode` | `CandlePrice` | `Weighted` | `Inp_MA_applied_price` | In den Indikator eingespeister Kerzenpreis. |
| `StartVolume` | `decimal` | `1` | `InpStartLots` | Basisordervolumen für den ersten Trade in einem Korb. |
| `GridStepPips` | `decimal` | `30` | `InpStep` | Abstand (in Pips) zwischen Mittelungseinträgen. |
| `LotExponent` | `decimal` | `1.6` | `InpLotExponent` | Auf die vorherige Ordergröße angewendeter Multiplikator. |
| `LotMaximum` | `decimal` | `15` | `InpLotMaximum` | Harte Obergrenze für ein einzelnes Ordervolumen. |
| `ProfitMinimum` | `decimal` | `15` | `InpProfitMinimum` | Mindest-schwebender Gewinn zum Schließen eines Korbs mit mehreren Trades. |
| `StopLossPips` | `decimal` | `0` | `InpStopLoss` | Stop-Loss-Abstand in Pips (0 deaktiviert den Stop). |
| `TakeProfitPips` | `decimal` | `100` | `InpTakeProfit` | Take-Profit-Abstand in Pips. |
| `TrailingStopPips` | `decimal` | `15` | `InpTrailingStop` | Gewinnschwelle, die den Trailing-Stop aktiviert. |
| `TrailingStepPips` | `decimal` | `5` | `InpTrailingStep` | Minimaler zusätzlicher Gewinn, bevor sich der Trailing-Stop wieder bewegt. |
| `CandleType` | `DataType` | 15-Minuten-Zeitrahmen | Chartperiode | Zeitrahmen für die Signalberechnung. |

## Unterschiede zum ursprünglichen EA
- StockSharp arbeitet in einer Netting-Umgebung, daher existiert nur eine Nettoposition pro Richtung. Die Strategie führt eine interne Liste von Einstiegspreisen und Volumen, um die MT5-Korbabrechnung zu emulieren.
- Börsenspezifische Volumenlimits werden beim Runden von Volumen immer respektiert, während der MT5-Code auf manuelle Prüfungen angewiesen war. Dies verhindert Orders, die vom Broker-Connector abgelehnt würden.
- Stop-Loss-, Take-Profit- und Trailing-Logik werden durch Marktausstiege statt durch Modifikation bestehender MT5-Positionen ausgedrückt. Das funktionale Verhalten bleibt dasselbe, aber das Ordermanagement wird von StockSharp übernommen.

## Verwendungshinweise
- Stellen Sie sicher, dass die Instrument-Metadaten (`PriceStep`, `StepPrice`, `MinVolume`, `VolumeStep`, `MaxVolume`) im Connector ausgefüllt sind, damit Pip-zu-Preis-Konvertierungen und Volumenrundungen korrekt funktionieren.
- Der Trailing-Block geht davon aus, dass die Pip-Größe dem Exchange-Preisschritt entspricht. Passen Sie `GridStepPips`, `StopLossPips` und `TrailingStopPips` für Instrumente mit ungewöhnlichen Tick-Größen an.
- Martingal-Raster sind von Natur aus riskant. Testen Sie die Strategie auf historischen Daten und verwenden Sie realistische Kommissions-/Slippage-Einstellungen, bevor Sie in der Produktion einsetzen.
