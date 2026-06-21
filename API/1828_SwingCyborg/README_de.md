# Swing Cyborg-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Swing Cyborg ist ein diskretionärer Assistent, der die Ausführung auf Basis der eigenen Trendprognose eines Traders automatisiert. Der Benutzer definiert die erwartete Trendrichtung und das Zeitfenster, in dem diese gültig sein soll. Die Strategie bestätigt Einstiege mit dem RSI-Indikator und verwaltet Ausstiege mit festen Zielen.

## Parameter
- `Volume` – Auftragsvolumen in Lots.
- `TrendPrediction` – erwartete Trendrichtung (Uptrend oder Downtrend).
- `TrendTimeframe` – Zeitrahmen für RSI und Handel (M30, H1 oder H4).
- `TrendStart` – Beginn des vom Benutzer definierten Trendzeitraums.
- `TrendEnd` – Ende des vom Benutzer definierten Trendzeitraums.
- `Aggressiveness` – Money-Management-Preset:
  - Niedrig: Take-Profit 300 Pips, Stop-Loss 200 Pips.
  - Mittel: Take-Profit 500 Pips, Stop-Loss 250 Pips.
  - Hoch: Take-Profit 600 Pips, Stop-Loss 300 Pips.

## Handelslogik
1. Warten auf eine neue Kerze im ausgewählten Zeitrahmen.
2. Nur handeln, wenn die aktuelle Zeit zwischen `TrendStart` und `TrendEnd` liegt.
3. RSI(14) berechnen.
4. Wenn keine offene Position vorhanden:
   - Wenn `TrendPrediction` Uptrend ist und RSI ≤ 65 → kaufen.
   - Wenn `TrendPrediction` Downtrend ist und RSI ≥ 35 → verkaufen.
5. `StartProtection` schließt die Position automatisch, wenn Gewinn oder Verlust das vordefinierte Niveau erreicht.

Die Strategie arbeitet auf abgeschlossenen Kerzen und eröffnet keine neue Position, solange eine aktive vorhanden ist.
