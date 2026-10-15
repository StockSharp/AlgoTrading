# Test-MACD-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Test-MACD-Strategie** ist eine getreue Konvertierung des MetaTrader-Expert-Advisors `TestMACD` in die StockSharp-High-Level-API. Sie nutzt den Moving Average Convergence Divergence (MACD)-Indikator, um Momentum-Wechsel zu erkennen, und führt Trades aus, wenn die MACD-Linie auf geschlossenen Kerzen die Signallinie kreuzt. Die Strategie arbeitet auf einem einzelnen Instrument und Zeitrahmen, der über den Parameter `CandleType` geliefert wird.

## Handelslogik
1. Kerzendaten gemäß `CandleType` abonnieren und einen MACD-Indikator mit konfigurierbaren schnellen, langsamen und Signalperioden berechnen.
2. Die MACD-Wertdifferenz (`MACD - Signal`) auf jeder abgeschlossenen Kerze überwachen.
3. Einen **bullischen Einstieg** auslösen, wenn die Differenz von nicht positiv zu positiv wechselt, was bedeutet, dass die MACD-Linie die Signallinie nach oben gekreuzt hat. Jede Short-Exposure wird vor Eröffnung der Long-Position geschlossen.
4. Einen **bärischen Einstieg** auslösen, wenn die Differenz von nicht negativ zu negativ wechselt, was bedeutet, dass die MACD-Linie die Signallinie nach unten gekreuzt hat. Jede Long-Exposure wird vor Eröffnung der Short-Position geschlossen.
5. Alle Orders werden zum Markt mit dem durch `TradeVolume` konfigurierten festen Volumen gesendet.
6. Jeder Einstieg wird automatisch mit Stop-Loss- und Take-Profit-Niveaus in Preisschritten geschützt, um das punktbasierte Risikomanagement des ursprünglichen Experten nachzubilden.

## Risikomanagement
- Stop-Loss- und Take-Profit-Distanzen spiegeln die MetaTrader-Eingaben und werden in Preisschritten angegeben. Fehlt der Security `PriceStep`, nutzt die Strategie absolute Preisdistanzen mit `MinPriceStep` oder `1` als Multiplikator.
- Schutzorders werden einmal beim Start der Strategie über `StartProtection` erstellt, sodass sie ohne Neukonfiguration für jeden späteren Trade gelten.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `FastPeriod` | Schnelle EMA-Länge für MACD-Berechnungen. | `12` |
| `SlowPeriod` | Langsame EMA-Länge für MACD-Berechnungen. | `24` |
| `SignalPeriod` | Signal-EMA-Länge für MACD-Glättung. | `9` |
| `StopLossPoints` | Stop-Loss-Distanz in Preisschritten. | `90` |
| `TakeProfitPoints` | Take-Profit-Distanz in Preisschritten. | `110` |
| `TradeVolume` | Festes Volumen für alle Marktorders. | `1` |
| `CandleType` | Von der Strategie abonnierter Kerzendatentyp und Zeitrahmen. | `30-Minuten-Zeitrahmen` |

## Nutzungshinweise
- Binden Sie die Strategie vor dem Start an eine Security, damit `PriceStep` und `MinPriceStep` verfügbar sind.
- Stellen Sie sicher, dass Marktdaten für den gewählten `CandleType` bereitgestellt werden; andernfalls bildet sich der MACD-Indikator nicht und es findet kein Handel statt.
- Die Strategie protokolliert jedes Crossover-Ereignis, wodurch Handelsentscheidungen in Backtests leicht nachvollziehbar sind.

## Konvertierungsdetails
- Die ursprünglichen MetaTrader-Klassen `CSignalMACD`, `CTrailingNone` und `CMoneyFixedLot` werden durch StockSharp-Indikatorbindung und `StartProtection`-Mechanismen ersetzt.
- Die Logik aus `ExtStateMACD`, die MACD-Kreuzungen prüfte, wird durch einen Vorzeichenwechsel-Detektor auf der MACD-Differenz zwischen aufeinanderfolgenden abgeschlossenen Kerzen dargestellt.
- Geldmanagement wird auf einen festen Volumenparameter vereinfacht und ähnelt stark dem Fixed-Lot-Verhalten von `CMoneyFixedLot`, wenn prozentbasiertes Sizing deaktiviert ist.
