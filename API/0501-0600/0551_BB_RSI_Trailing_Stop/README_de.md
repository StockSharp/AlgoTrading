# BB RSI Trailing Stop

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Bollinger-Ausbruch benötigt eine RSI-Erholung; danach gilt ein bedingter Trailing-Stop. `p` ist die vorherige gebildete Kerze; RSI verwendet Wilder-Glättung.

## Handelsregeln

Long: `Low < lowerBand` und `RSI[p] <= RsiOversold && RSI > RsiOversold`. Short: `High > upperBand` und `RSI[p] >= RsiOverbought && RSI < RsiOverbought`. Ein weiterhin extremer RSI genügt nicht. Einstieg mit `Volume` nur ohne Position, ohne versteckte Wartezeit.

Basis ist der gewichtete tatsächliche Einstiegspreis; Long nutzt ausführbaren Bid, Short Ask. Anfangsabstand `StopLossPoints`; Trailing beginnt erst nach einem günstigen Extrem von `TrailOffsetPoints`. Danach folgt der Stop diesem Extrem mit `TrailStopPoints`, ohne den Anfangsstop zu lockern. Eine Kursüberschreitung schließt die gesamte ausgeführte Position per Marktorder. Alle Abstände sind absolute Preiseinheiten, keine Ticks oder Prozente. Frühere Kerzenextreme lösen keinen erst später erzeugten Stop aus.

Einstiege verwenden nur abgeschlossene Kerzen und vollständig gebildete Indikatoren. Offene Positionen werden bis zum Ausstieg verwaltet; ausstehende Marktorders verhindern doppelte Aufträge.

Bollinger verwendet die Populationsstandardabweichung des gleitenden Schlusskursfensters einschließlich der aktuellen abgeschlossenen Kerze.

## Parameter

| Parameter | Default |
|---|---|
| `BollingerPeriod` | `25` |
| `BollingerDeviation` | `2` |
| `RsiPeriod` | `14` |
| `RsiOversold` | `33` |
| `RsiOverbought` | `60` |
| `StopLossPoints` | `50` |
| `TrailOffsetPoints` | `99` |
| `TrailStopPoints` | `40` |
| `CandleType` | `15m` |
