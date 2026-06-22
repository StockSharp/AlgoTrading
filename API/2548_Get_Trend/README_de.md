# Get-Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Diese Strategie ist ein StockSharp-Port des MetaTrader-Expertenberaters **"Get trend"**, der ursprünglich für den M15-Handel mit einem H1-Bestätigungsfilter konzipiert wurde. Der Algorithmus kombiniert geglättete gleitende Durchschnitte und einen stochastischen Oszillator, um Mean-Reversion-Einstiege zu timen, die mit einem übergeordneten Trend übereinstimmen.

## Handelslogik

- **Primärer Zeitrahmen:** 15-Minuten-Kerzen werden für die Signalgenerierung und Orderausführung verwendet.
- **Bestätigungs-Zeitrahmen:** Stündliche Kerzen liefern den übergeordneten geglätteten gleitenden Durchschnitt und den Schlusskurs zur Validierung des vorherrschenden Trends.
- **Trendfilter:** Sowohl der M15- als auch der H1-Schluss müssen auf derselben Seite ihrer jeweiligen geglätteten gleitenden Durchschnitte liegen. Zusätzlich muss der M15-Schluss innerhalb eines konfigurierbaren Abstands von seinem gleitenden Durchschnitt bleiben, um einen Pullback-Einstieg zu gewährleisten.
- **Momentum-Auslöser:** Long-Trades erfordern, dass die stochastische %K-Linie %D im überverkauften Bereich (unter 20) nach oben kreuzt. Short-Trades erfordern die umgekehrte Kreuzung im überkauften Bereich (über 80).
- **Ordersteuerung:** Positionen werden mit festen Stop-Loss- und Take-Profit-Niveaus in Preispunkten geschützt. Ein optionaler Trailing-Stop zieht den Ausstieg enger, sobald sich der Preis weit genug zugunsten des Trades bewegt.

## Einstiegsbedingungen

### Long-Setup
1. M15-Schluss liegt unter dem geglätteten gleitenden Durchschnitt von M15.
2. H1-Schluss liegt unter dem geglätteten gleitenden Durchschnitt von H1.
3. Der Abstand zwischen M15-Schluss und M15-Durchschnitt überschreitet den **Price Threshold** nicht (in Punkten/Ticks).
4. Stochastik %K und %D liegen beide unter 20.
5. Der vorherige %K-Wert lag unter %D, und der aktuelle %K-Wert kreuzte %D nach oben.
6. Keine bestehende Long-Position (eine Short-Position wird geschlossen und umgekehrt).

### Short-Setup
1. M15-Schluss liegt über dem geglätteten gleitenden Durchschnitt von M15.
2. H1-Schluss liegt über dem geglätteten gleitenden Durchschnitt von H1.
3. Der Abstand zwischen M15-Schluss und M15-Durchschnitt überschreitet den **Price Threshold** nicht.
4. Stochastik %K und %D liegen beide über 80.
5. Der vorherige %K-Wert lag über %D, und der aktuelle %K-Wert kreuzte %D nach unten.
6. Keine bestehende Short-Position (eine Long-Position wird geschlossen und umgekehrt).

## Ausstiegsregeln

- **Stop-Loss:** In absoluten Preispunkten vom Einstiegspreis festgelegt.
- **Take-Profit:** In absoluten Preispunkten vom Einstiegspreis festgelegt.
- **Trailing-Stop:** Wenn aktiviert, wird der Stop enger gezogen, sobald sich der Preis über die Trailing-Distanz hinaus bewegt, um Gewinne zu sichern und dabei den konfigurierten Trailing-Offset einzuhalten.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|--------------|----------|
| `M15CandleType` | Kerzentyp für die Signalgenerierung. | 15-Minuten-Zeitrahmen |
| `H1CandleType` | Kerzentyp für die Bestätigung. | 1-Stunden-Zeitrahmen |
| `MaM15Length` | Länge des geglätteten MA auf M15-Kerzen. | 99 |
| `MaH1Length` | Länge des geglätteten MA auf H1-Kerzen. | 184 |
| `StochasticLength` | %K-Periode des stochastischen Oszillators. | 27 |
| `StochasticSignalLength` | %D-Glättungsperiode. | 3 |
| `ThresholdPoints` | Maximaler Abstand (in Punkten) zwischen Preis und M15-MA für Einstiege. | 10 |
| `TakeProfitPoints` | Take-Profit-Distanz (in Punkten). | 540 |
| `StopLossPoints` | Stop-Loss-Distanz (in Punkten). | 90 |
| `TrailingStopPoints` | Trailing-Stop-Distanz (in Punkten). | 20 |
| `TradeVolume` | Basisordervolumen beim Öffnen neuer Trades. | 0.1 |

Alle punktbasierten Parameter werden mit dem `PriceStep` des Instruments multipliziert, um sie in absolute Preisinkremente umzurechnen.

## Implementierungshinweise

- Die Strategie verwendet StockSharp's High-Level-API mit Kerzenabonnements und Indikatorenbindung (`BindEx`), um manuelle Bufferverwaltung zu vermeiden.
- Die Trailing-Stop-Logik spiegelt die MetaTrader-Version: Sie aktiviert sich, sobald der unrealisierte Gewinn die Trailing-Distanz überschreitet, und zieht den Stop kontinuierlich in Richtung Preis.
- Aktive Orders werden vor dem Wenden von Positionen storniert, um widersprüchliche Orders im Orderbuch zu vermeiden.
- Chartbereiche zeigen M15-Kerzen mit dem geglätteten gleitenden Durchschnitt und ein dediziertes stochastisches Panel für visuelle Diagnosen.

## Verwendungshinweise

- Konfigurieren Sie die Kerzentypen passend zum Datenanbieter (z. B. können volumenbasierte Kerzen ersetzt werden, wenn sie dasselbe DataType-Konzept unterstützen).
- Passen Sie den Schwellenwert und die Stop-Parameter an, wenn Sie mit Instrumenten unterschiedlicher Volatilität oder Tick-Größen handeln.
- Für beste Ergebnisse wenden Sie die Strategie auf Trendinstrumente an, bei denen Pullbacks zur gleitenden Durchschnittslinie häufig vorkommen.
