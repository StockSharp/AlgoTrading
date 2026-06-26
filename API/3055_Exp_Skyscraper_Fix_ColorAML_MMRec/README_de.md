# Exp Skyscraper Fix Color AML MMRec-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Exp Skyscraper Fix Color AML MMRec ist der StockSharp-Port des MQL5-Expertenberaters *Exp_Skyscraper_Fix_ColorAML_MMRec*. Der ursprüngliche Roboter kombiniert zwei unabhängige Indikatoren — **Skyscraper Fix** und **Color AML** — und wendet die MMRec-Geldverwaltungslogik an, um die Ordergröße nach aufeinanderfolgenden Verlusten zu reduzieren. Die C#-Implementierung behält beide Signalquellen und die adaptive Positionsgrößenbestimmung bei, während die High-Level-API von StockSharp für das Order-Routing verwendet wird.

## Handelsablauf

1. **Skyscraper Fix-Modul** baut einen adaptiven Kanal aus den abgeschlossenen Kerzen von `SkyscraperCandleType`. Wenn die Kanalfarbe teal wird (Trend &gt; 0), kann jede Short-Position geschlossen werden und, wenn die vorherige Farbe nicht teal war, wird ein neuer Long-Trade eröffnet. Wenn die Farbe rot wird (Trend &lt; 0), wird die Logik für Short-Trades gespiegelt. Die Hilfsklasse `SkyscraperFixIndicator` wird aus der Strategie `3040_Exp_Skyscraper_Fix_Duplex` wiederverwendet.
2. **Color AML-Modul** verarbeitet Kerzen aus `ColorAmlCandleType`. Der übersetzte `ColorAmlIndicator` reproduziert das adaptive Marktniveau und gibt einen Farbcode aus: `2` (bullisch), `0` (bärisch) oder `1` (neutral). Das Modul schließt die Gegenseite immer dann, wenn eine bullische oder bärische Farbe erkannt wird, und eröffnet eine neue Position, wenn sich die Farbe gegenüber der vorherigen verzögerten Probe geändert hat.
3. **Signalverzögerung** wird unabhängig für beide Module über `SkyscraperSignalBar` und `ColorAmlSignalBar` gesteuert. Die Strategie pflegt Warteschlangen von Indikatorausgaben und führt Orders erst nach der konfigurierten Anzahl geschlossener Kerzen aus, was dem Verhalten `CopyBuffer(..., shift, ...)` im Expertenberater entspricht.
4. **Risikomanagement** spiegelt die ursprünglichen Stop/Take-Profit-Abstände wider. Jedes Modul definiert seine eigenen Schutzabstände in Preisschritten (Ticks). Die Strategie übersetzt sie in absolute Preise und prüft bei jeder abgeschlossenen Kerze, ob der Balkenbereich einen Stop-Loss oder Take-Profit berührt hat. Wenn ja, wird die Position mit einer Marktorder geglättet und alle Schutzniveaus werden gelöscht.
5. **MMRec-Geldverwaltung** verfolgt aufeinanderfolgende Verlust-Trades separat für Skyscraper Long, Skyscraper Short, Color AML Long und Color AML Short-Einstiege. Wenn die Verlustserie für eine Richtung den entsprechenden Auslöser (`*LossTrigger`) erreicht, wechselt das Volumen von `*Mm` auf den reduzierten Wert `*SmallMm`. Sobald ein profitabler Trade erscheint, wird die Serie auf null zurückgesetzt. Da die Beispielstrategie auf einer einzigen Nettoposition läuft, hat nur der `Lot`-Verwaltungsmodus praktische Auswirkung; andere Modi fallen auf direkte Lot-Größenbestimmung zurück.

## Implementierungshinweise

- Der Code verlässt sich ausschließlich auf die High-Level-API von StockSharp: Kerzenabonnements versorgen beide Indikatoren und alle Handelsentscheidungen werden durch die Helfer `BuyMarket`, `SellMarket` und `ClosePosition` ausgeführt.
- Schutzorders werden mit Marktausstiegen implementiert, nicht mit separaten Stop-/Limit-Orders. Dies vermeidet Konflikte, wenn beide Module dieselbe Nettoposition teilen.
- Die Geldverwaltung verwendet Ausführungsdaten, die in `OnOwnTradeReceived` empfangen werden, um das Ergebnis des vorherigen Trades zu bestimmen. Das Modul, das die Position eröffnet hat, speichert seinen Bezeichner, damit der korrekte Verlustzähler aktualisiert wird, wenn die Position geschlossen wird.
- Der übersetzte `ColorAmlIndicator` cached Kerzen und Glättungswerte, um dem ursprünglichen exponentiellen Glättungsschema zu folgen, einschließlich des dynamischen Alpha auf Basis von Fraktal-Ranges und der Farbkodierungslogik (Blau für steigendes AML, Rot für fallendes, Grau sonst).
- Magic Numbers und explizite Slippage-Einstellungen aus der MQL5-Version sind in StockSharp nicht erforderlich und werden daher weggelassen.

## Parameter

### Skyscraper Fix-Modul

| Parameter | Standard | Beschreibung |
| --- | --- | --- |
| `SkyscraperCandleType` | H4-Kerzen | Zeitrahmen zur Berechnung des Skyscraper Fix-Kanals. |
| `SkyscraperLength` | 10 | ATR-Lookback zur Definition des adaptiven Kanalschritts. |
| `SkyscraperKv` | 0.9 | Multiplikator auf die ATR-basierte Schrittgröße. |
| `SkyscraperPercentage` | 0 | Prozentualer Versatz der Mittellinie. |
| `SkyscraperMode` | HighLow | Preisquelle für den Envelope (High/Low oder Close). |
| `SkyscraperSignalBar` | 1 | Anzahl geschlossener Kerzen zur Verzögerung von Skyscraper-Signalen. |
| `SkyscraperEnableLongEntry` | true | Long-Einstiege erlauben, wenn der Kanal bullisch wird. |
| `SkyscraperEnableShortEntry` | true | Short-Einstiege erlauben, wenn der Kanal bärisch wird. |
| `SkyscraperEnableLongExit` | true | Long-Positionen bei bärischen Skyscraper-Signalen schließen. |
| `SkyscraperEnableShortExit` | true | Short-Positionen bei bullischen Skyscraper-Signalen schließen. |
| `SkyscraperBuyLossTrigger` | 2 | Aufeinanderfolgende Long-Verluste, die zum Wechsel auf reduziertes Volumen führen. |
| `SkyscraperSellLossTrigger` | 2 | Aufeinanderfolgende Short-Verluste, die zum Wechsel auf reduziertes Volumen führen. |
| `SkyscraperSmallMm` | 0.01 | Ordervolumen nach Erreichen des Verlustauslösers. |
| `SkyscraperMm` | 0.1 | Standard-Ordervolumen für Skyscraper-Signale. |
| `SkyscraperMmMode` | Lot | Geldverwaltungsmodus (nur `Lot` wirkt sich auf den C#-Port aus). |
| `SkyscraperStopLossTicks` | 1000 | Stop-Loss-Abstand in Preisschritten. Wert 0 deaktiviert den Stop. |
| `SkyscraperTakeProfitTicks` | 2000 | Take-Profit-Abstand in Preisschritten. Wert 0 deaktiviert das Ziel. |

### Color AML-Modul

| Parameter | Standard | Beschreibung |
| --- | --- | --- |
| `ColorAmlCandleType` | H4-Kerzen | Zeitrahmen für den Color AML-Indikator. |
| `ColorAmlFractal` | 6 | Fraktal-Fenster für die AML-Range-Berechnungen. |
| `ColorAmlLag` | 7 | Glättungs-Lag für die exponentielle AML-Mittelung. |
| `ColorAmlSignalBar` | 1 | Anzahl geschlossener Kerzen zur Verzögerung von Color AML-Signalen. |
| `ColorAmlEnableLongEntry` | true | Long-Einstiege erlauben, wenn AML bullisch wird (Farbe 2). |
| `ColorAmlEnableShortEntry` | true | Short-Einstiege erlauben, wenn AML bärisch wird (Farbe 0). |
| `ColorAmlEnableLongExit` | true | Long-Positionen bei bärischen AML-Farben schließen. |
| `ColorAmlEnableShortExit` | true | Short-Positionen bei bullischen AML-Farben schließen. |
| `ColorAmlBuyLossTrigger` | 2 | Aufeinanderfolgende Long-Verluste vor dem Wechsel auf reduziertes Volumen. |
| `ColorAmlSellLossTrigger` | 2 | Aufeinanderfolgende Short-Verluste vor dem Wechsel auf reduziertes Volumen. |
| `ColorAmlSmallMm` | 0.01 | Ordervolumen nach Erreichen des Verlustauslösers. |
| `ColorAmlMm` | 0.1 | Standard-Ordervolumen für Color AML-Signale. |
| `ColorAmlMmMode` | Lot | Geldverwaltungsmodus (nur `Lot` wirkt sich auf den C#-Port aus). |
| `ColorAmlStopLossTicks` | 1000 | Stop-Loss-Abstand in Preisschritten. Auf 0 setzen zum Deaktivieren. |
| `ColorAmlTakeProfitTicks` | 2000 | Take-Profit-Abstand in Preisschritten. Auf 0 setzen zum Deaktivieren. |

## Verwendung

1. Binden Sie die Strategie an ein Portfolio und das zu handelnde Instrument. Das Wertpapier muss die durch `SkyscraperCandleType` und `ColorAmlCandleType` definierten Kerzenserien bereitstellen.
2. Passen Sie die Geldverwaltungsparameter an, wenn Ihr Broker einen anderen Lot-Schritt verwendet. Da nur direkte Lot-Größenbestimmung angewendet wird, konfigurieren Sie `*Mm` und `*SmallMm` entsprechend.
3. Ändern Sie optional die Stop-Loss- und Take-Profit-Abstände (in Ticks) für jedes Modul. Das Setzen eines Abstands auf null deaktiviert den entsprechenden Schutz.
4. Starten Sie die Strategie. Sie abonniert beide Kerzenströme, berechnet die Indikatoren und verwaltet Einstiege und Ausstiege automatisch gemäß den oben genannten Regeln.

Das README spiegelt das Verhalten von `CS/ExpSkyscraperFixColorAmlMmrecStrategy.cs` wider und sollte als Referenzdokumentation für diese StockSharp-Implementierung verwendet werden.
