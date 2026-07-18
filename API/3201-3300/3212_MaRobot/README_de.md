# MaRobot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung
- Implementiert ein bar-basiertes Moving-Average-Crossover-System, das auf einem konfigurierbaren Intraday-Zeitrahmen operiert und tägliche ADX- und RSI-Filter verwendet.
- Verwendet StockSharp-High-Level-Bindungen, um zwei einfache gleitende Durchschnitte zusammen mit `Lowest`/`Highest`-Swing-Detektoren und täglichen `AverageDirectionalIndex`- und `RelativeStrengthIndex`-Indikatoren zu berechnen.
- Recreiert die ursprüngliche MT4-Schutzlogik: Take-Profit nach Prozentsatz, swing-basierter Stop-Loss und ein optionaler Break-Even-Stop, sobald ein Mindestgewinn erreicht ist.

## Indikatoren
- `SimpleMovingAverage` (schnell und langsam) auf dem primären Zeitrahmen.
- `Lowest` / `Highest` zum Erfassen der Extrempreise der letzten `BackClose` Kerzen für die Stop-Platzierung.
- Tägliche `AverageDirectionalIndex`- und `RelativeStrengthIndex`-Werte für Trendstärke- und Momentum-Filter.

## Parameter
- `CandleType` – primärer Zeitrahmen (Standard: 15-Minuten-Kerzen).
- `FastPeriod`, `SlowPeriod` – Längen der schnellen und langsamen SMA-Linien.
- `AdxThreshold` – maximal erlaubter Wert des täglichen ADX zum Aktivieren neuer Einstiege.
- `RsiThreshold` – tägliches RSI-Niveau für Long-Einstiege (der Short-Einstieg verwendet `100 - RsiThreshold`).
- `TakeProfitRatio` – Bruchabstand zwischen Einstiegspreis und Gewinnziel.
- `StopLossPoints` – Abstand des Schutz-Stops (in Instrumentpunkten), der nach Erreichen von `ProtectThreshold` aktiviert wird.
- `ProtectThreshold` – minimale offene Gewinnquote, die den Schutz-Stop aktiviert.
- `BackClose` – Anzahl abgeschlossener Kerzen für die Berechnung des Swing-High/Low-Stops.
- `DailyAdxPeriod`, `DailyRsiPeriod` – Längen der täglichen Indikatoren.

## Trading-Regeln
1. Nur auf abgeschlossenen Kerzen arbeiten, um dem MT4-Expertenberater zu entsprechen.
2. Warten, bis alle Indikatoren vollständig gebildet sind und tägliche Werte verfügbar sind.
3. **Einstiegsfilter**:
   - Neue Positionen ablehnen, wenn der tägliche ADX `AdxThreshold` überschreitet.
   - Long-Einstieg erfordert, dass die schnelle SMA über die langsame SMA kreuzt und der tägliche RSI unter `RsiThreshold` liegt.
   - Short-Einstieg erfordert, dass die schnelle SMA unter die langsame SMA kreuzt und der tägliche RSI über `100 - RsiThreshold` liegt.
4. Beim Einstieg den Swing-Extremwert speichern (`Lowest` für Longs, `Highest` für Shorts) als manuelle Stop-Referenz.
5. **Ausstiegslogik** bei aktiver Position:
   - Bei `TakeProfitRatio` Gewinn gemessen vom gespeicherten Einstiegspreis schließen.
   - Schließen, wenn der Kerzenschluss das gespeicherte Swing-Stop-Niveau durchbricht.
   - Bei einem entgegengesetzten Moving-Average-Crossover schließen.
   - Nachdem der Gewinn `ProtectThreshold` überschreitet, einen Break-Even-Stop, der um `StopLossPoints` (auf Tick-Größe gerundet) versetzt ist, aktivieren und schließen, wenn der Preis zurückläuft.
6. Gesamten internen Zustand zurücksetzen, wenn die Nettoposition auf null zurückkehrt.

## Hinweise
- Alle Kommentare im C#-Code werden gemäß den Repository-Richtlinien auf Englisch gehalten.
- Die Strategie basiert ausschließlich auf StockSharp-High-Level-Subscriptions über `Bind` und vermeidet manuelle Indikatorbuffer.
- Die Python-Übersetzung wird gemäß den Aufgabenanweisungen absichtlich weggelassen.
