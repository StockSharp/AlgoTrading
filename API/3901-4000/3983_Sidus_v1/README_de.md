# Sidus v1-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Sidus v1 ist eine Trendfolgestrategie, die zwei Sätze von Exponential Moving Averages (EMAs) mit Filtern für den Relative Strength Index (RSI) kombiniert. Der ursprüngliche MetaTrader 4-Expertenberater eröffnet eine Position, wenn ein schneller EMA von einem langsameren EMA abweicht und der RSI entweder überverkaufte oder überkaufte Bedingungen bestätigt. Dieser StockSharp-Port behält die Kernlogik bei, beschränkt den Handel auf Kerzen mit geringem Volumen und fügt asymmetrische Schutzaufträge für Long- und Short-Positionen hinzu.

## Verwendete Indikatoren
- **Schneller EMA (Kaufbein)** – misst die kurzfristige Dynamik für Long-Einstiege.
- **Langsamer EMA (Kaufbein)** – stellt den längerfristigen Trendfilter für Long-Einstiege dar.
- **Fast EMA (Sell Leg)** – misst die kurzfristige Dynamik für Short-Einstiege.
- **Slow EMA (Sell Leg)** – stellt den längerfristigen Trendfilter für Short-Einstiege dar.
- **RSI (Kaufabschnitt)** – validiert überverkaufte Bedingungen für Long-Trades.
- **RSI (Verkaufsabschnitt)** – validiert überkaufte Bedingungen für Short-Trades.

## Handelslogik
1. Abonnieren Sie die konfigurierte Kerzenserie (Standardzeitrahmen 15 Minuten).
2. Berechnen Sie alle EMA- und RSI-Indikatoren für jede fertige Kerze.
3. Signalauswertung überspringen, wenn das Kerzenvolumen den konfigurierten Grenzwert überschreitet (Standard 10).
4. **Kaufbedingung**:
   - Schneller EMA minus langsamer EMA liegt unter der Kaufschwelle.
   - Der Wert von RSI liegt unter dem Kaufschwellenwert von RSI.
   - Kein bestehendes Long-Engagement (Nettoposition muss nicht positiv sein).
5. **Verkaufszustand**:
   - Schneller EMA (Verkaufszweig) minus langsamer EMA (Verkaufszweig) liegt über dem Verkaufsschwellenwert.
   - RSI (Verkaufsabschnitt) liegt über dem Verkaufsschwellenwert RSI.
   - Es besteht kein Short-Engagement (die Nettoposition darf nicht negativ sein).
6. Wenn ein Signal ausgelöst wird, stornieren Sie alle ausstehenden Schutzaufträge, führen Sie einen Marktauftrag aus, dessen Größe die Nettoposition auf die gewünschte Seite dreht, und platzieren Sie sofort Take-Profit- und Stop-Loss-Aufträge, die auf die Positionsrichtung zugeschnitten sind.

## Risikomanagement
- Long-Trades setzen einen Take-Profit bei `entry + BuyTakeProfitPips * priceStep` und einen Stop-Loss bei `entry - BuyStopLossPips * priceStep`.
- Short-Trades setzen einen Take-Profit bei `entry - SellTakeProfitPips * priceStep` und einen Stop-Loss bei `entry + SellStopLossPips * priceStep`.
- Bei Schutzaufträgen wird die aktuelle Preisstufe des Wertpapiers wiederverwendet. Ändern Sie die Pip-Parameter, um sie an Instrumente mit unterschiedlichen Tick-Größen anzupassen.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `FastEmaLength` | Länge des schnellen EMA für Kaufsignale. | 23 |
| `SlowEmaLength` | Länge des langsamen EMA für Kaufsignale. | 62 |
| `FastEma2Length` | Länge des schnellen EMA für Verkaufssignale. | 18 |
| `SlowEma2Length` | Länge des langsamen EMA für Verkaufssignale. | 54 |
| `RsiPeriod` | RSI Zeitraum für die Kaufbestätigung. | 67 |
| `RsiPeriod2` | RSI Zeitraum für die Verkaufsbestätigung. | 97 |
| `BuyDifferenceThreshold` | Maximale Schnell-Langsam-Differenz von EMA, um Käufe zu ermöglichen. | 63 |
| `BuyRsiThreshold` | Maximales RSI-Level, um Käufe zuzulassen. | 59 |
| `SellDifferenceThreshold` | Minimale Schnell-Langsam-Differenz EMA, um Verkäufe zu ermöglichen. | -57 |
| `SellRsiThreshold` | Mindeststufe RSI, um Verkäufe zu ermöglichen. | 60 |
| `BuyTakeProfitPips` | Take-Profit-Distanz (Pips) für Long-Trades. | 95 |
| `BuyStopLossPips` | Stop-Loss-Distanz (Pips) für Long-Trades. | 100 |
| `SellTakeProfitPips` | Take-Profit-Distanz (Pips) für Short-Trades. | 17 |
| `SellStopLossPips` | Stop-Loss-Distanz (Pips) für Short-Trades. | 69 |
| `OrderVolume` | Volumen für neu eröffnete Positionen. | 0,5 |
| `MaxCandleVolume` | Maximal zulässiges Kerzenvolumen für den Handel. | 10 |
| `CandleType` | Für Berechnungen verwendeter Zeitrahmen. | 15-Minuten-Kerzen |

## Nutzungshinweise
- Stellen Sie sicher, dass das angeschlossene Wertpapier gleichzeitige Markt-, Stop- und Limit-Orders für ein ordnungsgemäßes Risikomanagement unterstützt.
- Passen Sie die Pip-Einstellungen an, um die Tick-Größe des Instruments widerzuspiegeln, wenn diese vom vom ursprünglichen Experten angenommenen MT4-Punktwert abweicht.
- Die Strategie basiert auf Nettopositionen; Es wird das gegnerische Engagement abflachen, bevor ein neuer Handel in die entgegengesetzte Richtung etabliert wird.
