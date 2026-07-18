# ChandelExit Wiedereinstiegs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den MetaTrader-Experten "Exp_ChandelExitSign_ReOpen" zur StockSharp High-Level-API. Sie handelt Ausbrüche mit den Chandelier-Exit-Bändern und öffnet Positionen automatisch erneut, wenn der Trend anhält. Das System reagiert auf Indikatorsignale, die auf einem konfigurierbaren höheren Zeitrahmen berechnet werden, und verwaltet dabei das Risiko mit ATR-basierten Stops und optionalen Take-Profit-Niveaus.

Die Kernidee besteht darin, den Chandelier Exit sowohl als Trendfilter als auch als dynamische Trailing-Barriere zu verwenden. Wenn das untere Band das obere Band kreuzt, wird ein bullischer Impuls erkannt; wenn das Gegenteil geschieht, erscheint ein bearischer Impuls. Die Strategie kann symmetrisch auf Long- und Short-Seiten arbeiten, und jedes Signal kann einzeln über Parameter aktiviert oder deaktiviert werden. Nach dem Einstieg muss sich der Preis um eine Anzahl von Kursschritten (`PriceStepPoints`) vorwärts bewegen, bevor eine Zusatzorder erlaubt ist. Die Zusätze imitieren das ursprüngliche Expertenverhaltensberater-Verhalten und sind durch `MaxAdditions` begrenzt, um unkontrollierte Positionsgrößen zu verhindern.

## Handelslogik

- **Signalberechnung**
  - `RangePeriod` Bars (verschoben um `Shift`) definieren das höchste Hoch und tiefste Tief, das von den Chandelier-Exit-Bändern verwendet wird.
  - `AtrPeriod` zusammen mit `AtrMultiplier` erzeugen einen Volatilitätspuffer, der die Ausstiegsbänder vom Preis wegschiebt.
  - `SignalBar` (Standard 1) verzögert die Ausführung, sodass die Strategie auf der vorherigen abgeschlossenen Kerze agiert und die MT5-Implementierung repliziert.
- **Einstiege**
  - **Long**: ausgelöst, wenn das untere Band das obere Band kreuzt (`IsUpSignal`). Erfordert `EnableBuyEntries = true`. Wenn eine Short-Position besteht, versucht die Strategie zunächst, sie zu schließen, wenn `EnableSellExits = true`.
  - **Short**: ausgelöst, wenn die Bänder in der entgegengesetzten Richtung kreuzen (`IsDownSignal`) und `EnableSellEntries = true`. Bestehende Longs werden nur geschlossen, wenn `EnableBuyExits = true`.
- **Ausstiege**
  - **Long**-Positionen schließen bei bearischen Signalen, wenn `EnableBuyExits = true`, oder wenn Schutz-Stops/-Targets erreicht werden.
  - **Short**-Positionen schließen bei bullischen Signalen, wenn `EnableSellExits = true`, oder durch Schutzniveaus.
  - Die Strategie scannt auch ältere Indikatorwerte, wenn sowohl Ein- als auch Ausstieg-Schalter aktiviert sind, um sicherzustellen, dass ein Schließsignal verfügbar ist, auch wenn die jüngste Kerze nur einen Einstieg erzeugte.
- **Wiedereinstieg / Aufstockung**
  - Nach jedem Einstieg wird der letzte Füllpreis gespeichert. Wenn sich der Preis um mindestens `PriceStepPoints * PriceStep` in Gunst des Trades bewegt, wird eine zusätzliche Order der Größe `Volume` gesendet, bis zu `MaxAdditions` mal.
  - Jede Aufstockung setzt die Stop-/Take-Berechnungen auf den neuesten Füllwert zurück, damit der Schutz nahe der neuesten Exposition bleibt.
- **Risikomanagement**
  - `StopLossPoints` und `TakeProfitPoints` drücken Abstände in Kursschritten vom letzten Füllwert aus. Stops und Targets sind optional; auf null setzen zum Deaktivieren.
  - Alle Schutzprüfungen laufen bei jeder abgeschlossenen Kerze. Wenn der Preis intrabar einen Stop oder Target verletzt, wird die Position zu Marktpreisen geschlossen.

## Standardparameter

| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `CandleType` | `TimeSpan.FromHours(4).TimeFrame()` | Zeitrahmen für Indikatorberechnungen. |
| `RangePeriod` | 15 | Beobachtungsfenster für das höchste Hoch / tiefste Tief. |
| `Shift` | 1 | Anzahl der kürzlichen Bars, die vor der Bereichsberechnung übersprungen werden. |
| `AtrPeriod` | 14 | ATR-Länge für den Volatilitätspuffer. |
| `AtrMultiplier` | 4 | ATR-Multiplikator, der auf den Puffer angewendet wird. |
| `SignalBar` | 1 | Wie viele abgeschlossene Bars zurück das Signal gelesen wird. |
| `PriceStepPoints` | 300 | Minimale günstige Bewegung in Kursschritten vor Aufstockung. |
| `MaxAdditions` | 10 | Maximale Anzahl von Zusatzorders nach dem Ersteinstieg. |
| `StopLossPoints` | 1000 | Stop-Loss-Abstand in Kursschritten. |
| `TakeProfitPoints` | 2000 | Take-Profit-Abstand in Kursschritten. |
| `EnableBuyEntries` / `EnableSellEntries` | `true` | Öffnen von Long/Short-Trades bei Signalen erlauben. |
| `EnableBuyExits` / `EnableSellExits` | `true` | Schließen von Long/Short-Trades bei entgegengesetzten Signalen erlauben. |

## Praktische Hinweise

- Die Strategie verlässt sich auf `Volume` zur Definition der Basisordergröße. Zusatztrades verwenden dieselbe Größe. `Volume` oder `MaxAdditions` anpassen, um Risikolimits einzuhalten.
- Da Wiedereinstiege eine in Kursschritten ausgedrückte Bewegung erfordern, sicherstellen, dass die Wertpapier-Metadaten (`PriceStep`) korrekt konfiguriert sind. Instrumente mit großen Punktwerten benötigen möglicherweise andere Standardwerte.
- `SignalBar` kann auf null gesetzt werden, um auf der zuletzt abgeschlossenen Kerze zu agieren, aber der ursprüngliche Experte verwendete eine Ein-Bar-Verzögerung, um nicht auf der Kerze zu handeln, die das Signal generiert hat.
- Die Strategie auf einer Symbol-/Portfolio-Kombination starten, die sowohl Long- als auch Short-Trades unterstützt. Die integrierten Parameter-Schalter verwenden, um sie auf eine Richtung zu beschränken, falls nötig.
- Chart-Helfer (`DrawCandles`, `DrawIndicator`, `DrawOwnTrades`) aktivieren sich automatisch, wenn ein Chartbereich verfügbar ist, was die Visualisierung von Bändern und Füllungen erleichtert.

## Beispiel-Workflow

1. Auf einen bullischen Kreuzer warten: das untere Band bricht auf der höheren Zeitrahmen-Kerze über das obere Band.
2. Wenn keine Position besteht und Long-Einstiege aktiviert sind, eine Markt-Kauforder der Größe `Volume` platzieren. Stops und Targets werden relativ zum Füllpreis gesetzt.
3. Wenn sich der Preis um mindestens `PriceStepPoints` * `PriceStep` erhöht, eine weitere Kauforder senden (dabei `MaxAdditions` respektieren).
4. Den gesamten Long schließen, wenn ein bearisches Signal erscheint, der Stop-Loss erreicht wird oder der Take-Profit erreicht wird. Der Prozess ist für Short-Trades gespiegelt.

Diese Dokumentation spiegelt die ursprüngliche MT5-Strategie wider, während sie StockSharp-Konventionen wie Strategieparameter, High-Level-Kerzenabonnements und explizites Positionsmanagement übernimmt.
