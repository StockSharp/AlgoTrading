# Strategie AddOn Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Port des MetaTrader-Experten **AddOn_TrailingStop**. Die Strategie eröffnet keine eigenständigen Positionen und passt lediglich Trailing Stops für eine bestehende Nettoposition an.

## Wie es funktioniert

- Abonniert Level1-Daten, um die neuesten besten Geld- und Briefkurse zu überwachen.
- Berechnet die Pip-Größe aus den Sicherheitsdezimalstellen, sodass sich die Eingaben wie in MetaTrader verhalten (4/5 Ziffern = 0,0001 Pip, 2/3 Ziffern = 0,01 Pip).
- Wenn eine Long-Position offen ist und der Geldkurs um `TrailingStartPips` Pips steigt, verschiebt die Strategie den internen Trailing Stop auf `Bid - TrailingStartPips` Pips.
- Der Long Stop wird nur dann vorgezogen, wenn das neue Level mindestens `TrailingStepPips` Pips höher ist als der vorherige Stop.
- Wenn eine Short-Position offen ist und der Briefkurs um `TrailingStartPips` Pips sinkt, verschiebt die Strategie den internen Trailing Stop auf `Ask + TrailingStartPips` Pips.
- Der Short-Stop wird nur dann vorgezogen, wenn das neue Level mindestens `TrailingStepPips` Pips niedriger ist als der vorherige Stop.
- Wenn der aktuelle Kurs den Trailing Stop überschreitet, schließt die Strategie die gesamte Position zum Marktwert und setzt ihren Status zurück.

## Parameter

- `EnableTrailing` (Standard **true**) – aktiviert oder deaktiviert die Trailing-Stop-Verwaltung.
- `TrailingStartPips` (Standard **15**) – erforderlicher Gewinn in Pips, bevor das Trailing aktiviert wird.
- `TrailingStepPips` (Standard **5**) – zusätzlicher Gewinn in Pips erforderlich, bevor sich der Stop wieder bewegen kann.
- `MagicNumber` (Standard **0**) – Bezeichner wird aus Gründen der Parität mit dem MQL-Experten beibehalten. Dies ist informativ, da StockSharp auf der aktuellen Strategieposition arbeitet.

## Notizen

- Erfordert einen konfigurierten `Security`-, `Portfolio`- und Level1-Datenfeed.
- Entwickelt, um andere Strategien zu ergänzen, die Einträge verarbeiten.
- Verwendet `StrategyParam<T>`, damit jede Eingabe in der Benutzeroberfläche optimiert oder verfügbar gemacht werden kann.
- Sendet `BuyMarket`/`SellMarket` Orders, wenn der Trailing Stop erreicht wird, da StockSharp automatisch Schutzorder verwaltet, nachdem die Position verlassen wurde.
