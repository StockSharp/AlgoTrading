# MACD-und-SAR-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den ursprünglichen MetaTrader-Expertenberater "MACD and SAR". Sie bewertet die Beziehung zwischen der MACD-Haupt- und Signallinie zusammen mit dem Parabolic-SAR-Niveau auf jeder abgeschlossenen Kerze. Konfigurierbare Schalter ermöglichen es, jeden Vergleich umzukehren, sodass dieselbe Vorlage sowohl für konträtere als auch für trendfolgende Setups verwendet werden kann. Mehrere Einstiege sind erlaubt, solange die konfigurierte maximale Anzahl gestapelter Positionen nicht überschritten wird.

Wenn ein Long-Signal erscheint, wird das bestehende Short-Engagement geschlossen und ein neues Long-Lot eröffnet (wenn das Limit nicht erreicht ist). Ebenso schließt ein Short-Signal zuerst Longs und fügt dann ein Short-Lot hinzu. Es gibt keine zusätzlichen Stop-Loss- oder Take-Profit-Orders; Trades werden nur geschlossen, wenn das entgegengesetzte Signal generiert wird.

## Strategielogik

1. Auf eine abgeschlossene Kerze des konfigurierten Zeitrahmens warten.
2. MACD-Werte (Haupt-, Signal-, Histogramm) und das Parabolic-SAR-Niveau auf Basis der Schlusskurse lesen.
3. Die folgenden Vergleiche auswerten, von denen jeder durch seinen entsprechenden booleschen Parameter umgekehrt werden kann:
   - MACD-Hauptlinie vs. Signallinie.
   - MACD-Signallinie vs. Nullniveau.
   - Parabolic SAR vs. Schlusskurs.
4. Wenn alle drei Vergleiche für die Long-Seite erfüllt sind und die Strategie noch Raum zum Stapeln neuer Positionen hat, das angegebene Lotvolumen kaufen (einschließlich des Volumens zum Schließen von Shorts).
5. Wenn alle drei Vergleiche für die Short-Seite erfüllt sind und das Stapellimit es erlaubt, das angegebene Lotvolumen verkaufen (einschließlich des Volumens zum Schließen von Longs).

## Parameter

- `TradeVolume` — Volumen pro Einzeltrade (Standard `0.1`).
- `MaxPositions` — maximale Anzahl gestapelter Positionen in eine Richtung (Standard `10`).
- `MacdFastPeriod` — schnelle EMA-Periode für MACD (Standard `12`).
- `MacdSlowPeriod` — langsame EMA-Periode für MACD (Standard `26`).
- `MacdSignalPeriod` — Signal-Glättungsperiode für MACD (Standard `9`).
- `SarStep` — Parabolic-SAR-Beschleunigungsschritt (Standard `0.02`).
- `SarMaximum` — maximale Parabolic-SAR-Beschleunigung (Standard `0.2`).
- `BuyMacdGreaterSignal` — wenn `true`, wird MACD-Haupt > Signal für Longs gefordert; sonst wird das Gegenteil erwartet (Standard `true`).
- `BuySignalPositive` — wenn `true`, wird MACD-Signal > 0 für Longs gefordert; sonst wird Signal < 0 erwartet (Standard `false`).
- `BuySarAbovePrice` — wenn `true`, wird SAR oberhalb des Preises für Longs gefordert; sonst wird Preis oberhalb SAR erwartet (Standard `false`).
- `SellMacdGreaterSignal` — wenn `true`, wird MACD-Haupt > Signal für Shorts gefordert; sonst wird MACD-Haupt < Signal erwartet (Standard `false`).
- `SellSignalPositive` — wenn `true`, wird MACD-Signal > 0 für Shorts gefordert; sonst wird Signal < 0 erwartet (Standard `true`).
- `SellSarAbovePrice` — wenn `true`, wird SAR oberhalb des Preises für Shorts gefordert; sonst wird Preis oberhalb SAR erwartet (Standard `true`).
- `CandleType` — Kerzentyp/Zeitrahmen für die Datenverarbeitung (Standard `15` Minuten).

## Zusätzliche Hinweise

- Die Strategie verlässt sich ausschließlich auf Indikatorkreuzungen; es gibt keine Schutz-Stops oder Gewinnziele.
- Das Positions-Stapeln wird durch Vergleich des absoluten Positionsvolumens mit `MaxPositions * TradeVolume` mit einer kleinen Toleranz für Rundung implementiert.
- Alle Trades werden mit Marktorders ausgeführt. Stellen Sie sicher, dass die Portfolio-Volumeneinstellung zu den geplanten Instrumenten passt.
- Fügen Sie optionale Portfolio-Schutzregeln hinzu, wenn Sie Drawdown-Limits oder Trailing Stops benötigen; standardmäßig sind keine enthalten.
