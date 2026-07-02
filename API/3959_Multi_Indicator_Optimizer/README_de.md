# Multi-Indikator-Optimierungsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie repliziert die Abstimmungslogik des MetaTrader-Experten **MultiIndicatorOptimizer** auf der hohen Ebene StockSharp API. Fünf klassische Oszillatoren bewerten die fertige Kerze und geben eine gewichtete Stimme für die aggregierte Stimmung ab. Der resultierende Wert wird dann mit benutzerdefinierten Schwellenwerten verglichen, um zu entscheiden, ob die Strategie eine Long-Position, eine Short-Position oder eine Abflachung einer bestehenden Position eingehen soll.

## Handelslogik

1. **MACD-Block** – prüft das Vorzeichen des Histogramms und die Beziehung zwischen der Haupt- und der Signallinie (beide stammen aus dem vorherigen fertigen Balken). Die Summe dieser beiden Signale wird gemittelt und mit `MacdWeight` multipliziert.
2. **Toller Oszillatorblock** – misst, ob der Oszillator über oder unter der Nulllinie liegt und ob sich die Dynamik im Vergleich zum Balken davor verbessert. Die durchschnittliche Abstimmung wird um `AoWeight` skaliert.
3. **OsMA-Block** – prüft das Vorzeichen des MACD-Histogramms der vorherigen Kerze und wendet `OsmaWeight` an.
4. **Williams %R-Block** – reagiert auf überverkaufte/überkaufte Kreuzungen, die durch `WilliamsLowerLevel` und `WilliamsUpperLevel` definiert werden. Ein Übergang vom unteren Band nach oben stimmt als bullisch, während ein Übergang vom oberen Band nach unten als bärisch gilt. Das Ergebnis wird mit `WilliamsWeight` multipliziert.
5. **Stochastic-Block** – kombiniert zwei Prüfungen: eine Schwellenwertüberschreitung von %K vs. `StochasticLowerLevel`/`StochasticUpperLevel` und eine %K/%D-Beziehung. Der Durchschnitt beider Teilsignale wird mit `StochasticWeight` multipliziert.

Die aggregierte Punktzahl wird in der Spalte `Signal` der Protokolle gespeichert und über das Feld `_lastSignal` innerhalb der Strategie angezeigt. Die Handelsmaschine wertet den Score wie folgt aus:

- `signal >= EntryThreshold`: Jede Short-Position schließen und eine Long-Position eröffnen/beibehalten.
- `signal <= -EntryThreshold`: Schließen Sie eine beliebige Long-Position und eröffnen/behalten Sie eine Short-Position.
- `abs(signal) <= ExitThreshold`: Die Position flach halten, um den Handel unter neutralen Marktbedingungen zu vermeiden.

Alle Berechnungen basieren auf der zuvor fertigen Kerze, um mit der ursprünglichen MT4-Implementierung übereinzustimmen, die indizierte Indikatorwerte (`shift = 1/2`) verwendet hat.

## Parameter

| Parameter | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Primärer Zeitrahmen für alle Indikatorberechnungen. | H1-Kerzen |
| `MacdFast` / `MacdSlow` / `MacdSignal` | EMA Längen für den Block MACD. | 26.12.9 |
| `MacdWeight` | Abstimmungsmultiplikator für den Block MACD. Negative Werte kehren die Abstimmung um. | 1 |
| `AoShortPeriod` / `AoLongPeriod` | Vom Awesome Oscillator verwendete gleitende Durchschnittslängen. | 5 / 34 |
| `AoWeight` | Stimmenmultiplikator für den Awesome-Block. | 1 |
| `OsmaFastPeriod` / `OsmaSlowPeriod` / `OsmaSignalPeriod` | MACD Einstellungen wurden zum Erstellen des OsMA-Histogramms wiederverwendet. | 26.12.9 |
| `OsmaWeight` | Stimmenmultiplikator für den OsMA-Block. | 1 |
| `WilliamsPeriod` | Lookback-Länge für Williams %R. | 14 |
| `WilliamsLowerLevel` / `WilliamsUpperLevel` | Grenzen für Überverkauft/Überkauft (in Prozent). | -80 / -20 |
| `WilliamsWeight` | Abstimmungsmultiplikator für den Block Williams. | 1 |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | Perioden für den Stochastic-Oszillator und seine interne Glättung. | 5 / 3 / 3 |
| `StochasticLowerLevel` / `StochasticUpperLevel` | Überverkaufte/überkaufte Schwellenwerte für %K. | 20 / 80 |
| `StochasticWeight` | Abstimmungsmultiplikator für den Block Stochastic. | 1 |
| `EntryThreshold` | Die absolute Mindeststimme ist erforderlich, um eine Position zu eröffnen oder umzukehren. | 0,5 |
| `ExitThreshold` | Breite der neutralen Zone. Positionen werden geschlossen, wenn der Absolutwert des Signals unter diesen Wert fällt. | 0,1 |

Alle Gewichte können negativ sein, um den Beitrag eines Blocks zu unterdrücken oder umzukehren, was bei Optimierungsläufen praktisch ist.

## Notizen

- Die Strategie basiert ausschließlich auf den übergeordneten API: `SubscribeCandles`, Indikatorbindungen und `BuyMarket`/`SellMarket`-Helfern.
- Bei jeder Indikatorabstimmung werden nur abgeschlossene Kerzen verwendet, um sicherzustellen, dass Entscheidungen auf bestätigten Daten basieren.
- Die Positionsgröße wird durch die Basiseigenschaft `Volume` von `Strategy` gesteuert. Schutzaufträge (Stop-Loss/Take-Profit) können bei Bedarf extern über `StartProtection` hinzugefügt werden.
- Um die weitere Pflege zu vereinfachen, werden auf Wunsch ausführliche Kommentare in englischer Sprache bereitgestellt.
