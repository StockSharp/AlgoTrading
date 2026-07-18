# Color XMACD Kerzen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-Implementierung des "ColorXMACDCandle"-Expert Advisors. Sie handelt mit dem MACD-Indikator und interpretiert Farbänderungen des Histogramms oder seiner Signallinie als Einstiegssignale.

## Idee

Die Strategie analysiert die Neigung einer MACD-Komponente:

- **Histogramm-Modus** – Ein neuer Histogrammbalken, der über den vorherigen Balken steigt, signalisiert wachsendes bullishes Momentum. Ein neuer Balken, der unter den vorherigen fällt, signalisiert bearishes Momentum.
- **Signallinienmodus** – Stattdessen wird die Neigung der MACD-Signallinie verwendet. Eine aufwärts gerichtete Neigung wirkt als Kaufsignal, während eine abwärts gerichtete Neigung als Verkaufssignal wirkt.

Wenn die gewählte Komponente aufwärts dreht und vorher nicht gestiegen war, kann eine Short-Position geschlossen und eine neue Long-Position eröffnet werden. Wenn die Komponente abwärts dreht und vorher nicht gefallen war, kann eine Long-Position geschlossen und eine Short-Position eröffnet werden.

Das Verhalten beim Öffnen und Schließen von Positionen wird durch separate Parameter gesteuert, sodass der Benutzer jede Aktion unabhängig aktivieren oder deaktivieren kann.

## Parameter

- `Mode` – Signalquelle: `Histogram` oder `SignalLine`.
- `FastPeriod` – Schneller EMA-Zeitraum für MACD.
- `SlowPeriod` – Langsamer EMA-Zeitraum für MACD.
- `SignalPeriod` – MACD-Signal-Glättungszeitraum.
- `EnableBuyOpen` – Eröffnung von Long-Positionen erlauben.
- `EnableSellOpen` – Eröffnung von Short-Positionen erlauben.
- `EnableBuyClose` – Schließen von Long-Positionen erlauben.
- `EnableSellClose` – Schließen von Short-Positionen erlauben.
- `CandleType` – Kerzentyp für Berechnungen.

## Handelsregeln

1. Abonniere die ausgewählte Kerzenserie und berechne den MACD-Indikator.
2. Verfolge die Neigung des Histogramms oder der Signallinie je nach ausgewähltem Modus.
3. Wenn die Neigung aufwärts dreht, schließe alle Short-Positionen (wenn erlaubt) und öffne optional eine Long-Position.
4. Wenn die Neigung abwärts dreht, schließe alle Long-Positionen (wenn erlaubt) und öffne optional eine Short-Position.

Die Strategie enthält keine Stop-Loss- oder Take-Profit-Mechanismen. Risikomanagement kann bei Bedarf separat hinzugefügt werden.
