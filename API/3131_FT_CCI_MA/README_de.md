# FT CCI MA (StockSharp Port)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie ist ein direkter Port des MetaTrader-Experten "FT CCI MA". Sie handelt beim Schlusskurs jeder abgeschlossenen Kerze und kombiniert einen linear gewichteten gleitenden Durchschnitt (LWMA) mit Commodity Channel Index (CCI) Schwellenwerten und einem optionalen Handelssessions-Filter. Die StockSharp-Implementierung behält dieselben Parameternamen und Standardwerte bei, sodass das ursprüngliche Verhalten reproduziert werden kann, während die High-Level-API genutzt wird (Kerzen-Abonnements, Indikator-Binding, Positionsschutz).

Wichtige Design-Notizen:
- Die LWMA arbeitet mit dem gewichteten Preis `(High + Low + 2 * Close) / 4` und entspricht dem `PRICE_WEIGHTED`-Modus von MetaTrader.
- Der CCI verwendet den typischen Preis `(High + Low + Close) / 3`, wie in `PRICE_TYPICAL`.
- Alle Entscheidungen werden auf dem gerade geschlossenen Balken ausgewertet, was dem ursprünglichen EA entspricht, der auf den Beginn des nächsten Balkens wartete, bevor er auf den vorherigen reagierte.
- Der Positionsschutz repliziert den Take-Profit und Stop-Loss des EA in Pip-Einheiten.

## Handelsregeln
1. **Long-Einstiege**
   - Schlusskurs über der LWMA und CCI unter `CciLevelBuy` (Standard -100), *oder*
   - Schlusskurs unter der LWMA und CCI unter `CciLevelDown` (Standard -200).
   - Nur einsteigen, wenn die aktuelle Nettoposition flat oder short ist.
2. **Short-Einstiege**
   - Schlusskurs unter der LWMA und CCI über `CciLevelSell` (Standard 100), *oder*
   - Schlusskurs über der LWMA und CCI über `CciLevelUp` (Standard 200).
   - Nur einsteigen, wenn die aktuelle Nettoposition flat oder long ist.
3. **Zeitfilter**
   - Wenn `UseTimeFilter` aktiviert ist, prüft die Strategie die Stunde von `candle.CloseTime`.
   - Wenn die Stunde außerhalb des aktiven Fensters liegt, werden alle Positionen und Orders sofort storniert/geschlossen.
4. **Risikokontrollen**
   - `StartProtection` setzt absolute Stop-Loss- und Take-Profit-Abstände unter Verwendung der Pip-Größe aus `Security.PriceStep`.
   - Das Order-Volumen wird gesetzt, sodass das Öffnen in entgegengesetzter Richtung automatisch die vorherige Exposition schließt.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `OrderVolume` | Handelsgröße in Lots. | `1` |
| `StopLossPips` | Stop-Loss-Abstand in Pips (0 deaktiviert). | `150` |
| `TakeProfitPips` | Take-Profit-Abstand in Pips (0 deaktiviert). | `150` |
| `UseTimeFilter` | Aktiviert den Sessions-Filter. | `true` |
| `StartHour` | Sitzungs-Startzeit in Exchange-Zeit (0-23). | `10` |
| `EndHour` | Sitzungs-Endzeit in Exchange-Zeit (0-23). Wenn kleiner als die Startzeit, überspannt die Session Mitternacht. | `5` |
| `CciPeriod` | Commodity Channel Index Länge. | `14` |
| `CciLevelUp` | Aggressiver Short-Schwellenwert (+200). | `200` |
| `CciLevelDown` | Aggressiver Long-Schwellenwert (-200). | `-200` |
| `CciLevelBuy` | Weicher Long-Schwellenwert wenn Preis über der MA (-100). | `-100` |
| `CciLevelSell` | Weicher Short-Schwellenwert wenn Preis unter der MA (+100). | `100` |
| `MaPeriod` | LWMA-Länge. | `200` |
| `MaShift` | Horizontale Verschiebung der LWMA in Balken. Die aktuelle Kerze vergleicht mit dem Wert `MaShift` Balken zurück. | `0` |
| `CandleType` | Kerzendatentyp/Zeitrahmen für Berechnungen. | `1 hour time frame` |

## Implementierungsdetails
- **Pip-Berechnung** – Die Pip-Größe entspricht `Security.PriceStep`. Für 3- oder 5-stellige Forex-Symbole wird sie mit 10 multipliziert, um 0.00001 in den vom EA verwendeten 0.0001-Pip zu übersetzen.
- **Sessions-Filter** – Implementiert die zwei Szenarien aus dem MQL-Quellcode: Intraday-Fenster (`StartHour < EndHour`) und Overnight-Fenster (`StartHour > EndHour`). Wenn `StartHour == EndHour`, ist der Handel deaktiviert und entspricht der ursprünglichen Logik.
- **Indikator-Binding** – Verwendet `SubscribeCandles().Bind(...)`, sodass CCI und LWMA automatische Updates ohne manuelles Buffering erhalten. Werte werden nur zur Unterstützung des optionalen LWMA-Shifts gespeichert.
- **Order-Management** – `CancelActiveOrders()` läuft vor jeder Market-Order und spiegelt das EA-Verhalten eines sauberen Order-Buchs wider.
- **Keine Python-Version** – Nur die C#-Strategie wird bereitgestellt, wie angefordert.

## Verwendung
1. Die Strategie einem Instrument zuordnen und `CandleType` auf den gewünschten Zeitrahmen setzen.
2. Volumen und Pip-Parameter passend zum Instrument wählen (Broker-Pip-Definitionen mit der eingebauten Konvertierung abgleichen).
3. Den Sessions-Filter nach den Handelszeiten aktivieren oder deaktivieren.
4. Strategie starten; sie abonniert Kerzen, wendet die Indikatorlogik an und verwaltet Orders/Stops automatisch.
