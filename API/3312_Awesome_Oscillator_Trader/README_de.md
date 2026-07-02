# Awesome-Oscillator-Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Awesome-Oscillator-Trader-Strategie ist eine direkte Konvertierung des MetaTrader-Expert-Advisors "AwesomeOscTrader". Sie kombiniert Bill Williams' Awesome Oscillator mit Bollinger-Bandbreite und Stochastic-Filtern, um Ausbrüche nach tiefen Momentum-Kontraktionen zu timen. Das System ist für stündlichen Einzelwert-Handel auf sehr liquiden FX-Paaren wie EURUSD ausgelegt und spiegelt die ursprüngliche Empfehlung wider.

Die Strategie wartet, bis der Bollinger-Band-Spread in einen konfigurierbaren Bereich eintritt und damit signalisiert, dass Volatilität kontrahiert ist, aber nicht verschwunden. Während dieses Squeeze muss das Awesome-Oscillator-Histogramm ein markantes Fünf-Bars-Umkehrmuster bilden: vier aufeinanderfolgende fallende Histogrammbars unter null, gefolgt von einer neuen Bar, die zur Aufwärtsfarbe wechselt und dennoch negativ bleibt. Wenn diese Struktur entsteht und der Stochastic-Oszillator wieder über ein überverkauftes Niveau kreuzt, eröffnet die Strategie eine Long-Position in Erwartung einer Auflösung nach oben. Das inverse Muster, vier positive steigende Histogrammbars über null und eine neue abwärts gefärbte Bar, die noch positiv ist, kombiniert mit einem Stochastic-Fall unter eine obere Schwelle, löst einen Short-Einstieg aus.

Positionen werden mit einer ATR-basierten Stop-Distanz geschützt. Auf jeder Bar liest das System den Average True Range über 3 Perioden, multipliziert ihn mit einem konfigurierbaren Faktor und wandelt das Ergebnis anhand der Tickgröße des Instruments in Pips um. Dieser Wert definiert sowohl den anfänglichen Stop-Loss als auch die Take-Profit-Ziele und reproduziert die symmetrische Ausstiegslogik der MetaTrader-Version. Ein optionaler Trailing Stop zieht das Schutzniveau enger, sobald sich der Preis um die konfigurierte Pip-Anzahl günstig bewegt, während `CloseOnReversal` Positionen schließt, wenn das entgegengesetzte Awesome-Oscillator-Muster oder ein Farbwechsel erscheint. Ein Gewinnfilter erlaubt das Schließen nur gewinnender, nur verlierender oder aller Trades bei Umkehrsignalen und repliziert das `ProfitTypeClTrd`-Verhalten des EA.

## Handelsregeln

- **Zeitrahmen:** standardmäßig 1-Stunden-Kerzen (voll konfigurierbar).
- **Filter:**
  - Die Bollinger-Bandbreite muss zwischen `BollingerSpreadLower` und `BollingerSpreadUpper` Pips liegen.
  - Stochastic %K wird für Longs gegen `StochasticLowerLevel` und für Shorts gegen `StochasticUpperLevel` geprüft.
  - Der Awesome Oscillator muss die Fünf-Bars-Umkehrstruktur bilden, wobei die jüngste Bar die Farbe wechselt, aber auf der Gegenseite von null bleibt; seine normalisierte Stärke muss `AoStrengthLimit` überschreiten.
- **Einstiege:**
  - **Long:** obige Bedingungen plus aktuelle Bar innerhalb des erlaubten Handelszeitfensters.
  - **Short:** gespiegelte Bedingungen.
- **Ausstiege:**
  - ATR-abgeleitete Stop-Loss- und Take-Profit-Niveaus werden beim Einstieg symmetrisch gesetzt.
  - Trailing Stop (wenn `TrailingStopPips` &gt; 0) zieht in Gewinnrichtung nach.
  - Optionales Schließen bei Gegensignal oder Oszillator-Farbwechsel abhängig von `CloseOnReversal` und `ProfitFilter`.

## Schlüsselparameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `CandleType` | 1 Stunde | Zeitrahmen für alle Indikatoren. |
| `BollingerPeriod` | 20 | Periode des Bollinger-Band-Volatilitätsfilters. |
| `BollingerSigma` | 2.0 | Standardabweichungsmultiplikator für Bollinger-Bänder. |
| `BollingerSpreadLower` | 24 Pips | Mindest-Bandspread zum Handeln. |
| `BollingerSpreadUpper` | 230 Pips | Maximal erlaubter Bandspread. |
| `AoFastPeriod` / `AoSlowPeriod` | 4 / 28 | Schnelle und langsame Perioden des Awesome Oscillator. |
| `AoStrengthLimit` | 0.0 | Minimale normalisierte AO-Stärke zur Einstiegsbestätigung. |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | 1 / 4 / 1 | Stochastic-Längen, die die MetaTrader-Standards reproduzieren. |
| `StochasticLowerLevel` / `StochasticUpperLevel` | 12 / 21 | Überverkauft- und Überkauft-Schwellen zur Signalbestätigung. |
| `EntryHour` / `OpenHours` | 16 / 13 | Startstunde und Dauer des Handelsfensters. Mitternachtswechsel wird wie im EA behandelt. |
| `RiskPercent` | 0.5% | Risikoprozentsatz für Positionsgrößen, wenn Kontodaten verfügbar sind. |
| `AtrMultiplier` | 4.5 | Multiplikator auf den 3-Perioden-ATR zur Stop-Distanzberechnung. |
| `TrailingStopPips` | 40 Pips | Distanz für optionalen Trailing Stop (0 deaktiviert). |
| `ProfitFilter` | OnlyProfitable | Wählt, ob Umkehrausstiege beliebige, nur profitable oder nur verlierende Trades schließen dürfen. |
| `MaxOpenOrders` | 1 | Maximale Anzahl gleichzeitiger Positionen (1 zur Übereinstimmung mit dem EA). |

## Implementierungshinweise

- Verwendet StockSharp-Indikatoren `BollingerBands`, `StochasticOscillator`, `AwesomeOscillator`, `AverageTrueRange` und `Highest`; keine manuellen Indikatorberechnungen.
- AO-Werte werden über die letzten 100 Bars normalisiert, um die MetaTrader-Indikatorpuffer nachzuahmen und die Farblogik ohne eigenen Code zu reproduzieren.
- Die Positionsgröße berücksichtigt `Security.StepVolume`, `Security.MinVolume`, `Security.MaxVolume` und `Security.StepPrice`, sofern verfügbar; andernfalls wird das Standardvolumen der Strategie genutzt.
- Schutzniveaus werden vollständig in der Strategie verwaltet: Stop- und Take-Profit-Prüfungen laufen auf jeder abgeschlossenen Kerze, entsprechend der Tick-Verwaltung des EA ohne brokerseitige Orders.
- Alle Kommentare im Code sind auf Englisch, und die Einrückung verwendet Tabulatoren gemäß Projektrichtlinien.
