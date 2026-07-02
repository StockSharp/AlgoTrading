# Sechs-Indikatoren-Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert den MetaTrader 4-Expertenberater **6xIndics_M** unter Verwendung des StockSharp-High-Level-API. Es mischt sechs Impulseingänge, die von Bill Williams' Accelerator Oscillator (AC) und Awesome Oscillator (AO) abgeleitet sind, und leitet sie durch eine wählbare Entscheidungsmatrix. Als Endfilter fungiert ein langsamer stochastischer Oszillator. Es ist jeweils nur eine Position offen; Martingale-Geldmanagement, Stop-Loss/Take-Profit und optionale Trailing-Stops emulieren das ursprüngliche Verhalten.

## Wie die Strategie funktioniert

1. **Datenabonnement** – die Strategie abonniert die konfigurierte Kerzenserie (`CandleType`, standardmäßige 1-Stunden-Balken).
2. **Indikatoren**
   - Awesome Oscillator berechnet die Differenz zwischen den einfachen gleitenden 5- und 34-Perioden-Durchschnittswerten des Medianpreises.
   - Ein einfacher gleitender 5-Perioden-Durchschnitt des AO ergibt die Accelerator Oscillator-Werte (AC).
   - Ein Stochastic-Oszillator mit den Parametern 5/5/5 liefert die %K-Linie, die um eine geschlossene Kerze verzögert ist (MT4-Verschiebung = 1).
3. **Sechs Indikatorplätze** – jede fertige Kerze füllt die folgenden Puffer:
   - Slot 0: AC-Wert um 1 Kerze verschoben (`AC[1]`).
   - Slot 1: AC-Wert um 10 Kerzen verschoben (`AC[10]`).
   - Slot 2: AC-Wert um 20 Kerzen verschoben (`AC[20]`).
   - Slot 3: AO-Impuls, d. h. `AO[0] - AO[shift]`, wobei die Verschiebung konfigurierbar ist (`AoMomentumShift`).
   - Slot 4: Wechselstromimpuls `AC[0] - AC[shift #1]` (`AcPrimaryShift`).
   - Slot 5: AC-Impuls `AC[0] - AC[shift #2]` (`AcSecondaryShift`).
4. **Auswählbare Signalmatrix** – Parameter `FirstSourceIndex` … `SixthSourceIndex` wählen aus, welcher Slot die sechs booleschen Prüfungen speist, die ursprünglich `k`, `u`, `t`, `e`, `r`, `o` genannt wurden. Dieselben Indizes werden sowohl zum Generieren von Einträgen als auch zum Schließen von Geschäften wiederverwendet, wenn `CloseOnReverseSignal` aktiviert ist.
5. **Eingabelogik**
   - **Kaufen**, wenn die ausgewählten Slots Folgendes erfüllen: `A > 0`, `B > 0.0001 × Sensitivity`, `C > 0.0002 × Sensitivity`, `D < 0`, `E < 0.0001 × Sensitivity`, `F < 0.0002 × Sensitivity` und der vorherige stochastische %K unter 15 liegt.
   - **Verkaufen**, wenn `A < 0`, `B < 0.0001 × Sensitivity`, `C < 0.0002 × Sensitivity`, `D > 0`, `E > 0.0001 × Sensitivity`, `F > 0.0002 × Sensitivity` und der vorherige stochastische %K über 85 liegt.
6. **Positionsverwaltung**
   - Es ist nur eine Position zulässig. Wenn ein Trade offen ist, überspringt die Strategie neue Einträge und spiegelt damit den MT4-Experten wider.
   - Stop-Loss- und Take-Profit-Level werden mithilfe der Tick-Größe des Instruments von Pips in absolute Preise umgewandelt (genau wie `Point` in MT4 funktioniert).
   - Der optionale Trailing Stop reproduziert das ursprüngliche Verhalten: Er wird aktiviert, sobald sich der Preis um `TrailingStopPips` über den Einstieg hinaus bewegt (und, wenn `RequireProfitForTrailing` wahr ist, zusätzlich um `LockProfitPips`). Der Stop folgt dem Preis nur in die günstige Richtung.
   - `CloseOnReverseSignal` schließt einen profitablen Handel ab, wenn das entgegengesetzte Signal auftritt (Gebot über dem Einstiegspunkt für Long-Positionen, Briefkurs unten für Short-Positionen).
7. **Martingale-Größenbestimmung** – wenn aktiviert, entspricht das nächste Ordervolumen dem vorherigen Handelsvolumen multipliziert mit `(TakeProfitPips + StopLossPips) / TakeProfitPips`, wenn ein Handel mit einem Verlust oder Break-Even schließt. Gewinnende Trades setzen die Größe auf den Basiswert `Volume` zurück.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `AllowBuy`, `AllowSell` | Aktivieren oder deaktivieren Sie lange/kurze Einträge. | `true` |
| `CloseOnReverseSignal` | Schließen Sie die aktuelle Position, wenn ein entgegengesetztes Signal erscheint, während der Handel profitabel ist. | `false` |
| `FirstSourceIndex` … `SixthSourceIndex` | Wählen Sie aus, welcher der sechs Indikatorsteckplätze jede logische Prüfung speist. Werte außerhalb von 0–5 werden geklemmt. | `1,2,3,4,3,4` |
| `AoMomentumShift` | Anzahl der Balken zwischen dem aktuellen AO-Wert und dem in Slot 3 verwendeten Vergleich. | `10` |
| `AcPrimaryShift`, `AcSecondaryShift` | Anzahl der Balken zwischen dem aktuellen AC-Wert und den Vergleichen für die Steckplätze 4 und 5. | `10` / `10` |
| `SensitivityMultiplier` | Multiplikator, der auf die bei den Slot-Prüfungen verwendeten Schwellenwerte von 0,0001 und 0,0002 angewendet wird. | `1.0` |
| `TakeProfitPips`, `StopLossPips` | Ausgangsentfernungen, ausgedrückt in Pips im MetaTrader-Stil (sie werden entsprechend der Tick-Größe neu skaliert). | `300` / `300` |
| `UseTrailingStop` | Aktivieren Sie die Trailing-Stop-Logik. | `false` |
| `TrailingStopPips` | Abstand zwischen Preis und Trailing Stop, in Pips. | `300` |
| `RequireProfitForTrailing` | Wenn aktiviert, wird der Trailing Stop erst aktiviert, nachdem der Trade einen zusätzlichen `LockProfitPips` erzielt hat. | `false` |
| `LockProfitPips` | Zusätzlicher Gewinn (in Pips), der gesperrt werden muss, bevor sich der Trailing Stop in Bewegung setzt. | `300` |
| `Volume` | Basisbestellgröße. | `0.1` |
| `UseMartingale` | Aktivieren Sie die Größenbestimmung der Martingalposition. | `false` |
| `CandleType` | Für alle Berechnungen verwendete Kerzenreihe. | `TimeSpan.FromHours(1)` |

## Hinweise und Best Practices

- Jede Kerze wird erst verarbeitet, nachdem sie beendet ist, daher ahmen die Signale den MT4-Experten nach, der einmal pro Balken ausgeführt wird (`prevtime`-Schutz im Originalcode).
- Die Strategie speichert nur den erforderlichen Verlauf (bis zu 256 Balken), um die MT4-Verschiebungsberechnungen zu reproduzieren, ohne `GetValue()` für Indikatoren aufzurufen, und erfüllt so die Projektrichtlinien.
- Trailing- und Stop/Limit-Exits werden auf Kerzenhochs/-tiefs simuliert. In einer Live-Umgebung sollten Sie für eine garantierte Ausführung echte Stop-Orders verwenden.
- Bei der Dimensionierung von Martingale werden die Limits `VolumeStep`, `MinVolume` und `MaxVolume` des Instruments verwendet, um die Volumina innerhalb der Brokerregeln zu halten.
- Wenn `AllowBuy` oder `AllowSell` deaktiviert ist, werden die entsprechenden Signale ignoriert, aber das entgegengesetzte Signal kann weiterhin für `CloseOnReverseSignal` verwendet werden.

## Unterschiede zum MT4-Experten

- Indikatorberechnungen verwenden die integrierten Awesome Oscillator- und SMA-Klassen von StockSharp; Es ist keine manuelle Pufferverwaltung erforderlich.
- Alle Trades werden über Marktaufträge (`BuyMarket` / `SellMarket`) und Exits über `ClosePosition()` ausgeführt, während die MT4-Version explizite `OrderSend`/`OrderClose`-Anfragen sendete.
- Bei der Losgröße wird die Granularität des Austauschvolumens berücksichtigt, indem auf `VolumeStep` gerundet und auf `[MinVolume, MaxVolume]` geklammert wird.
- Diagrammhelfer (`DrawCandles`, `DrawIndicator`, `DrawOwnTrades`) werden zur visuellen Überprüfung hinzugefügt, wenn ein Diagramm verfügbar ist.
