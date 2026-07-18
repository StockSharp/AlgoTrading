# Gordago EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Port des historischen "Gordago EA" MetaTrader 5-Expert-Advisors. Die Strategie handelt auf dem Basis-Zeitrahmen (Standard M3), liest MACD-Signale von einem höheren Intraday-Chart und einen stochastischen Filter von einem Stundenchart. Sie bewahrt die ursprünglichen Stop/Take-Parameter und die Trailing-Logik, verwendet jedoch die StockSharp-High-Level-API für Datenabonnements und Auftragsverwaltung.

## Strategielogik

- **Marktdaten**
  - Haupt-Ausführungskerzen: konfigurierbar, Standard Drei-Minuten-Kerzen.
  - MACD-Kerzen: konfigurierbar, Standard Zwölf-Minuten-Kerzen.
  - Stochastik-Kerzen: konfigurierbar, Standard Ein-Stunden-Kerzen.
- **Indikatoren**
  - MACD (schnell 12, langsam 26, Signal 9) berechnet auf dem MACD-Zeitrahmen.
  - Stochastischer Oszillator (Länge 5, %K-Glättung 3, %D 3) berechnet auf dem stochastischen Zeitrahmen.
- **Einstiegsbedingungen**
  - **Kaufen**: aktueller MACD-Wert über dem vorherigen, vorheriger MACD unter null, stochastisches %K unter dem Kaufschwellenwert (Standard 37) und steigend gegenüber dem Vorwert.
  - **Verkaufen**: aktueller MACD-Wert unter dem vorherigen, vorheriger MACD über null, stochastisches %K über dem Verkaufsschwellenwert (Standard 96) und fallend gegenüber dem Vorwert.
- **Auftragserteilung**
  - Das Ordervolumen ist fest; das Wechseln der Richtung gleicht automatisch jede entgegengesetzte Position aus, bevor eine neue eröffnet wird.
  - Separate Stop-Loss/Take-Profit-Abstände existieren für Long- und Short-Trades (Standards: 40/70 Pips für Long, 10/40 Pips für Short).
- **Ausstiege**
  - Schützende Stop-Loss- und Take-Profit-Niveaus werden bei jeder abgeschlossenen Basiskerze geprüft.
  - Ein Trailing Stop aktiviert sich, wenn der Preis über die konfigurierte Trailing-Distanz plus Trailing-Schritt hinausgeht; einmal ausgelöst, ratscht er weiterhin um die Trailing-Distanz in Richtung Markt.
  - Trailing kann einen Schutz-Stop einführen, auch wenn der ursprüngliche Stop deaktiviert war, und spiegelt damit den Quell-EA wider.

## Parameter

- `OrderVolume` – Handelsvolumen in Lots.
- `StopLossBuyPips` / `TakeProfitBuyPips` – Stop-Loss- und Take-Profit-Abstände für die Long-Seite (in Pips).
- `StopLossSellPips` / `TakeProfitSellPips` – Stop-Loss- und Take-Profit-Abstände für die Short-Seite (in Pips).
- `TrailingStopPips` – Trailing-Abstand in Pips; auf null setzen um Trailing zu deaktivieren.
- `TrailingStepPips` – minimaler zusätzlicher Gewinn (in Pips) bevor der Trailing Stop vorrücken kann.
- `StochasticBuyLevel` / `StochasticSellLevel` – Oszillatorschwellenwerte für Long- und Short-Einstiege.
- `CandleType` – Arbeitszeitrahmen für die Ausführungslogik.
- `MacdCandleType` – Zeitrahmen für die MACD-Indikator-Speisung.
- `StochasticCandleType` – Zeitrahmen für die Speisung des stochastischen Oszillators.
- `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` – MACD-Perioden.
- `StochasticLength`, `StochasticSignalPeriod`, `StochasticSmoothing` – stochastische Oszillatorperioden.

## Implementierungshinweise

- Pip-Abstände werden mit dem `PriceStep` des Wertpapiers in Preise umgerechnet. Wenn der Schritt drei oder fünf Nachkommastellen hat, multipliziert die Strategie ihn mit zehn und reproduziert damit die Pip-Anpassung der ursprünglichen MQL-Implementierung für 3/5-stellige Forex-Kurse.
- Der Trailing Stop wird ignoriert, wenn `TrailingStopPips` positiv ist, `TrailingStepPips` jedoch nicht; in diesem Fall wird eine Warnung protokolliert.
- Da die StockSharp-Version auf Kerzen-Schließereignissen arbeitet, wird die Schutzlogik einmal pro abgeschlossener Kerze ausgeführt statt bei jedem Tick wie in der MT5-Version. Das Handelsverwaltungsverhalten folgt ansonsten den ursprünglichen Regeln.
- Nur die C#-Implementierung wird bereitgestellt; keine Python-Übersetzung oder -Ordner ist auf Anfrage enthalten.
