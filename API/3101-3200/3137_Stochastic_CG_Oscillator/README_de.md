# Stochastic CG Oscillator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den MetaTrader 5-Expertenberater **Exp_StochasticCGOscillator** nach StockSharp. Die Konvertierung behält die ursprüngliche Logik des Stochastic Center of Gravity Oszillators bei, baut das Trigger-Linien-Smoothing neu auf und führt Trades über die StockSharp High-Level-Strategie-API aus.

## Funktionsweise

1. **Indikator-Pipeline** – jede abgeschlossene Kerze von `CandleType` speist den benutzerdefinierten Stochastic CG Oszillator. Medianpreise treiben einen Center-of-Gravity-Loop an, Werte werden über die letzten `Length` Balken normalisiert, und ein gewichtetes rollierendes Fenster erzeugt die Oszillatorlinie. Die Triggerlinie wird durch dasselbe `0.96 * (previous + 0.02)`-Smoothing recreated, das der EA anwendet.
2. **Signalabtastung** – die Strategie untersucht zwei historische Lesungen, die durch `SignalBar` getrennt sind. Ein Kauf wird vorbereitet, wenn die ältere Lesung (Shift `SignalBar + 1`) über dem Trigger liegt, während die neuere (Shift `SignalBar`) darunter kreuzt. Shorts spiegeln die Logik in entgegengesetzter Richtung.
3. **Positionsmanagement** – Long-Positionen werden geschlossen, sobald die ältere Lesung unter den Trigger fällt, während Short-Positionen aussteigen, wenn die ältere Lesung über ihn steigt. Wenn ein neuer Einstieg auf der gegenüberliegenden Seite erscheint, wird die aktuelle Position vor dem Umkehrauftrag geschlossen.
4. **Risikohandling** – optionale Stop-Loss- und Take-Profit-Abstände werden in Instrument-Steps ausgedrückt und auf dem Schlusskurs jeder verarbeiteten Kerze bewertet. Sie spiegeln die Schutz-Inputs des EA wider, ohne auf Pending-Orders zu setzen.
5. **Aufwärmkontrolle** – die Strategie wartet, bis der Indikator vollständig initialisiert ist (genug Geschichte für den CG-Loop und den Vier-Wert-Smoothing-Buffer), bevor Signale emittiert werden, was deterministische Backtests garantiert.

## Risikomanagement & Positionsgröße

- **Stops/Ziele** – `StopLossPoints` und `TakeProfitPoints` werden mithilfe von `Security.PriceStep` in absolute Abstände umgerechnet. Ein Wert von `0` deaktiviert das jeweilige Limit.
- **Einzelne aktive Position** – der Algorithmus hält niemals gleichzeitig Long- und Short-Exposition. Gegensignale lösen ein explizites Schließen vor dem Einstieg in die neue Richtung aus.
- **Positionsgröße** – `SizingMode = FixedVolume` handelt immer mit `FixedVolume`. `SizingMode = PortfolioShare` konvertiert `DepositShare` des Portfoliowerts in Kontrakte mithilfe des letzten Schlusskurses und `Security.VolumeStep`.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `CandleType` | Zeitrahmen für Kerzen und Indikatorberechnungen. |
| `Length` | Periode des Stochastic CG Oszillators (beeinflusst CG- und Normalisierungsfenster). |
| `SignalBar` | Anzahl der geschlossenen Kerzen zurück für die Signalbewertung (`1` reproduziert den EA-Standard). |
| `AllowLongEntry` / `AllowShortEntry` | Schaltet Long/Short-Eintritte ein/aus. |
| `AllowLongExit` / `AllowShortExit` | Schaltet automatische Ausstiege für Long/Short-Positionen ein/aus. |
| `StopLossPoints` / `TakeProfitPoints` | Schutzabstände in Preisschritten. Auf `0` setzen zum Deaktivieren. |
| `FixedVolume` | Ordergröße beim Fixvolumen-Modus. |
| `DepositShare` | Portfolioanteil für anteilsbasiertes Sizing. |
| `SizingMode` | Wählt zwischen festem Volumen und anteilsbasierter Positionsgröße. |

## Nutzungshinweise

- Richten Sie `CandleType` am Zeitrahmen des ursprünglichen Indikators aus (H8 in der MQL-Version). Größere `SignalBar`-Werte erfordern eine längere Aufwärmzeit, da der Indikator-Geschichtspuffer den Shift abdecken muss.
- Stops und Ziele wirken auf Kerzen-Schlusskurse; sie sind keine Intrabar-Orders. Passen Sie die Punktwerte an die Tick-Größe des Instruments an.
- Wenn `PortfolioShare`-Sizing aktiviert ist, stellen Sie sicher, dass die Portfoliobewertung verfügbar ist; andernfalls fällt die Strategie auf das feste Volumen zurück.
- Der Indikator gibt Werte im Bereich `[-1, 1]` aus wie die ursprüngliche Implementierung, damit vertraute schwellenwertbasierte Filter wiederverwendet werden können.

## Unterschiede zum Original-EA

- Marktorders werden sofort ohne den `Deviation_`-Parameter gesendet; die Slippage-Behandlung wird an die StockSharp-Ausführungsschicht delegiert.
- Das Money-Management ist auf zwei Modi vereinfacht (`FixedVolume` und `PortfolioShare`). Die zusätzlichen margin-basierten Sizing-Optionen des EA werden nicht reproduziert.
- Zeitstempel für Pending-Orders (`UpSignalTime` / `DnSignalTime`) sind unnötig, da StockSharp-Strategien auf abgeschlossenen Kerzen arbeiten und synchron ausführen.
