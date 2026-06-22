# Levels-mit-Trailing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konvertiert aus dem MQL-Skript `levels_with_trail.mq4`. Die Strategie eröffnet Trades, wenn der Preis ein bestimmtes Level durchbricht, und kann den Stop-Loss nachziehen.

## So funktioniert es
- Abonniert Kerzen des gewählten Zeitrahmens.
- Wenn keine offene Position vorhanden ist und der Schlusskurs über `Level Price` liegt, wird gekauft; liegt er darunter, wird verkauft.
- Wenn `Trail Stop` aktiviert ist, folgt der Stop-Loss dem Preis, wenn die Position profitabel ist.
- Positionen werden geschlossen, wenn der Stop-Loss, der Take-Profit oder ein entgegengesetztes Ausbruchssignal ausgelöst wird.

## Parameter
- `Stop Loss` – Stop-Loss-Größe in Preiseinheiten.
- `Take Profit` – Take-Profit-Größe in Preiseinheiten.
- `Level Price` – zu beobachtendes Ausbruchs-Level.
- `Trail Stop` – Trailing Stop-Loss aktivieren oder deaktivieren.
- `Candle Type` – für die Analyse verwendeter Kerzen-Zeitrahmen.
