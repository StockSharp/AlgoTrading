# Blockbuster-Bollinger-Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Blockbuster Bollinger Breakout-Strategie ist eine direkte Portierung des MetaTrader 4-Expertenberaters „BLOCKBUSTER EA“. Das ursprüngliche System suchte nach aggressiven Umkehrungen, nachdem der Preis um eine konfigurierbare Distanz über ein Bollinger-Band hinaus gestiegen war. Diese StockSharp-Version behält die gleiche Logik bei, umfasst aber das High-Level-API für Kerzenabonnements, Indikatorbindung und Positionsverwaltung.

## Kernidee

1. Erstellen Sie Bollinger-Bänder mit einem benutzerdefinierten Zeitraum und einer benutzerdefinierten Abweichung.
2. Messen Sie, wann der Schlusskurs der aktuellen Kerze um einen zusätzlichen Versatz (in Punkten) über das obere Band oder unter das untere Band bricht.
3. Geben Sie Short ein, wenn der Schlusskurs das obere Band plus den Offset überschreitet. Geben Sie „Long“ ein, wenn der Schlusskurs unter das untere Band abzüglich des Offsets fällt.
4. Verwalten Sie die Position mit punktbasierten Gewinn- und Verlustschwellenwerten, die mit den MQL-Einstellungen identisch sind.

Distanz, Stopp und Ziel werden in Instrumentenpunkten ausgedrückt. Sie passen sich dem Preisschritt des Instruments an, sodass ein Wert von `3` drei `PriceStep`-Einheiten bedeutet, unabhängig vom zugrunde liegenden Symbol.

## Detaillierte Logik

- **Indikatorberechnung**
  - Indikator: Bollinger Bänder.
  - Eingaben: Candle-Close-Preise (der MT4-Code verwendet `PRICE_OPEN`; dieser Port behält die Close-Preise für eine bessere StockSharp-Kompatibilität bei und behält gleichzeitig die Bandlänge und die Abweichungsparameter bei).
  - Parameter:
    - `BollingerPeriod`: Anzahl der Kerzen, die im gleitenden Durchschnitt und in der Standardabweichung verwendet werden.
    - `BollingerDeviation`: Standardabweichungsmultiplikator für das obere und untere Band.
  - Zusätzlicher Offset `DistancePoints` (umgerechnet in Preis mit dem Instrument `PriceStep`).

- **Eintrittsbedingungen**
  - **Long**: `Close < LowerBand - Distance` und die aktuelle Nettoposition ist flach oder short.
  - **Short**: `Close > UpperBand + Distance` und die aktuelle Nettoposition ist flach oder long.
  - Jede offene Gegenposition wird um die Marktordergröße `TradeVolume + |Position|` abgeflacht, um das MT4-Verhalten „Nur eine Order“ widerzuspiegeln.

- **Exit Conditions**
  - Die Positionen jeder fertigen Kerze werden überwacht. Der nicht realisierte Gewinn in Punkten wird mit dem Instrument `PriceStep` berechnet.
  - **Take Profit**: wenn der Gewinn `ProfitTargetPoints` erreicht oder überschreitet.
  - **Stop-Loss**: wenn der Verlust `LossLimitPoints` erreicht oder überschreitet.
  - Exits werden mit Marktaufträgen durchgeführt, die die gesamte Position schließen.

- **Risiko- und Geldmanagement**
  - `TradeVolume` legt die Basisauftragsgröße fest. Der Abgleich der MetaTrader-Eingabe „Lots“ ist so einfach wie das Festlegen desselben numerischen Werts.
  - Sowohl Stopp als auch Ziel können deaktiviert werden, indem der entsprechende Parameter auf `0` gesetzt wird.
  - Wenn beide Schwellenwerte aktiviert sind, wird der Stopp nach dem Ziel ausgewertet, genau so, wie der ursprüngliche EA zuerst den Gewinnzweig überprüft hat.

- **Statusverfolgung**
  - Die Strategie erfasst den Einstiegspreis zum Zeitpunkt des Signals und verwendet ihn für alle nachfolgenden Gewinn-/Verlustberechnungen.
  - Wenn eine Ausstiegsorder die Position abflacht, wird der Status automatisch zurückgesetzt.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `BollingerPeriod` | 20 | Anzahl der Kerzen im gleitenden Durchschnitt der Bollinger-Bänder. |
| `BollingerDeviation` | 2,0 | Standardabweichungsmultiplikator. |
| `DistancePoints` | 3 | Zusätzlicher Abstand über das Band hinaus, bevor ein Trade platziert wird (Instrumentenpunkte). |
| `ProfitTargetPoints` | 3 | Take-Profit-Schwelle in Instrumentenpunkten. Zum Deaktivieren auf 0 setzen. |
| `LossLimitPoints` | 20 | Stop-Loss-Schwelle in Instrumentenpunkten. Zum Deaktivieren auf 0 setzen. |
| `TradeVolume` | 1 | Band für neue Einträge. |
| `CandleType` | Zeitrahmen von 1 Minute | Für Berechnungen verwendeter Kerzentyp. |

## Nutzungshinweise

- Funktioniert auf jedem Instrument, das Kerzen und einen `PriceStep` ungleich Null liefert. Forex-Paare, Index-CFDs und liquide Futures spiegeln die ursprüngliche EA-Umgebung am besten wider.
- Da der Indikator nun auf Schlusskursen basiert, wird empfohlen, ihn im vorgesehenen Zeitrahmen zu testen, um ein ähnliches Verhalten wie bei der MT4-Version sicherzustellen.
- Die Strategie verwendet `CreateChartArea`-Helfer, um Kerzen, die Bollinger-Bänder und ausgeführte Trades zu visualisieren, wenn ein Diagramm in der Benutzeroberfläche verfügbar ist.
- Die Logik geht von einer kontinuierlichen Auswertung fertiger Kerzen aus und gewährleistet so ein deterministisches Verhalten beim Backtesting und Live-Handel.

## Schlagworte

- Kategorie: Ausbruch gegen den Trend
- Richtung: Beide
- Indikatoren: Bollinger Bänder
- Stoppt: Ja (konfigurierbar)
- Zeitrahmen: Kurzfristig (Standard 1 Minute)
- Komplexität: Einfach
