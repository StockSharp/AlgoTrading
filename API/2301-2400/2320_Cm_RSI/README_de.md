# cm RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist ein direkter Port des MetaTrader 4 Experten "cm_RSI". Sie verwendet den Relative Strength Index (RSI)-Indikator, um Momentum-Umkehrungen zu erfassen.

Der Algorithmus überwacht RSI-Werte, die aus den Eröffnungspreisen der Kerzen berechnet werden. Eine Long-Position wird eröffnet, wenn der RSI nach einem Unterschreiten über ein konfigurierbares *Kaufniveau* steigt. Eine Short-Position wird eröffnet, wenn der RSI nach einem Überschreiten unter ein konfigurierbares *Verkaufsniveau* fällt. Jeder Trade ist durch feste Take-Profit- und Stop-Loss-Werte in Preispunkten geschützt.

## Strategie-Logik

1. RSI mit einer benutzerdefinierten Periode unter Verwendung der Kerzeneröffnungspreise berechnen.
2. Wenn der vorherige RSI-Wert unter dem Kaufniveau lag und der aktuelle Wert darüber kreuzt, eine Long-Marktposition eröffnen.
3. Wenn der vorherige RSI-Wert über dem Verkaufsniveau lag und der aktuelle Wert darunter kreuzt, eine Short-Marktposition eröffnen.
4. Jeder Trade verwendet das gleiche konfigurierbare Volumen und ist durch Stop-Loss- und Take-Profit-Orders geschützt.

## Parameter

| Name | Beschreibung |
|------|--------------|
| `RsiPeriod` | RSI-Berechnungsperiode. |
| `BuyLevel` | RSI-Level für Long-Einträge. |
| `SellLevel` | RSI-Level für Short-Einträge. |
| `TakeProfit` | Take Profit in absoluten Preispunkten. |
| `StopLoss` | Stop Loss in absoluten Preispunkten. |
| `OrderVolume` | Volumen für jeden Trade. |
| `CandleType` | Kerzentyp für Berechnungen. |

## Hinweise

- Die Strategie verarbeitet nur abgeschlossene Kerzen.
- Es wird immer nur eine offene Position gehalten.
- `StartProtection` wird verwendet, um Stop-Loss- und Take-Profit-Orders automatisch zu verwalten.

