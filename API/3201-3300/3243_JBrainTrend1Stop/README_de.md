# JBrainTrend1Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **JBrainTrend1Stop-Strategie** ist ein StockSharp-Port des MetaTrader 5-Expertenberaters `Exp_JBrainTrend1Stop`. Sie kombiniert zwei Average True Range-Messungen, einen Stochastic-Oszillator und Jurik Moving Averages, um BrainTrading-Trendumkehrungen zu erkennen. Wenn der Jurik-geglättete Preis einen ausreichend großen Swing macht und der Stochastic seine neutrale Zone verlässt, wechselt die Strategie die Ausrichtung, aktualisiert die BrainTrend-Stop-Linie und dreht (optional) nach einer konfigurierbaren Verzögerung die Nettoposition um.

## Handelslogik

1. Velas abonnieren, die durch `CandleType` definiert sind, und sie einspeisen in:
   - Einen primären `AverageTrueRange` mit Länge `AtrPeriod`.
   - Einen erweiterten `AverageTrueRange` mit Periode `AtrPeriod + StopDPeriod`.
   - Einen `StochasticOscillator` mit `StochasticPeriod` und Ein-Bar-%K-Glättung (um die MT5-Einstellungen anzupassen).
   - Drei `JurikMovingAverage`-Instanzen (Hoch, Tief und Schlusskurs) mit `JmaLength` und `JmaPhase`.
2. Für jede abgeschlossene Kerze berechnen:
   - `range = ATR / 2.3` (entspricht der ursprünglichen Konstante `d = 2.3`).
   - `range1 = ATR_extended * 1.5` (entspricht `s = 1.5`).
   - `val3 = |JMA_close - JMA_close[shift 2]|`, was den MT5-Pufferdifferenz reproduziert.
3. Wenn `val3 > range` und der Stochastic sein neutrales Band verlässt:
   - Wenn `%K < 47` geht die Strategie in den bärischen BrainTrend-Zustand (`_trendState = -1`), setzt den Verkaufs-Stop bei `JMA_high + range1 / 4` und erzeugt ein **Verkaufs**-Signal.
   - Wenn `%K > 53` geht die Strategie in den bullischen Zustand (`_trendState = 1`), setzt den Kauf-Stop bei `JMA_low - range1 / 4` und erzeugt ein **Kauf**-Signal.
4. Während der Zustand unverändert bleibt, wird der BrainTrend-Stop um `range1` in Richtung Preis nachgezogen (`JMA_high + range1` für bärische Trends, `JMA_low - range1` für bullische Trends).
5. Signale werden nach `SignalBar` abgeschlossenen Bars ausgelöst. Bei Ausführung:
   - Ein Kaufsignal schließt Short-Positionen (wenn `SellClose` aktiviert ist) und öffnet optional eine neue Long-Position (wenn `BuyOpen` aktiviert ist).
   - Ein Verkaufssignal schließt Long-Positionen (wenn `BuyClose` aktiviert ist) und öffnet optional eine neue Short-Position (wenn `SellOpen` aktiviert ist).

Charts zeigen automatisch den Jurik-geglätteten Schlusskurs und den Stochastic-Oszillator neben Trade-Markierungen.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|--------------|---------|
| `CandleType` | Von der Strategie verarbeitete Kerzen-Serie. | H4 (4-Stunden-Zeitrahmen) |
| `AtrPeriod` | Länge des primären ATR für den BrainTrend-Trigger. | 7 |
| `StochasticPeriod` | Periode für %K/%D des Stochastic-Oszillators (Ein-Bar-%K-Glättung). | 9 |
| `StopDPeriod` | Zusätzliche Bars zum sekundären ATR-Zeitraum (`AtrPeriod + StopDPeriod`). | 3 |
| `JmaLength` | Jurik Moving Average-Länge für Hoch/Tief/Schluss. | 7 |
| `JmaPhase` | Phase-Argument für die Jurik Moving Averages (begrenzt auf [-100; 100]). | 100 |
| `SignalBar` | Anzahl abgeschlossener Bars vor einem neuen Signal. | 1 |
| `BuyOpen` / `SellOpen` | Long/Short-Positionen nach einem Signal eröffnen erlauben. | `true` |
| `BuyClose` / `SellClose` | Bestehende Long/Short-Positionen bei entgegengesetztem Signal schließen erlauben. | `true` |

Die `Volume`-Eigenschaft der Strategie oder die Broker-Konfiguration für die Ordergröße verwenden.

## Unterschiede zur MT5-Version

- Der ursprüngliche Geldmanagement-Block (`MM`, `MMMode`, `Deviation_`, dynamische Lot-Größenbestimmung) wird durch StockSharp-Standard-Ordergrößenbestimmung via `Volume` und Marktorders ersetzt. Slippage-Kontrolle wird nicht reproduziert.
- Absolute Stop-Loss- und Take-Profit-Abstände (`StopLoss_`, `TakeProfit_`) sind nicht implementiert. Schutz kann bei Bedarf manuell über die Hosting-Umgebung konfiguriert werden.
- BrainTrend-Stop-Niveaus werden intern für das Signal-Timing verwendet; sie werden nicht als ausstehende Orders platziert.
- Die Jurik Moving Averages verwenden die `JurikMovingAverage`-Implementierung von StockSharp. Der Phase-Parameter wird durch Reflexion angewendet, was dem Verhalten anderer BrainTrading-Ports in diesem Repository entspricht.

## Verwendung

1. Die Strategie an ein Wertpapier anhängen und `CandleType` setzen (z. B. 4-Stunden-Kerzen für Konsistenz mit dem EA).
2. Indikatorparameter (`AtrPeriod`, `StochasticPeriod`, `StopDPeriod`, `JmaLength`, `JmaPhase`) abstimmen, um die gewünschte BrainTrend-Empfindlichkeit zu erreichen.
3. `SignalBar` anpassen, um die Signalausführung bei Bedarf um mehrere abgeschlossene Bars zu verzögern.
4. `Volume` und die Öffnen/Schließen-Schalter konfigurieren, um die bevorzugte Handelsrichtung widerzuspiegeln.
5. (Optional) Externes Risikomanagement wie Stop-Loss oder Portfolio-Limits über die Hosting-Plattform hinzufügen.

Die Strategie verfolgt nach dem Start BrainTrend-Umkehrungen, schließt entgegengesetzte Positionen und dreht optional nach der konfigurierten Verzögerung die Richtung um.
