# AdaptiveTrader Pro-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
AdaptiveTrader Pro ist eine Multi-Timeframe-Trendfolgestrategie, die aus dem MetaTrader 5 Expert Advisor *AdaptiveTrader_Pro_Final_EA.mq5* konvertiert wurde. Es kombiniert RSI, ATR und gleitende Durchschnitte, um in Richtung des vorherrschenden Trends zu handeln und gleichzeitig Geldmanagementkontrollen anzuwenden.

Die Strategie arbeitet mit einem konfigurierbaren primären Zeitrahmen (Standard 5 Minuten) und bestätigt die Trendrichtung mithilfe eines gleitenden Durchschnitts für einen höheren Zeitrahmen (Standard 1 Stunde). Einträge basieren auf überverkauften/überkauften RSI-Signalen, die mit beiden gleitenden Durchschnitten übereinstimmen.

## Handelsregeln
- **Long Entry**: Wenn RSI unter 30 fällt und der Kerzenschluss über dem Hauptzeitrahmen SMA und dem höheren Zeitrahmen SMA liegt.
- **Short-Einstieg**: Wenn RSI über 70 steigt und der Kerzenschluss unter beiden SMAs liegt.
- **Einzelposition**: Es wird jeweils nur eine Richtungsposition beibehalten. Gegenüberliegende Positionen werden vor der Umkehrung geschlossen.

## Risiko- und Handelsmanagement
- **Positionsgröße**: Die Positionsgröße wird aus Portfolio-Eigenkapital, Risikoprozentsatz und ATR-basierter Stoppdistanz berechnet.
- **Stop-Handling**: Ein ATR-basierter Trailing-Stop folgt dem Preis und wird auf die Gewinnschwelle verschärft, nachdem sich der Handel um ein konfigurierbares ATR-Vielfaches zu seinen Gunsten bewegt.
- **Teilgewinn**: Ein konfigurierbarer Bruchteil der Position wird bei einem ersten Ziel (ATR-Vielfaches) geschlossen. Das verbleibende Volumen wird durch den Trailing Stop verwaltet.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `MaxRiskPercent` | Risikoprozentsatz, der pro Trade auf das Konto angewendet wird. | `0.2` |
| `RsiPeriod` | RSI Länge im Hauptzeitraum. | `14` |
| `AtrPeriod` | ATR Länge im Hauptzeitraum. | `14` |
| `AtrMultiplier` | ATR-Multiplikator für den anfänglichen Stoppabstand. | `1.5` |
| `TrailingStopMultiplier` | ATR-Multiplikator, der beim Nachlaufen des Stopps verwendet wird. | `1.0` |
| `TrailingTakeProfitMultiplier` | ATR-Multiplikator für das Teil-Take-Profit-Ziel. | `2.0` |
| `TrendPeriod` | SMA Länge im Hauptzeitraum. | `20` |
| `HigherTrendPeriod` | SMA Länge im höheren Zeitrahmen. | `50` |
| `BreakEvenMultiplier` | ATR-Multiplikator, der die Verschiebung des Stops auf die Gewinnschwelle auslöst. | `1.5` |
| `PartialCloseFraction` | Bruchteil der Anfangsposition, die beim ersten Ziel geschlossen wurde. | `0.5` |
| `MaxSpreadPoints` | Maximal zulässiger Spread in Preisschritten vor der Eröffnung von Geschäften. | `20` |
| `CandleType` | Primärer Kerzentyp (Zeitrahmen), der für die Analyse verwendet wird. | `5 minute candles` |
| `HigherCandleType` | Kerzentyp mit höherem Zeitrahmen, der zur Bestätigung verwendet wird. | `1 hour candles` |

## Notizen
- Die Strategie verwendet StockSharp auf hoher Ebene API mit Kerzenabonnements und Indikatorbindung.
- Spreads werden anhand der besten Geld-/Briefkurse überwacht; Der Handel wird ausgesetzt, bis der Spread innerhalb des konfigurierten Limits liegt.
- Gemäß den Anweisungen wird absichtlich auf die Python-Implementierung verzichtet.
