# SMC Trader Camel CCI MACD Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters **"Steve Cartwright Trader Camel CCI MACD"**.
Es reproduziert die ursprüngliche Handelslogik basierend auf einem exponentiellen gleitenden Durchschnittskanal im Kamelstil.
ein MACD-Trendfilter und Schwellenwerte für den Commodity Channel Index (CCI). Trades werden nach Abschluss ausgeführt
Kerzen, um deterministisches Verhalten sicherzustellen und nahe am Bar-für-Bar-Workflow der MQL-Version zu bleiben.

## Handelslogik

1. **Indikatoren**
   - Zwei exponentielle gleitende Durchschnitte (EMA) mit derselben Periode werden auf Kerzenhochs und -tiefs angewendet, um das zu bilden
Kamelkanal. Ein Ausbruch des vorherigen Schlusskurses über diese Hüllkurven hinaus signalisiert die Stärke des Momentums.
   - Ein Standard-MACD-Indikator (schneller EMA, langsamer EMA und Signallinie) wird verwendet, um die zugrunde liegende Trendrichtung zu bestätigen.
   - Ein CCI-Indikator validiert die Momentumstärke anhand der standardmäßig überkauften/überverkauften Niveaus von ±100.
2. **Lange Einträge**
   - Der vorherige Kerzenschluss liegt über dem Kamelhoch EMA.
   - Der vorherige MACD-Hauptwert liegt über Null **und** über der Signallinie.
   - Der vorherige CCI-Wert liegt über dem positiven Schwellenwert.
   - Es ist keine aktive Position offen und es erfolgte kein Ausstieg innerhalb des aktuellen Candle-Zeitrahmens (verhindert einen schnellen Wiedereinstieg).
3. **Kurze Einträge**
   - Der vorherige Kerzenschluss liegt unter dem Kameltief EMA.
   - Der vorherige MACD-Hauptwert liegt unter Null **und** unterhalb der Signallinie.
   - Der vorherige CCI-Wert liegt unter dem negativen Schwellenwert.
   - Gleiche flache Positions- und Abklingbedingungen wie bei langen Setups.
4. **Ausgänge**
   - Long-Positionen werden geschlossen, wenn der vorherige MACD-Hauptwert die Signallinie unterschreitet oder wenn der vorherige CCI
Der Wert fällt unter den positiven Schwellenwert.
   - Short-Positionen werden geschlossen, wenn der vorherige MACD-Hauptwert die Signallinie überschreitet.
   - Nach jedem Austritt wird vor neuen Eintritten eine Abklingzeit von einer Kerzendauer erzwungen.

Die Strategie handelt höchstens einmal pro Balken, da jede Entscheidung auf Daten der zuvor abgeschlossenen Kerze basiert.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `CandleType` | Kerzendatentyp/Zeitrahmen, der für alle Indikatoren verwendet wird. | 1-stündiger Zeitrahmen |
| `CamelLength` | Länge des Hoch-/Tief-EMA-Kanals. | 34 |
| `CciPeriod` | Länge des CCI-Filters. | 20 |
| `MacdFastPeriod` | Schnelle EMA-Länge für MACD. | 12 |
| `MacdSlowPeriod` | Langsame EMA-Länge für MACD. | 26 |
| `MacdSignalPeriod` | Signalglättungszeitraum für MACD. | 9 |
| `CciThreshold` | Absoluter CCI-Pegel, der für Einträge überschritten werden muss (symmetrisch angewendet). | 100 |

Alle Parameter sind dank der `SetOptimize`-Aufrufe durch den StockSharp-Optimierer optimierbar.

## Risikomanagement

- Bestellungen werden über `BuyMarket` und `SellMarket` gesendet, wobei die Strategieeigenschaft `Volume` geerbt wird.
- `StartProtection()` ist aktiviert, um standardmäßige StockSharp-Schutzhelfer zu initialisieren.
- Im ursprünglichen Algorithmus ist kein fester Stop-Loss oder Take-Profit definiert; Exits basieren ausschließlich auf Indikatorsignalen.

## Diagramme

Die Strategie zeichnet automatisch die Kamel-Indikatoren EMA-Kanal, MACD und CCI zusammen mit eigenen Trades auf.
Dies repliziert die visuellen Hinweise, die in der MT4-Implementierung verwendet wurden.

## Notizen

- Der Abklingzeit-Timer verwendet die von `CandleType.Arg` abgeleitete Kerzendauer. Stellen Sie sicher, dass `CandleType` ein enthält
`TimeSpan`-Argument, wenn Sie den Zeitrahmen ändern.
- Da alle Entscheidungen auf den Werten des vorherigen Balkens basieren, spiegelt die Reihenfolge der Operationen die `iMACD`, `iCCI` wider.
und `iMA` (mit Shift=1) ruft in der Quelle EA auf.
