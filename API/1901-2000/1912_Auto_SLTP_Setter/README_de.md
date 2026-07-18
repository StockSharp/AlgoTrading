# Automatischer SLTP-Setzer-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Hilfsstrategie, die automatisch Stop-Loss- und Take-Profit-Aufträge an offene Positionen anhängt, wenn diese fehlen. Die Abstände können als feste Pip-Werte oder als Vielfache des Average True Range (ATR) definiert werden.

## Parameter

- `Candle Type` – Zeitrahmen für die ATR-Berechnung.
- `Set Stop Loss` – automatische Stop-Loss-Platzierung aktivieren.
- `Set Take Profit` – automatische Take-Profit-Platzierung aktivieren.
- `Stop Loss Method` – 1 = feste Pips, 2 = ATR-Vielfaches.
- `Fixed SL (pips)` – Stop-Loss-Abstand in Pips für die feste Methode.
- `SL ATR Multiplier` – ATR-Multiplikator für den Stop-Loss bei der ATR-Methode.
- `Take Profit Method` – 1 = feste Pips, 2 = ATR-Vielfaches.
- `Fixed TP (pips)` – Take-Profit-Abstand in Pips für die feste Methode.
- `TP ATR Multiplier` – ATR-Multiplikator für den Take-Profit bei der ATR-Methode.
- `ATR Period` – Anzahl der Perioden für die ATR-Berechnung.

## Funktionsweise

1. Beim Start wertet die Strategie die Konfiguration aus.
2. Wenn ATR-basierte Werte angefordert werden, abonniert sie die angegebene Kerzenserie und berechnet den ATR.
3. Sobald der ATR-Wert verfügbar ist, ruft die Strategie `StartProtection` mit den berechneten Abständen auf.
4. `StartProtection` platziert Schutzaufträge für alle bestehenden Positionen und für zukünftige Trades, die von der Strategie eröffnet werden.

Die Strategie generiert keine Handelssignale; sie verwaltet nur das Risiko, indem sie sicherstellt, dass jede Position angemessene Stop-Loss- und Take-Profit-Niveaus hat.
