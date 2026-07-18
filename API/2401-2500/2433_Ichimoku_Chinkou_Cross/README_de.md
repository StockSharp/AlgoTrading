# Ichimoku Chinkou Kreuzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis der Kreuzung des Ichimoku Chinkou Span (verzögerte Linie) mit dem Preis.

## Strategielogik

- **Long:** Chinkou kreuzt den Preis von unten nach oben, sowohl der aktuelle Preis als auch Chinkou befinden sich oberhalb der Kumo-Wolke, und der RSI liegt über `RsiBuyLevel`.
- **Short:** Chinkou kreuzt den Preis von oben nach unten, sowohl der aktuelle Preis als auch Chinkou befinden sich unterhalb der Kumo-Wolke, und der RSI liegt unter `RsiSellLevel`.

Die Strategie nutzt Stop-Loss-Schutz über `StartProtection` und Parameter für Tenkan, Kijun, Senkou Span B und RSI.

## Parameter

| Name | Beschreibung | Standard |
|------|--------------|---------|
| `TenkanPeriod` | Tenkan-sen-Zeitraum | 9 |
| `KijunPeriod` | Kijun-sen-Zeitraum | 26 |
| `SenkouSpanPeriod` | Senkou Span B-Zeitraum | 52 |
| `RsiPeriod` | RSI-Berechnungszeitraum | 14 |
| `RsiBuyLevel` | RSI-Minimum für Long | 70 |
| `RsiSellLevel` | RSI-Maximum für Short | 30 |
| `StopLoss` | Stop-Loss-Prozent oder -Wert | 2% |
| `CandleType` | Kerzentyp für die Subscription | 5-Minuten-Kerzen |

## Indikatoren

- Ichimoku
- RSI
