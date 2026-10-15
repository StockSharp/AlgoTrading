# ZeroLag MACD-Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den **ZeroLagEA-AIP**-Algorithmus aus MetaTrader 5. Sie verwendet einen Zero-Lag-MACD, der aus zwei Zero-Lag-Exponential-Moving-Averages konstruiert ist. Das System öffnet eine Short-Position, wenn der MACD-Wert im Vergleich zur vorherigen Kerze steigt, und öffnet eine Long-Position, wenn der MACD fällt. Erscheint ein entgegengesetztes Signal bei offener Position, wird die aktuelle Position geschlossen und eine neue auf der folgenden Kerze eröffnet.

## Logik

1. Zwei Zero-Lag-EMAs mit konfigurierbaren Perioden werden berechnet.
2. Ihre Differenz multipliziert mit 10 bildet den Zero-Lag-MACD-Wert.
3. Ein Trade wird nur ausgeführt, wenn sich die MACD-Richtung zwischen zwei aufeinanderfolgenden Kerzen ändert (optional).
4. Trading ist nur zwischen den konfigurierten Start- und Endstunden erlaubt. Alle Positionen werden außerhalb dieses Fensters oder am angegebenen Wochentag und Stunde zwangsgeschlossen.

## Parameter

- **Volume** – Ordervolumen.
- **Fast EMA** – Periode der schnellen Zero-Lag-EMA.
- **Slow EMA** – Periode der langsamen Zero-Lag-EMA.
- **Use Fresh Signal** – wenn aktiviert, wird nur bei einer neuen MACD-Richtungsänderung gehandelt.
- **Start Hour / End Hour** – Grenzen der Handelssitzung in UTC.
- **Kill Day / Kill Hour** – Wochentag und Stunde, an dem alle Positionen geschlossen werden.
- **Candle Type** – Kerzendaten für Berechnungen.

## Hinweise

Die Strategie verwendet die High-Level-StockSharp-API mit `SubscribeCandles` und `Bind` zum Empfang von Indikatorwerten. Positionen werden mit Marktorders geschlossen.
