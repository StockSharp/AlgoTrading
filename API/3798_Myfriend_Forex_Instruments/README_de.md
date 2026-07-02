# Myfriend Forex Instruments-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Myfriend Forex Instruments Strategy** reproduziert den Experten von „MyFriend“ MetaTrader aus dem Jahr 2006. Es handelt EUR/USD auf 30-Minuten-Kerzen durch die Kombination von täglichen Pivot-Levels, Donchian-Kanalerweiterungen und einem kurzen vs. langen Momentum-Spread, gemessen an den Schlusskursen. Das System sucht nach Kerzen, die den täglichen Pivot mit einem breiten realen Körper durchdringen oder nach abrupten Donchian-Breitenerweiterungen. Wenn einer dieser Impulse mit dem Intraday-Momentum-Bias übereinstimmt, eröffnet die Strategie eine einzelne Position mit vordefinierten Schutzniveaus.

## Handelslogik

1. **Tägliche Pivot-Karte** – Die Höchst-, Tiefst- und Schlusskurse des Vortages bilden die klassische Pivot-Leiter (`Pivot`, `R1`, `S1`). Diese Niveaus bleiben während der gesamten Handelssitzung unverändert und definieren die erwartete Handelsspanne.
2. **Momentum-Puls** – Zwei einfache gleitende Durchschnitte des Schlusskurses (3 und 9 Perioden) bilden einen Short/Long-Momentum-Spread. Der Spread wird mit 1000 multipliziert, um die MetaTrader „MP“-Berechnung nachzuahmen und zu bestimmen, ob bullischer oder bärischer Druck dominiert.
3. **Breakout-Filter**
   - *Pivot-Schub*: Nachdem eine Kerze mit einem Körper von mehr als 12 Punkten über den Pivot schließt und die nächste Kerze in die gleiche Richtung schließt, markiert die Strategie einen potenziellen Handel.
   - *Donchian-Erweiterung*: Wenn sich der 16-Perioden-Donchian-Kanal über den `R1 - S1`-Bereich hinaus erweitert und seine Richtung mit der Preisbewegung übereinstimmt, wird das Signal ebenfalls ausgelöst.
4. **Auftragsverwaltung** – Es ist jeweils nur eine Position zulässig. Bei Long-Einstiegen wird das vorherige Kerzentief abzüglich eines Puffers als Stop und ein fester Take-Profit von 70 Punkten verwendet. Short-Einträge spiegeln diese Logik mit dem vorherigen Hoch plus einem Puffer wider.
5. **Ausstiegstaktik**
   - *Zeitbasierter Ausstieg*: Wenn sich der letzte geschlossene Balken zwischen der 3. und 4. Kerze nach dem Einstieg um 3 Punkte gegenüber der Position bewegt, wird der Handel vorzeitig geschlossen.
   - *Trailing Stop*: Sobald der offene Gewinn 5 Punkte überschreitet und sich die Donchian-Grenze weiterhin zu Gunsten des Handels bewegt, wird der Stop entlang des Kanals plus/minus eines 1-Punkt-Puffers nachgezogen.
   - *Harte Ziele*: Wenn der Preis den berechneten Stop oder Take-Profit berührt, wird die Position sofort geschlossen.

## Parameter

| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `BaseVolume` | Auftragsvolumen, das für jeden neuen Trade verwendet wird. | `1` |
| `TakeProfitPoints` | Abstand des Take-Profits vom Einstieg in MetaTrader Punkten. | `70` |
| `StopLossBufferPoints` | Zusätzlicher Puffer, der über das vorherige Kerzenextremum hinaus für den Schutzstopp hinzugefügt wurde. | `13` |
| `ChannelPeriod` | Donchian Kanalzeitraum, der für Breitenerweiterungstests und Trailing verwendet wird. | `16` |
| `UseTrailingStop` | Aktiviert oder deaktiviert den Donchian-basierten Trailing Stop. | `true` |
| `TrailingStartPoints` | Erforderlicher offener Gewinn (Punkte), bevor der Trailing Stop enger werden kann. | `5` |
| `TrailingBufferPoints` | Puffer (Punkte), der beim Trailing auf die Donchian-Grenze angewendet wird. | `1` |
| `UseTimeClose` | Aktiviert den 3–4-Kerzen-Ablehnungsausgang. | `true` |
| `CandleType` | Primärer Kerzentyp (Standardzeitrahmen 30 Minuten). | `M30` |
| `DailyCandleType` | Täglicher Kerzentyp, der zur Wiederherstellung der Pivot-Levels verwendet wird. | `D1` |

## Notizen

- Die Strategie ist für EUR/USD und 30-Minuten-Kerzen konzipiert und spiegelt den ursprünglichen Experten wider. Unterschiedliche Instrumente oder Zeitrahmen erfordern möglicherweise Parameteranpassungen.
- Punktbasierte Parameter basieren auf dem `PriceStep` des Instruments. Ist dies nicht durch die Marktdaten gegeben, greift die Strategie auf eine Stückpreiserhöhung zurück.
- Es werden nur abgeschlossene Kerzen verarbeitet, was dem MetaTrader-Verhalten des Quellalgorithmus entspricht.
