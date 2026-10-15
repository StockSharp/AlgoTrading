# FiveMinuteRsiCci-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

`FiveMinuteRsiCciStrategy` ist ein StockSharp-Port des MetaTrader 4-Expertenberaters **5Mins Rsi Cci EA.mq4**. Das ursprüngliche Skript handelt Fünf-Minuten-Kerzen, indem es einen RSI-Schwellenwertdurchschnitt mit einem geglätteten/EMA-Gleitenden-Durchschnitts-Filter und der Polarität von zwei CCI-Indikatoren kombiniert. Die C#-Version behält die gleiche Entscheidungslogik bei, während sie das übergeordnete API von StockSharp für Datenabonnements, Indikatorbindung und Risikomanagement verwendet.

## Handelslogik

1. Abonnieren Sie den konfigurierten Kerzentyp (standardmäßig fünfminütiger Zeitrahmen) und aktualisieren Sie fünf Indikatoren in Echtzeit: RSI, ein geglätteter MA des Eröffnungspreises, ein EMA des Eröffnungspreises sowie schnelle und langsame CCIs, die aus typischen Preisen berechnet werden.
2. Jede fertige Kerze wird nur dann ausgewertet, wenn keine Position offen ist und die aktuelle Geld-/Briefspanne unter `MaxSpreadPoints` liegt (umgerechnet in Preiseinheiten).
3. Ein langes Signal erfordert:
   - der geglättete MA über dem EMA,
   - die RSI-Kreuzung nach oben durch `BullishRsiLevel` zwischen der vorherigen und der aktuellen Kerze,
   - beide CCI-Werte über Null.
4. Ein kurzes Signal erfordert die umgekehrten Bedingungen (geglätteter MA unter EMA, RSI kreuzt nach unten durch `BearishRsiLevel`, beide CCIs unter Null).
5. Das Ordervolumen reproduziert die dynamische Positionsgröße des EA: `LotCoefficient × sqrt(Equity / EquityDivisor)` wird auf den Volumenschritt des Instruments gerundet und durch `VolumeMin`/`VolumeMax` eingeschränkt.
6. Die Schutzlogik wird von `StartProtection` verwaltet, das Stop-Loss-, Take-Profit- und Trailing-Stop-Distanzen hinzufügt, die aus MetaTrader Punkten in absolute Preisversätze umgewandelt werden.

## Parameter

| Parameter | Standard | Beschreibung |
| --- | --- | --- |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` | Zeitrahmen für Indikatoraktualisierungen und Signalauswertung. |
| `RsiPeriod` | `14` | Anzahl der Kerzen, die in der RSI-Berechnung verwendet werden. |
| `FastSmmaPeriod` | `2` | Zeitraum des schnell geglätteten gleitenden Durchschnitts, der auf die Eröffnungspreise angewendet wird. |
| `SlowEmaPeriod` | `6` | Zeitraum der langsamen EMA, angewendet auf die Eröffnungspreise. |
| `FastCciPeriod` | `34` | Zeitraum des Fastens CCI, berechnet aus dem typischen Preis `(H+L+C)/3`. |
| `SlowCciPeriod` | `175` | Zeitraum des langsamen CCI, berechnet aus dem typischen Preis. |
| `BullishRsiLevel` | `55` | RSI Schwellenwert, der nach oben überschritten werden muss, um einen langen Eintrag zu aktivieren. |
| `BearishRsiLevel` | `45` | RSI Schwellenwert, der nach unten überschritten werden muss, um einen kurzen Eintrag zu aktivieren. |
| `StopLossPoints` | `60` | Stop-Loss-Distanz in MetaTrader Punkten (umgerechnet in absoluten Preis). Zum Deaktivieren auf `0` setzen. |
| `TakeProfitPoints` | `0` | Take-Profit-Distanz in MetaTrader Punkten. Zero behält das ursprüngliche EA-Verhalten bei (kein TP). |
| `TrailingStopPoints` | `20` | Trailing-Stop-Distanz in MetaTrader Punkten. Null deaktiviert das Nachziehen. |
| `LotCoefficient` | `0.01` | Basiskoeffizient, der in der dynamischen Positionsgrößenformel verwendet wird. |
| `EquityDivisor` | `10` | Teiler innerhalb der Quadratwurzel für die auf Eigenkapital basierende Dimensionierung (`sqrt(Equity / EquityDivisor)`). |
| `MaxSpreadPoints` | `18` | Maximal zulässiger Spread (in MetaTrader Punkten). Aufträge werden übersprungen, bis sich der Spread verringert. |

## Notizen

- Der Spread-Filter basiert auf Daten der Ebene 1; Wenn die besten Geld-/Briefkurse nicht verfügbar sind, wartet die Strategie, bevor neue Positionen eröffnet werden.
- Die Punkt-zu-Preis-Umrechnung wird automatisch um `PriceStep` und die Instrumentengenauigkeit skaliert (5/3-Dezimalinstrumente multiplizieren den Schritt mit 10), um den `Point`-Wert von MetaTrader widerzuspiegeln.
- Stops und Trailing werden über die integrierte Schutz-Engine von StockSharp mit Marktausstiegen verwaltet, was der Verwendung von Marktaufträgen für Trailing-Stop-Updates durch EA entspricht.
