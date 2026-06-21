# Exp ATR-Trailing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Beispiel zeigt, wie bestehende Positionen mit einem Trailing-Stop auf Basis des **Average True Range (ATR)**-Indikators verwaltet werden. Die Strategie erzeugt keine Einstiegssignale; sie passt lediglich das Ausstiegsniveau einer offenen Position entsprechend der Marktvolatilität an.

## Funktionsweise

1. Die Strategie abonniert Kerzendaten eines gewählten Zeitrahmens.
2. Auf jeder Kerze wird ein `AverageTrueRange`-Indikator berechnet.
3. Für Long-Positionen wird der Stop-Level auf `Close - ATR * BuyFactor` angehoben.
4. Für Short-Positionen wird der Stop-Level auf `Close + ATR * SellFactor` gesenkt.
5. Wenn der Preis das Trailing-Niveau kreuzt, wird die Position zum Marktpreis geschlossen.

Der Trailing-Stop bewegt sich nur in Richtung des Trades und zieht sich nie zurück, was einen volatilitätsangepassten Ausstieg bietet.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `AtrPeriod` | ATR-Berechnungsperiode. |
| `BuyFactor` | Multiplikator für den ATR beim Trailing einer Long-Position. |
| `SellFactor` | Multiplikator für den ATR beim Trailing einer Short-Position. |
| `CandleType` | Zeitrahmen der für die Analyse verwendeten Kerzen. |

## Verwendungshinweise

- Strategie einem Instrument zuordnen und eine Position manuell oder über eine andere Strategie eröffnen.
- Geeignet für das Risikomanagement, bei dem Ausstiege unabhängig von Einstiegen gesteuert werden.
- Der Chartbereich zeigt Kerzen, ATR-Werte und ausgeführte Trades für die visuelle Analyse.

## Referenzen

- [Average True Range in der StockSharp-Dokumentation](https://doc.stocksharp.com/topics/indicator_average_true_range.html)
- [Strategy Designer](https://doc.stocksharp.com/topics/designer.html)
