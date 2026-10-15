# MacdPatternTraderV01 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

`MacdPatternTraderV01Strategy` ist eine originalgetreue StockSharp-Portierung des FORTRADER „MacdPatternTraderv01“ MetaTrader 4-Expertenberaters. Das System sucht nach MACD-Hook-Mustern, die auftreten, nachdem der Oszillator ein extremes Niveau erreicht hat und dann in Richtung der Nulllinie zurückrollt. Wenn sich nach einem überkauften Anstieg ein bärischer Haken bildet, eröffnet die Strategie Short-Positionen, während ein bullischer Haken nach einem überverkauften Rückgang Long-Positionen auslöst. Die StockSharp-Version behält das ursprüngliche mehrschichtige Risikomanagement bei, einschließlich rekursiver Stop-Loss- und Take-Profit-Levels sowie einer abgestuften Positionsskalierung.

Die C#-Implementierung verwendet das High-Level-Kerzenabonnement API mit den Indikatoren `MACD`, `ExponentialMovingAverage` und `SimpleMovingAverage`. Alle Berechnungen werden an fertigen Kerzen durchgeführt und spiegeln die Aufrufe `iMACD` und `iMA` mit expliziten Balkenverschiebungen aus der Version MQL wider. Zusätzliche Hilfslogik verfolgt manuell die jüngsten Höchst- und Tiefststände, um die rekursiven Preissuchen zu reproduzieren, die EA für Schutzaufträge verwendet.

## Signallogik

1. **Scharfschaltbedingungen**
   - Ein *bärisches* Setup wird aktiviert, sobald die MACD-Hauptlinie `BearishThreshold` überschreitet. Das Scharfschaltflag wird gelöscht, sobald MACD unter Null fällt.
   - Ein *bullisches* Setup wird aktiviert, sobald die MACD-Hauptlinie unter `BullishThreshold` fällt. Das Flag wird gelöscht, wenn MACD positiv wird.
2. **Hook-Bestätigung**
   - Für kurze Einstiege ist es erforderlich, dass `macd₀ < BearishThreshold`, `macd₀ < macd₁`, `macd₁ > macd₂`, die bärische Flagge aktiv bleibt, und `macd₂ < BearishThreshold`, während `macd₀` über Null bleibt.
   - Lange Einträge erfordern `macd₀ > BullishThreshold`, `macd₀ > macd₁`, `macd₁ < macd₂`, die bullische Flagge, um aktiv zu bleiben, und `macd₂ > BullishThreshold`, während `macd₀` negativ bleibt.
3. **Auftragsausführung**
   - Wenn der Hook abgeschlossen ist, sendet die Strategie eine Marktorder mit dem Volumen `OrderVolume`. Gleichzeitig werden die berechneten Stop-Loss- und Take-Profit-Preise zur späteren Überwachung gespeichert.

## Risikomanagement

### Stop-Loss

Der Stop-Loss ahmt die MQL-Funktion `StopLoss(type)` nach:

- Short-Trades suchen nach dem höchsten Hoch der letzten `StopLossBars` Kerzen **ausgenommen** des frisch geschlossenen Balkens und addieren dann `OffsetPoints * PriceStep` zum Ergebnis.
- Long-Trades suchen nach dem niedrigsten Tief der letzten `StopLossBars` historischen Kerzen und subtrahieren den gleichen Offset.

Diese Logik wird mit manuellen Extremwertsuchen über einen begrenzten In-Memory-Puffer (1.000 Werte) implementiert, um den Aufbau großer benutzerdefinierter Sammlungen zu vermeiden.

### Take-Profit

Der Take-Profit reproduziert die rekursive Routine `TakeProfit(type)` MQL:

1. Beginnen Sie mit dem neuesten Block von `TakeProfitBars`-Werten. Fügen Sie die Kerze hinzu, die das Signal ausgelöst hat.
2. Berechnen Sie das Extrem (niedrig für Short-Positionen, hoch für Long-Positionen) innerhalb dieses Blocks.
3. Gehen Sie um `TakeProfitBars` Kerzen zurück und wiederholen Sie den Vorgang, während der neue Block ein günstigeres Extrem liefert.
4. Stoppen Sie beim ersten Block, der das Extrem **nicht** verbessert, und verwenden Sie den zuletzt aufgezeichneten Wert als Take-Profit.

### Teilpositionsverwaltung

- Nach dem Einstieg erfasst die Strategie das ursprüngliche Volumen und den Einstiegspreis.
- Teilausstiege sind erst zulässig, wenn der in der Kontowährung ausgedrückte variable Gewinn `ProfitThreshold` übersteigt.
- Für Long-Positionen:
  1. Schließen Sie ein Drittel des ursprünglichen Volumens, wenn der Schlusskurs der Kerze über den Mittelwert EMA (`EmaMediumPeriod`) steigt.
  2. Schließen Sie die Hälfte der verbleibenden Position, wenn das Kerzenhoch den Durchschnitt der Werte `SmaPeriod` und `EmaLongPeriod` durchbricht.
- Für Short-Positionen spiegeln sich die Regeln wider, wobei der Kerzenschluss unter dem Mittelwert EMA und der Kerzentiefstwert unter dem zusammengesetzten Durchschnitt liegt.

Schutzbefehle werden vor der Skalierung überprüft, um sicherzustellen, dass harte Stopps oder Ziele immer Vorrang haben.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `StopLossBars` | 6 | Anzahl der historischen Kerzen für die Stop-Loss-Swing-Suche. |
| `TakeProfitBars` | 20 | Blockgröße, die vom rekursiven Take-Profit-Algorithmus verwendet wird. |
| `OffsetPoints` | 10 | Zusätzliche Punkte werden zum Stop-Loss-Preis hinzugefügt. |
| `MacdFastPeriod` | 5 | Schnelle EMA-Länge des MACD-Indikators. |
| `MacdSlowPeriod` | 13 | Langsame EMA-Länge des MACD-Indikators. |
| `MacdSignalPeriod` | 1 | Signallänge EMA des Indikators MACD. |
| `BearishThreshold` | 0,0045 | Positiver MACD-Pegel, der kurze Setups aktiviert. |
| `BullishThreshold` | -0,0045 | Negativer MACD-Pegel, der lange Setups aktiviert. |
| `OrderVolume` | 1 | Volumen pro Market-Order. |
| `EmaShortPeriod` | 7 | Schnelles EMA wird im ersten Teilexit verwendet. |
| `EmaMediumPeriod` | 21 | Mittel EMA wird in Filtern und Teilexits verwendet. |
| `SmaPeriod` | 98 | SMA wird im zusammengesetzten Exit-Durchschnitt verwendet. |
| `EmaLongPeriod` | 365 | Langes EMA kombiniert mit dem SMA für den zweiten Teilausstieg. |
| `ProfitThreshold` | 5 | Minimaler variabler Gewinn (in Währungseinheiten) vor der Skalierung. |
| `CandleType` | 1-stündiger Zeitrahmen | Von der Strategie verarbeitete Kerzenserie. |

Alle Parameter werden über `StrategyParam<T>` verfügbar gemacht und unterstützen gegebenenfalls die Optimierung.

## Implementierungshinweise

- Die Strategie basiert ausschließlich auf `SubscribeCandles`-Bindungen auf hoher Ebene. Gemäß den Projektrichtlinien werden keine Indikatoren in die `Indicators`-Sammlung verschoben.
- Der MACD-Verlauf wird mithilfe eines kompakten dreiwertigen Schieberegisters (`_macdPrev1..3`) gespeichert, um den `iMACD(..., shift)`-Zugriff nachzuahmen.
- Schutzpreisniveaus werden als Dezimalzahlen verfolgt; Wenn Kerzen einen Stop oder ein Ziel erreichen, schließt die Strategie die gesamte Position mit Marktaufträgen und setzt die interne Zustandsmaschine zurück.
- Der variable PnL wird unter Verwendung von `PriceStep`/`StepPrice` geschätzt, sodass der Teilausstiegsschwellenwert unabhängig von der Preisskala des Instruments konsistent bleibt.
- Die Kerzenpuffer für Hochs und Tiefs sind auf 1.000 Elemente begrenzt, was für die Standardparameter ausreichend ist, aber unkontrolliertes Wachstum verhindert.

## Nutzung

1. Instanziieren Sie `MacdPatternTraderV01Strategy` und weisen Sie die gewünschte Sicherheit, das gewünschte Portfolio und den gewünschten Connector zu.
2. Passen Sie optional Parameter wie `CandleType`, `StopLossBars` oder `OrderVolume` an das gehandelte Instrument an.
3. Starten Sie die Strategie; Es abonniert die konfigurierte Kerzenserie, zeichnet MACD und handelt mit Markierungen im Diagramm und verwaltet Aufträge automatisch.

Die Strategie enthält ausführliche Inline-Kommentare, die jeden übersetzten Block beschreiben, um die Wartung und weitere Anpassung zu erleichtern.
