# Super Simple RSI Engulfing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den ursprünglichen SSEATwRSI MetaTrader-Expertenberater in StockSharp. Sie überwacht abgeschlossene Kerzen und berechnet einen 7-Perioden-RSI auf dem Kerzenhoch. Ein Trade wird nur ausgelöst, wenn der RSI einen Extremwert erreicht und die vorherigen zwei Bars eine saubere Engulfing-Umkehrung bilden.

Ein Long-Setup erfordert, dass der RSI über die überkaufte Schwelle steigt, während eine bearishe Kerze vollständig von der nächsten bullishen Kerze verschluckt wird. Ein Short-Setup spiegelt diese Logik mit einer überverkauften RSI-Lesart und einem bullish-zu-bearish Engulfing-Muster. Die Positionsgröße ist durch den Parameter `Volume` fixiert, aber eine entgegengesetzte Exposition wird vor dem Öffnen eines neuen Trades aufgelöst.

Sobald im Markt, überwacht die Strategie weiterhin den globalen Gewinn und Verlust. Wenn der schwebende PnL das konfigurierte Gewinnziel (in Kontowährung) erreicht oder unter den erlaubten Verlust fällt, wird die gesamte Position geschlossen. Es gibt keine zusätzlichen Trailing Stops; Trades werden ausschließlich durch die Muster-Umkehrung und die Kontoschwellen verwaltet.

## Details

- **Einstiegskriterien**:
  - **Long**: RSI auf Hochs > `OverboughtLevel` und die letzte Kerze verschluckt eine bearishe Bar von vor zwei Bars, während der Preis über der Eröffnung dieser älteren Bar schließt.
  - **Short**: RSI auf Hochs < `OversoldLevel` und die letzte Kerze verschluckt eine bullishe Bar von vor zwei Bars, während der Preis unter der Eröffnung dieser älteren Bar schließt.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Konto-PnL ≥ `ProfitGoal` → Glätten.
  - Konto-PnL ≤ `-MaxLoss` → Glätten.
  - Das entgegengesetzte Signal verrechnet automatisch die vorherige Position, wenn eine neue Order platziert wird.
- **Stops**: Währungsbasierte Take-Profit- und Max-Verlust-Prüfungen abgeleitet vom gesamten Strategie-PnL.
- **Filter**:
  - RSI auf dem Kerzenhoch berechnet, um Erschöpfungsbewegungen zu betonen.
  - Bestätigung über eine Zwei-Bar-Engulfing-Umkehrung.

## Parameter

- `Volume` = 0.1 – Ordergröße in Kontrakten. Bestehende Exposition wird vor dem Öffnen eines neuen Trades verrechnet.
- `ProfitGoal` = 190 – Währungs-Gewinnziel, das eine flache Position erzwingt, sobald es erreicht wird.
- `MaxLoss` = 10 – Maximaler erlaubter Währungsverlust, bevor die Strategie alle Positionen schließt. Die Prüfung verwendet intern `-MaxLoss`.
- `RsiPeriod` = 7 – Durchschnittslänge des RSI-Indikators.
- `RsiPrice` = High – Preisquelle für die RSI-Berechnung.
- `OverboughtLevel` = 88 – RSI-Level, das vor einer Long-Umkehrung überschritten werden muss.
- `OversoldLevel` = 37 – RSI-Level, das vor einer Short-Umkehrung unterschritten werden muss.
- `CandleType` = standardmäßig 1-Stunden-Kerzen; anpassen, um dem Zeitrahmen des ursprünglichen Charts zu entsprechen.
