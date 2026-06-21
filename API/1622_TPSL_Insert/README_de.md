# TPSL Insert-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-Übersetzung des MetaTrader 4-Skripts **TPSL-Insert.mq4**. Sie generiert keine Ein- oder Ausstiegssignale. Ihr einziger Zweck ist es, Take-Profit- und Stop-Loss-Aufträge an bestehende Positionen anzuhängen.

## Funktionsweise

1. Beim Start liest die Strategie die Parameter `TakeProfitPips` und `StopLossPips`.
2. Die Werte werden von Pips in Preis umgerechnet, mithilfe des `PriceStep` des Wertpapiers.
3. `StartProtection` wird aufgerufen, um Schutzaufträge zu platzieren.
   - Wenn bereits eine Position vorhanden ist, werden Schutzaufträge sofort gesendet.
   - Zukünftige von der Strategie eröffnete Positionen werden automatisch geschützt.

Dieses Verhalten ist nützlich, wenn Positionen manuell oder durch externe Systeme geöffnet werden und Sie schnell Risikolimits einfügen müssen.

## Parameter

| Name | Beschreibung | Standard |
|------|--------------|----------|
| `TakeProfitPips` | Take-Profit-Abstand in Pips. | `35` |
| `StopLossPips` | Stop-Loss-Abstand in Pips. | `100` |

## Hinweise

- Die Strategie abonniert keine Marktdaten und enthält keine Handelslogik.
- `StartProtection` übernimmt die Erstellung und Stornierung von Schutzaufträgen.
