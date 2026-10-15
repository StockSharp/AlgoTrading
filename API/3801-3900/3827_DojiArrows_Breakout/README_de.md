# Doji-Pfeile-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Doji Arrows Strategy** ist eine StockSharp-Portierung des MetaTrader-Expertenberaters `Doji_arrows_expert1.mq4`. Die Handelsidee besteht darin, eine neutrale Doji-Kerze zu erkennen und den Ausbruch, der auf den nächsten Balken folgt, sofort zu handeln. Wenn der Markt eine sehr kleine Körperkerze druckt (eröffnet ≈ geschlossen) und die nachfolgende Kerze über dem Doji-Hoch oder -Tief schließt, interpretiert die Strategie die Bewegung als einen Richtungsausbruch und tritt in diese Richtung ein.

## Handelslogik
- **Signalerkennungsfenster** – die Strategie puffert kontinuierlich die beiden zuvor abgeschlossenen Kerzen. Die älteste Kerze muss ein Doji sein, während die neuere Kerze den Ausbruch bestätigt.
- **Doji-Definition** – eine Kerze gilt als Doji, wenn die absolute Differenz zwischen Eröffnung und Schluss kleiner oder gleich `DojiBodyThresholdSteps * PriceStep` ist. Mit dem Standardschwellenwert von 1 Schritt darf der Balken höchstens um einen Tick abweichen.
- **Ausbruchsbestätigung** –
  - Langes Setup: Die dem Doji folgende Kerze schließt über dem Doji-Hoch plus dem optionalen `BreakoutBufferSteps`-Filter.
  - Kurzes Setup: Die Kerze, die dem Doji folgt, schließt unter dem Doji-Tief abzüglich desselben Puffers.
- **Single-Shot-Signalisierung** – die Strategie merkt sich, ob der vorherige Balken bereits ein Long- oder Short-Signal ausgelöst hat und reagiert nur auf einen erneuten Ausbruch. Dieses Verhalten spiegelt den ursprünglichen Experten wider, der einen Pfeil pro Breakout-Sequenz generierte.
- **Auftragsausführung** –
  - Wenn ein Ausbruch gegen eine bestehende Gegenposition auftritt, schließt die Strategie diese zunächst und geht dann mit einem Volumen von `Volume + |Position|` in die neue Richtung ein, um den neuen Handel sowohl umzudrehen als auch zu eröffnen.
  - Im neutralen Zustand eröffnet es eine Marktorder in Ausbruchsrichtung.

## Risikomanagement
- **Anfänglicher Stop-Loss** – nach jedem Einstieg setzt die Strategie ein internes Schutzniveau `InitialStopSteps * PriceStep` vom Erfüllungspreis entfernt.
- **Fester Take-Profit** – verlässt einen Teil oder die gesamte Position, wenn der Preis `TakeProfitSteps * PriceStep` vom Einstiegspunkt aus erreicht.
- **Trailing Stop** – sobald sich der Handel mehr als `TrailingStopSteps * PriceStep` zu Gunsten bewegt, wird das Stop-Level Kerze für Kerze nachgezogen, wodurch Gewinne gesichert werden, während die Bewegung weiterlaufen kann.
- Alle Schutzberechnungen werden in nativen Preisschritten durchgeführt, wodurch die Logik instrumentenunabhängig ist.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Zu analysierender Kerzentyp/Zeitrahmen. | Zeitrahmen von 5 Minuten |
| `DojiBodyThresholdSteps` | Maximaler Doji-Körper, ausgedrückt in Preisschritten. | 1 |
| `BreakoutBufferSteps` | Zusätzlicher Filter über/unter dem Doji-Extrem, bevor ein Ausbruch akzeptiert wird. | 0 |
| `InitialStopSteps` | Anfänglicher Stop-Loss-Abstand vom Einstieg in Schritten. | 20 |
| `TakeProfitSteps` | Take-Profit-Distanz ab Einstieg in Schritten. | 25 |
| `TrailingStopSteps` | Die Trailing-Stop-Distanz wird beibehalten, sobald der Trade profitabel ist. | 10 |

Alle Parameter werden über `StrategyParam<T>` verfügbar gemacht, wodurch sie in der Benutzeroberfläche sichtbar und für die Optimierung bereit sind.

## Hinweise zur Implementierung
- Der Kurs basiert auf dem High-Level-Kerzenabonnement API (`SubscribeCandles().Bind(...)`), um mit den Best Practices des Frameworks synchron zu bleiben.
- Der Status zwischen Aufrufen wird mit `_previousCandle` und `_twoCandlesAgo` beibehalten, um sicherzustellen, dass nur fertige Kerzen an der Entscheidungsfindung beteiligt sind.
- Schutzniveaus werden für Long- und Short-Positionen getrennt gespeichert und zurückgesetzt, wenn Positionen geschlossen werden oder die Marktdaten nicht ausreichen.
- Protokollierungsanweisungen bieten Einblick in Signalerkennung, Stop-Loss- und Take-Profit-Ereignisse und vereinfachen das Debuggen während Backtests.

## Anwendungstipps
1. Überprüfen Sie die Standard-Tick-Schwellenwerte für jedes Instrument: Erhöhen Sie `DojiBodyThresholdSteps` für volatile Märkte, in denen genaue Doji-Abdrücke selten sind.
2. Optimieren Sie `BreakoutBufferSteps`, um kleine gefälschte Ausbrüche herauszufiltern, wenn Spreads oder Rauschen erheblich sind.
3. Kombinieren Sie die Strategie mit externen Risikoüberlagerungen (Portfoliostopp, Handelssitzungsfilter), wenn Sie sie auf mehreren Symbolen gleichzeitig einsetzen.
4. Da Signale auf abgeschlossenen Kerzen basieren, wählen Sie einen Kerzentyp, der mit Ihrem gewünschten Handelshorizont kompatibel ist (z. B. 1 Minute für Scalping, 15 Minuten für Swing-Einstiege).
