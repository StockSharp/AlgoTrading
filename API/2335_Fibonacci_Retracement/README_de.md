# Fibonacci-Retracement-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Fibonacci-Retracement-Ausbrüche, die aus ZigZag-Pivots abgeleitet werden.

## Idee

1. Swing-Hochs und -Tiefs mit einem ZigZag-Ansatz erkennen.
2. Fibonacci-Retracement-Levels (23.6%, 38.2%, 61.8%, 76.4%) zwischen den letzten beiden Pivots aufbauen.
3. In einem Aufwärtstrend kauft die Strategie, wenn der Preis über einem Fibonacci-Level schließt.
4. In einem Abwärtstrend verkauft die Strategie, wenn der Preis unter einem Fibonacci-Level schließt.
5. Jede Order ist mit einem festen Stop-Loss und einem Take-Profit auf Basis der Swing-Spanne geschützt.
6. Nach dem Schließen einer Position wartet die Strategie eine Anzahl von Balken, bevor sie erneut handelt.

## Parameter

- `ZigzagDepth` – Tiefe für die Suche nach neuen Pivots.
- `SafetyBuffer` – Abstand in Punkten, den der Preis über das Level hinausbewegen muss.
- `TrendPrecision` – Mindestunterschied zwischen Pivots zur Erkennung der Trendrichtung.
- `CloseBarPause` – Anzahl der Balken, die nach dem Schließen eines Trades gewartet wird.
- `TakeProfitFactor` – Bruchteil der Swing-Spanne als Take-Profit-Erweiterung.
- `StopLossPoints` – Stop-Loss-Abstand vom Einstiegspreis in Punkten.
- `CandleType` – Kerzentyp für Berechnungen.

## Hinweise

Diese Datei enthält nur die C#-Implementierung. Eine Python-Version ist noch nicht verfügbar.
