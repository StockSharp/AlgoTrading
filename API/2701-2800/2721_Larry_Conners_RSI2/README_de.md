# Larry Connors RSI-2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein getreuer StockSharp-Port des klassischen Larry Connors RSI-2-Systems. Die Strategie kombiniert einen schnellen 2-Perioden-RSI-Oszillator mit gleitenden Durchschnittsfiltern im Stundenzeittrahmen, um kurzfristige Mean-Reversion-Setups zu erfassen und gleichzeitig mit dem übergeordneten Trend ausgerichtet zu bleiben. Optionale Stop-Loss- und Take-Profit-Niveaus in Pips replizieren die originalen MetaTrader-Geldverwaltungsregeln.

## Konzeptübersicht

- **Typ**: Mean Reversion mit Trendfilter.
- **Markt**: Entwickelt für Forex-Paare auf dem H1-Chart.
- **Richtung**: Handelt sowohl Long als auch Short, aber nur in Richtung des langsamen SMA-Filters.
- **Kernindikatoren**: 5-Perioden-SMA (Ausstiegszeitpunkt), 200-Perioden-SMA (Trendfilter), 2-Perioden-RSI (Signalauslöser).

## Handelsregeln

### Long-Einstiege
- RSI-Wert fällt unter `RSI Long Entry` (Standard 6).
- Der Schlusskurs der abgeschlossenen Kerze bleibt über dem `Slow SMA` (Standard 200 Perioden).
- Keine offene Position vorhanden.

### Short-Einstiege
- RSI-Wert steigt über `RSI Short Entry` (Standard 95).
- Der Schlusskurs liegt unter dem `Slow SMA`.
- Keine offene Position vorhanden.

### Ausstiegsbedingungen
- **Long-Positionen** schließen, wenn der Schlusskurs über den `Fast SMA` (Standard 5) steigt. Optionale Stop-Loss- und Take-Profit-Niveaus in Pips können den Trade ebenfalls schließen, wenn aktiviert.
- **Short-Positionen** schließen, wenn der Schlusskurs unter den `Fast SMA` fällt. Optionale Stop-Loss- und Take-Profit-Niveaus in Pips gelten symmetrisch.

### Risikomanagement
- `Use Stop Loss` schaltet eine feste Stop-Distanz in Pips relativ zum Einstandspreis um.
- `Use Take Profit` aktiviert ein symmetrisches Gewinnziel in Pips.
- Pip-Distanzen werden über den `PriceStep` des Instruments und die Dezimalpräzision in absolute Preise umgerechnet, entsprechend der MT5-Logik für 4/5-stellige Kurse.

## Standardwerte

| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `Trade Volume` | 1 | Basis-Ordervolumen für jeden Einstieg. |
| `Fast SMA Period` | 5 | Ausstiegs-Timing-Durchschnitt. |
| `Slow SMA Period` | 200 | Trendrichtungsfilter. |
| `RSI Period` | 2 | Lookback für den RSI-Oszillator. |
| `RSI Long Entry` | 6 | Überverkauft-Schwelle für Long-Trades. |
| `RSI Short Entry` | 95 | Überkauft-Schwelle für Short-Trades. |
| `Use Stop Loss` | true | Schutzstop aktivieren/deaktivieren. |
| `Stop Loss (pips)` | 30 | Stop-Loss-Distanz in Pips. |
| `Use Take Profit` | true | Festes Gewinnziel aktivieren/deaktivieren. |
| `Take Profit (pips)` | 60 | Gewinnziel-Distanz in Pips. |
| `Candle Type` | 1 Stunde | Zeitrahmen der Arbeitskerzen. |

Alle einstellbaren Parameter stellen `.SetCanOptimize(true)` bereit und ermöglichen die Batch-Optimierung in Designer/Tester.

## Ausführungshinweise

- Signale werden auf geschlossenen Kerzen ausgewertet, um der ursprünglichen MetaTrader-Implementierung zu entsprechen.
- Schutzlevel werden intern verfolgt und schließen die gesamte Position mit Marktorders, wenn sie verletzt werden.
- Die Strategie setzt den internen Zustand (`pipSize`, Einstiegsanker) bei jedem Neustart zurück, um reproduzierbare Backtests zu gewährleisten.
- Fügen Sie die Strategie einem Projekt zusammen mit zuverlässigen Forex-Daten hinzu, um die veröffentlichten Performance-Ergebnisse zu replizieren.

## Empfohlene Verwendung

1. Verbinden Sie einen Forex-Datenfeed, der 1-Stunden-Kerzen liefert.
2. Fügen Sie die Strategie zu Designer hinzu oder führen Sie sie programmgesteuert über die StockSharp API aus.
3. Passen Sie pip-basierte Risikoparameter an die Kontraktspezifikationen des Brokers an, falls erforderlich.
4. Optimieren Sie optional RSI-Schwellenwerte oder gleitende Durchschnittslängen, um das Modell an andere Symbole anzupassen.

Durch die Beibehaltung der exakten RSI- und gleitenden Durchschnittslogik ermöglicht dieser Port MT5-Nutzern, die Larry Connors RSI-2-Methodik innerhalb des StockSharp-Ökosystems zu evaluieren.
