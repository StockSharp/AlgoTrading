# Dynamic Averaging-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Dynamic Averaging ist ein direkter Port des MetaTrader-5-Expert-Advisors „Dynamic averaging.mq5" (ID 23319). Die Strategie kombiniert einen schnellen Stochastic-Oszillator mit einem Volatilitätsfilter auf Basis der Standardabweichung. Trades sind nur erlaubt, solange die Marktvolatilität unter ihrem rollierenden Durchschnitt bleibt, was Einstiege in Konsolidierungsphasen erzwingt, in denen Stochastic-Umkehrungen zuverlässiger sind.

## Parameter
- **TradeVolume** – Ordergröße für jeden neuen Einstieg. Wird nach einer Verlustsequenz automatisch verdoppelt und nach einer Gewinnserie zurückgesetzt.
- **MinimumProfit** – Schwebender Gewinn (in Kontowährung), der alle offenen Positionen schließt, sobald er überschritten wird.
- **SlidingWindowDays** – Anzahl der Kalendertage für die Mittelung der Standardabweichungswerte und den Aufbau der Volatilitätsbasis.
- **StochasticKPeriod** – Anzahl der Bars für die %K-Berechnung.
- **StochasticDPeriod** – Glättungslänge für die %D-Linie.
- **StochasticSlowPeriod** – Abschließende Verlangsamungsperiode des Stochastic-Oszillators.
- **StdDevPeriod** – Rückblickperiode für den Standardabweichungsindikator.
- **CandleType** – Quellkerzen für Berechnungen (Standard: 15-Minuten-Zeitrahmen).

## Handelsregeln
1. Die Strategie operiert ausschließlich auf abgeschlossenen Kerzen. Beim Schluss jedes Bars werden Stochastic- und Volatilitätsfilter über `SubscribeCandles().BindEx` aktualisiert.
2. Marktvolatilität mit `StandardDeviation(StdDevPeriod)` berechnen und mit der durchschnittlichen Volatilität aus `SimpleMovingAverage` über die letzten `SlidingWindowDays` Bars vergleichen.
3. Liegt die aktuelle Standardabweichung über dem rollierenden Durchschnitt, wird der Bar übersprungen.
4. Bei gedämpfter Volatilität:
   - **Long** einsteigen, wenn %K unter 25 liegt und die Steigung der letzten zwei %K-Werte positiv ist (letzter Wert minus Wert vor zwei Bars).
   - **Short** einsteigen, wenn %K über 75 liegt und die Steigung der letzten zwei %K-Werte negativ ist.
5. Positionen werden umgekehrt, indem genug Volumen gesendet wird, um die Gegenseite zu glätten und die neue `TradeVolume`-Exposition hinzuzufügen.
6. Sobald der schwebende PnL der offenen Position `MinimumProfit` übersteigt, verlässt die Strategie den Markt sofort.

## Positionsgrößen und Erholung
- Die anfängliche Ordergröße entspricht `TradeVolume`.
- Nach dem Schließen der Position wird die realisierte PnL-Änderung geprüft.
  - Ein **Verlust** verdoppelt die nächste Handelsgröße (`Martingal`-Schritt), um das ursprüngliche EA-Verhalten zu replizieren.
  - **Gewinn oder Breakeven** setzt die Größe auf das Basis-`TradeVolume` zurück.

## Implementierungsdetails
- Kerzen, Stochastic- und Standardabweichungswerte werden über die hochrangige API mit `BindEx` verarbeitet, was manuelle Pufferverwaltung vermeidet.
- Das gleitende Volatilitätsfenster wandelt Kalendertage in Baranzahlen um, indem der Kerzen-Zeitrahmen verwendet wird, wenn verfügbar.
- Die Schwebgewinn-Kontrolle basiert auf dem aktuellen Kerzenschluss und `PositionAvgPrice`, was der MQL-Implementierung entspricht, die nur den Gewinn offener Positionen summiert.
- Alle Codekommentare sind auf Englisch; keine Python-Version gemäß Aufgabenanforderungen.
