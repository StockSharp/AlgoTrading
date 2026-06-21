# Symr Neuer-Balken-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Symr Neuer-Balken-Strategie** zeigt, wie der Beginn neuer Kerzen über mehrere Zeitrahmen hinweg mit einem einzigen Abonnement erkannt werden kann. Die Strategie überwacht einen Basis-Zeitrahmen und berechnet, wann größere Intervalle wie 5m, 15m, 30m, 1h, 4h, 1d, 20m und 55m beginnen. Jeder erkannte Balken wird protokolliert.

## Details

- **Einstiegskriterien**: Keine. Die Strategie platziert keine Trades.
- **Ausstiegskriterien**: Keine.
- **Long/Short**: Nicht anwendbar.
- **Stops**: Keine Stops werden verwendet.

### Parameter

| Name | Standard | Beschreibung |
|------|----------|--------------|
| `CandleType` | `TimeSpan.FromMinutes(1).TimeFrame()` | Basis-Zeitrahmen für die Erkennung neuer Balken. |

### Hinweise

- Speichert die letzte Öffnungszeit für jeden vordefinierten Zeitraum.
- Wenn der Basiszeitraum voranschreitet, werden größere Zeiträume ausgewertet und protokolliert, wenn sie sich überschlagen.
- Nützlich als Vorlage für die Verarbeitung von Multi-Timeframe-Ereignissen.
