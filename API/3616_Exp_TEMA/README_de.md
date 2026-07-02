# Exp TEMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Exp TEMA-Strategie** ist eine StockSharp-Portierung des MetaTrader-Expertenberaters `Exp_TEMA.mq5`. Das ursprüngliche System scannt mehrere Forex-Paare und überwacht die Steigung des Triple Exponential Moving Average (TEMA). Immer wenn die Steigung ihr Vorzeichen umkehrt, betritt der Experte entweder eine neue Trendfolgeposition oder verlässt die entgegengesetzte. Diese C#-Konvertierung behält die gleiche Indikatorlogik bei und konzentriert sich dabei auf ein einzelnes Wertpapier, das der Strategie in StockSharp zugewiesen ist.

## Handelslogik

Die Strategie arbeitet mit fertigen Kerzen, die durch den ausgewählten Parameter `CandleType` erzeugt wurden. Bei jedem Kerzenschluss wird ein TEMA mit der konfigurierbaren Länge `TemaPeriod` berechnet. Drei aufeinanderfolgende TEMA-Messwerte werden verglichen, um das Steigungserkennungsschema des MQL5-Experten zu reproduzieren:

1. Sei `tema[0]` der letzte Kerzenwert, `tema[1]` der vorherige und `tema[2]` der Wert zwei Kerzen zurück.
2. Die kurzfristige Steigung beträgt `d1 = tema[1] - tema[2]`, während die ältere Steigung `d2 = tema[2] - tema[3]` beträgt.
3. Ein **bullischer Einstieg** wird ausgelöst, wenn die Steigung ansteigt (`d2 < 0` und `d1 > 0`). Jede Short-Position wird zuerst geschlossen, dann wird eine Long-Order von `Volume + |Position|` Lots platziert.
4. Ein **bärischer Einstieg** wird ausgelöst, wenn die Steigung nach unten geht (`d2 > 0` und `d1 < 0`). Jede Long-Position wird zuerst abgeflacht, dann wird eine Short-Order von `Volume + |Position|` Lots gesendet.
5. Schutzausgänge ahmen die ursprünglichen Stop-Flags nach: Wenn die aktuelle Steigung negativ wird, wird die Long-Position geschlossen, während eine positive Steigung jede Short-Position schließt.

Dies reproduziert das gleiche Signaltiming wie die Quelle EA, ohne historischen Pufferzugriff zu verwenden, und bleibt innerhalb des High-Level-StockSharp API.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `TemaPeriod` | 15 | Länge des dreifach exponentiellen gleitenden Durchschnitts. |
| `TradeVolume` | 1 | Grundauftragsvolumen. Die ausgeführte Größe wird zu „TradeVolume +“. |Position|` beim Rückwärtsfahren. |
| `StopLossPoints` | 1000 | Stop-Loss-Distanz ausgedrückt in Preisschritten. Wird an `StartProtection` übergeben, wenn positiv. |
| `TakeProfitPoints` | 2000 | Take-Profit-Distanz, ausgedrückt in Preisschritten. Wird an `StartProtection` übergeben, wenn positiv. |
| `CandleType` | 15-Minuten-Kerzen | Kerzentyp, der den Indikator speist. Wählen Sie einen Zeitrahmen, der dem vom ursprünglichen Experten verwendeten Diagramm entspricht. |

Alle Parameter werden mit `StrategyParam<T>` erstellt, sodass sie im Designer optimiert werden können.

## Unterschiede zum MQL5 Expert

- Die Version MQL verwaltet bis zu zwölf Symbole gleichzeitig. StockSharp-Strategien sind an einen bestimmten `Security` gebunden, daher handelt dieser Port mit dem Instrument, das beim Start der Strategie zugewiesen wurde. Führen Sie mehrere Strategieinstanzen aus, wenn eine Abdeckung mit mehreren Symbolen erforderlich ist.
- Die Auftragsverwaltung basiert auf `BuyMarket`/`SellMarket` und `StartProtection`, die die ursprünglichen Marktaufträge, Stopps und Ziele dem übergeordneten API von StockSharp zuordnen.
- Der Indikatorzugriff erfolgt über `SubscribeCandles().Bind(...)`, wodurch manuelles Pufferkopieren vermieden wird und die Repository-Richtlinien eingehalten werden.

## Nutzungstipps

1. Hängen Sie die Strategie an das gewünschte Wertpapier an und legen Sie den `CandleType` fest, der Ihrem analytischen Zeitrahmen entspricht.
2. Passen Sie die Stop- und Take-Profit-Abstände in Preisschritten entsprechend der Volatilität des Instruments an.
3. Optional: Führen Sie die Optimierung für `TemaPeriod`, `StopLossPoints` und `TakeProfitPoints` aus, um die in MetaTrader durchgeführten Parameter-Sweeps zu replizieren.
4. Überwachen Sie den enthaltenen Diagrammbereich, um Kerzen, die TEMA-Linie und die ausgeführten Trades zu visualisieren.
