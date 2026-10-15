# Roboter ADX + 2 MA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Robot ADX + 2 MA-Strategie ist eine StockSharp-Portierung des MetaTrader-Experten `Robot_ADX+2MA`. Das System kombiniert ein schnelles und ein langsames
exponentieller gleitender Durchschnitt mit den +DI/-DI-Komponenten des Average Directional Index (ADX). Bestellungen werden erst geöffnet, wenn die
Die vorherige Kerze zeigt einen ausreichend großen Abstand von EMA und die aktuelle Kerze bestätigt die Dynamik durch den Richtungsindex. Die
Bei der Konvertierung bleibt das ursprüngliche Verhalten erhalten, bei dem jeweils höchstens eine Marktposition eröffnet und Exits an Stop-Loss- und Stop-Loss-Positionen delegiert werden
Take-Profit-Schutz.

## Handelslogik
1. Abonnieren Sie die über `CandleType` konfigurierte primäre Kerzenserie und verarbeiten Sie nur fertige Kerzen.
2. Füttere zwei exponentielle gleitende Durchschnitte (Perioden 5 und 12) mit den Schlusskursen der Kerze. Ihre Werte von der vorherigen Kerze
emulieren den in MetaTrader verwendeten `shift = 1`-Lookback.
3. Füttern Sie einen `AverageDirectionalIndex`-Indikator (Periode 6) mit denselben Kerzen. Speichern Sie sowohl den aktuellen als auch den vorherigen +DI/-DI
Messwerte zur Replikation der EA-Filter.
4. Berechnen Sie den absoluten EMA-Abstand von der vorherigen Kerze und vergleichen Sie ihn mit `DifferenceThreshold`, umgerechnet aus Punkten in
Preiseinheiten (`Point` in MetaTrader entspricht `Security.PriceStep` in StockSharp).
5. **Bulsischer Einstieg**: nur zulässig, wenn keine Position offen ist und die folgenden Bedingungen erfüllt sind:
   - Der vorherige schnelle EMA liegt unter dem vorherigen langsamen EMA.
   - Der vorherige +DI liegt unter 5, der aktuelle +DI liegt über 10 und +DI ist stärker als -DI.
   - Die Entfernung EMA liegt über dem konfigurierten Schwellenwert.
6. **Bearischer Einstieg**: symmetrisch zu den langen Regeln, erfordert, dass das vorherige schnelle EMA über dem langsamen EMA liegt, die -DI-Filter
zufrieden und -DI, um +DI zu dominieren.
7. Wenn ein Trade eröffnet wird, verlassen Sie sich darauf, dass das von `StartProtection` gestartete Risikomodul über Take-Profit oder Stop-Loss aussteigt. Kein Handbuch
Exit-Regeln werden hinzugefügt, die dem ursprünglichen Experten entsprechen.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Zeitrahmen von 1 Minute | Von der Strategie verarbeitete primäre Kerzenserie. |
| `TakeProfitPoints` | `int` | `4700` | Abstand des Take-Profit-Ziels, ausgedrückt in Preisschritten. Zum Deaktivieren auf Null setzen. |
| `StopLossPoints` | `int` | `2400` | Abstand des Stop-Loss-Ziels in Preisschritten. Zum Deaktivieren auf Null setzen. |
| `TradeVolume` | `decimal` | `0.1` | Nettovolumen, das für jede Marktorder verwendet wird. |
| `DifferenceThreshold` | `int` | `10` | Mindestentfernung EMA (in Preisschritten), die erforderlich ist, bevor ein Signal akzeptiert wird. |

## Risikomanagement
- Die StockSharp-Version ruft `StartProtection` mit `UnitTypes.Step` auf, also sind die konfigurierten Stop-Loss- und Take-Profit-Abstände
automatisch auf die Preisstufe des Brokers umgerechnet.
- Schutzaufträge werden als Marktaustritte (`useMarketOrders = true`) generiert und reproduzieren das unmittelbare Schlussverhalten des
MQL Hilfsfunktion.

## Details zur Implementierung
- Indikatorbindungen verwenden das übergeordnete `SubscribeCandles(...).Bind(...).BindEx(...)` API, sodass keine manuellen Datenschleifen erforderlich sind.
- EMA-Werte der vorherigen Kerze werden zwischengespeichert, um die `iMA(..., shift = 1)`-Aufrufe im ursprünglichen EA zu reproduzieren.
- ADX-Daten werden über `AverageDirectionalIndexValue` verbraucht und ermöglichen so den direkten Zugriff auf die +DI- und -DI-Komponenten ohne Aufruf
verbotene `GetValue` Helfer.
- Ein Schutz pro Kerze (`_lastProcessedTime`) stellt sicher, dass Signale nur einmal ausgewertet werden, auch wenn sowohl die Bindungen EMA als auch ADX ausgelöst werden
Rückrufe für dieselbe Kerze.

## Unterschiede zum MetaTrader-Experten
- Der redundante direkte `OrderSend`-Aufruf im Verkaufszweig des MQL-Codes wurde entfernt; Beide Richtungen verwenden ein einziges
`BuyMarket`/`SellMarket` Helfer.
- MetaTrader prüft die freie Marge, bevor Orders gesendet werden. Der StockSharp-Port delegiert Risikokontrollen an die Hosting-Umgebung und
setzt ausreichendes Gleichgewicht voraus.
- Die Schutzlogik wird über den Risikomanager von StockSharp anstelle von benutzerdefinierten Schleifen implementiert, die `OrderSend` wiederholt aufrufen.

## Anwendungstipps
- Passen Sie `TradeVolume` an, um den Losschritt des ausgewählten Wertpapiers zu berücksichtigen, bevor Sie mit dem Live-Handel beginnen.
- Wenn der Markt eine andere Preisskala verwendet, passen Sie `DifferenceThreshold` zusammen mit den Stopp-/Zielentfernungen an, sodass die EMA
Die Trennung ist vergleichbar mit der MetaTrader-Konfiguration.
- Der Standardzeitrahmen beträgt eine Minute, aber der Parameter `CandleType` ermöglicht den Wechsel zu jeder anderen von den Daten unterstützten Serie
Quelle.

## Indikatoren
- `ExponentialMovingAverage(5)` berechnet auf Grundlage der Schlusskurse.
- `ExponentialMovingAverage(12)` berechnet auf Grundlage der Schlusskurse.
- `AverageDirectionalIndex(6)` bietet +DI/-DI- und ADX-Stärkefilter.
