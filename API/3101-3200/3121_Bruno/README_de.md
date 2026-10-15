# Bruno-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Bruno-Expertenberater ist ein Trendfolgesystem, das ursprünglich für MetaTrader 5 geschrieben wurde. Der Port behält dieselbe Bestätigungskette: Average Directional Index (ADX) mit Richtungsbewegung, ein Paar exponentieller gleitender Durchschnitte (EMA 8/21), MACD (13, 34, 8), ein Stochastischer Oszillator (21, 3, 3) und die Steigung eines Parabolic SAR (0.055, 0.21). Jeder Filter, der mit der Richtung übereinstimmt, multipliziert die Ordergröße mit einem konfigurierbaren Faktor. Wenn sowohl Long- als auch Short-Signale auf derselben Kerze verstärkt werden, wird kein Trade durchgeführt, um widersprüchliche Orders zu vermeiden.

### Handelslogik

- **Richtungsbias**
  - Long-Druck wird verstärkt wenn `+DI > -DI` und `+DI > 20`.
  - Short-Druck wird verstärkt wenn `+DI < -DI` und `+DI < 40`.
- **Momentum-Ausrichtung**
  - Long-Präferenz erfordert EMA(8) über EMA(21), Stochastisches %K über %D und %K unter der Überkauft-Schwelle (Standard 80).
  - Short-Präferenz erfordert EMA(8) unter EMA(21), Stochastisches %K unter %D und %K über der Überverkauft-Schwelle (Standard 20).
- **MACD-Filter**
  - Long-Bias: MACD-Histogramm über null und MACD-Hauptlinie über der Signallinie.
  - Short-Bias: MACD-Histogramm unter null und MACD-Hauptlinie unter der Signallinie.
- **Parabolic SAR-Steigung**
  - Long-Bias wird verstärkt wenn die vorherigen SAR-Werte steigen während EMA(8) > EMA(21).
  - Short-Bias wird verstärkt wenn die vorherigen SAR-Werte fallen während EMA(8) < EMA(21).

Jede erfüllte Bedingung multipliziert die Basis-Lotgröße mit `SignalMultiplier` (Standard 1.6). Nur eine Seite kann gleichzeitig aktiv sein. Wenn ein abschließendes Signal generiert wird, schließt die Strategie jede entgegengesetzte Position, sendet die Marktorder mit dem multiplizierten Volumen und speichert den aktuellen Schlusskurs als Einstiegspreis.

### Positionsmanagement

- **Stop-Loss / Take-Profit** – feste Abstände in angepassten Pips, entsprechend der MetaTrader-Version. Wenn ein Level intrabar getroffen wird, wird die Position sofort geschlossen.
- **Trailing-Stop** – aktiviert, sobald der schwebende Gewinn `TrailingStop + TrailingStep` Pips übersteigt. Der Stop wird dann `TrailingStop` Pips hinter dem Preis platziert und rückt nur vor, wenn der Gewinn um mindestens `TrailingStep` weitere Pips zunimmt.
- **Konfliktbehandlung** – wenn sowohl Long- als auch Short-Filter auf derselben Kerze ausgelöst werden, wird kein neuer Trade eingegangen.

### Parameter

| Gruppe | Name | Beschreibung |
| --- | --- | --- |
| Handel | `BaseVolume` | Anfängliche Lotgröße vor Multiplikatoren. |
| Handel | `SignalMultiplier` | Volumen-Multiplikator, der von jedem übereinstimmenden Filter angewendet wird. |
| Risikomanagement | `StopLossPips` / `TakeProfitPips` | Schutzabstände in angepassten Pips. Auf null setzen zum Deaktivieren. |
| Risikomanagement | `TrailingStopPips` / `TrailingStepPips` | Trailing-Abstand und Mindestschritt in angepassten Pips. |
| Indikatoren | `AdxPeriod`, `AdxPositiveThreshold`, `AdxNegativeThreshold` | ADX-Länge und DI-Schwellenwerte. |
| Indikatoren | `FastEmaPeriod`, `SlowEmaPeriod` | EMA-Längen für die Trendbestätigung. |
| Indikatoren | `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | MACD-Konfiguration. |
| Indikatoren | `StochasticPeriod`, `StochasticKsmoothing`, `StochasticDsmoothing`, `StochasticOverbought`, `StochasticOversold` | Stochastischer Oszillator-Einstellungen. |
| Allgemein | `CandleType` | Zeitrahmen für die gesamte Signalkette (Standard 1 Stunde). |

### Hinweise

- Die angepasste Pip-Größe folgt der MetaTrader-Konvention: Instrumente mit 3 oder 5 Dezimalstellen werden mit 10 multipliziert.
- Parabolic SAR arbeitet mit Beschleunigungsschritt `0.055` und Maximum `0.21`, entsprechend den Expertenberater-Standardwerten.
- Der Port behält den ursprünglichen Geldmanagement-Stil (Volumen-Stacking) bei, aggregiert das Exposure jedoch in einer einzigen StockSharp-Position.
