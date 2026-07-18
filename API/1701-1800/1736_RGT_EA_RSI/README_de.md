# RGT EA RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert den **Relative Strength Index (RSI)** mit **Bollinger-Bändern**, um extreme Preisbewegungen zu identifizieren und potenzielle Umkehrungen zu handeln. Positionen werden eröffnet, wenn der RSI in überverkaufte oder überkaufte Zonen eintritt und der Preis die Bollinger-Bänder kreuzt. Ein Stop-Loss und Trailing Stop verwalten das Risiko und sichern Gewinne.

## Funktionsweise

1. RSI und Bollinger-Bänder werden für eingehende Kerzen berechnet.
2. **Kaufen**, wenn der RSI unter dem überverkauften Niveau liegt und der Schlusskurs unter dem unteren Band liegt.
3. **Verkaufen**, wenn der RSI über dem überkauften Niveau liegt und der Schlusskurs über dem oberen Band liegt.
4. Nach dem Einstieg wird ein fester Stop-Loss platziert. Sobald die Position den Mindestgewinn erreicht, verfolgt der Stop-Loss den Preis.

## Parameter

| Name | Beschreibung |
|------|-------------|
| `Volume` | Ordervolumen. |
| `RsiPeriod` | RSI-Berechnungsperiode. |
| `RsiHigh` | RSI-Überkauft-Schwellenwert. |
| `RsiLow` | RSI-Überverkauft-Schwellenwert. |
| `StopLoss` | Anfänglicher Stop-Loss-Abstand in Preiseinheiten. |
| `TrailingStop` | Trailing-Stop-Abstand in Preiseinheiten. |
| `MinProfit` | Mindestgewinn bevor das Trailing aktiviert wird. |
| `CandleType` | Kerzentyp für Berechnungen. |

## Hinweise

- Funktioniert mit jedem von StockSharp unterstützten Instrument und Zeitrahmen.
- Verwendet Marktaufträge für Ein- und Ausstiege.
- Der Trailing Stop wird bei jeder abgeschlossenen Kerze aktualisiert.
