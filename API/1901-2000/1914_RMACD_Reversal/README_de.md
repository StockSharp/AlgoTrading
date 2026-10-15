# RMACD Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie verwendet den Moving Average Convergence Divergence (MACD)-Indikator, um Umkehrsignale zu generieren. Vier verschiedene Modi definieren, wie Einstiege erkannt werden:

1. **Breakdown** – steigt long ein, wenn das MACD-Histogramm unter null kreuzt, und short, wenn es über null kreuzt.
2. **MacdTwist** – sucht nach einer Richtungsänderung im MACD durch Vergleich der letzten zwei Histogrammwerte.
3. **SignalTwist** – überwacht die Signallinie auf Richtungsänderungen.
4. **MacdDisposition** – steigt ein, wenn das MACD-Histogramm die Signallinie kreuzt.

Die Strategie verwendet immer Marktaufträge und dreht Positionen um, wenn ein neues entgegengesetztes Signal erscheint.

## Parameter
- **Fast Length** – Periode für den schnellen EMA innerhalb des MACD.
- **Slow Length** – Periode für den langsamen EMA innerhalb des MACD.
- **Signal Length** – Glättungsperiode für die Signallinie.
- **Candle Type** – Zeitrahmen der für Berechnungen verwendeten Kerzen.
- **Mode** – wählt den oben beschriebenen Einstiegsalgorithmus aus.

## Hinweise
- Signale werden nur auf abgeschlossenen Kerzen ausgewertet.
- Die Strategie speichert vorherige MACD-Werte intern, anstatt historische Daten anzufordern.
- Es wird kein expliziter Stop-Loss oder Take-Profit verwendet; Positionen werden nur bei entgegengesetzten Signalen geschlossen.
