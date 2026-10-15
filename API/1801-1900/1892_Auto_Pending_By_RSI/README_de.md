# Automatische Pending-Orders per RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie platziert Pending-Limit-Orders, nachdem der Relative Strength Index (RSI) für mehrere aufeinanderfolgende Kerzen in extremen Zonen verbleibt.

Wenn der RSI für `MatchCount` Kerzen unter dem überverkauften Niveau bleibt, wird eine Kauf-Limit-Order unterhalb des Kerzenschlusskurses um `PendingOffset` Preispunkte registriert. Wenn der RSI für dieselbe Anzahl von Kerzen über dem überkauften Niveau bleibt, wird eine Verkaufs-Limit-Order oberhalb des Schlusskurses mit demselben Versatz platziert.

## Parameter
- `RsiPeriod` – RSI-Berechnungsperiode.
- `RsiOverbought` – Niveau, das die überkaufte Zone definiert.
- `RsiOversold` – Niveau, das die überverkaufte Zone definiert.
- `PendingOffset` – Abstand vom Schlusskurs zur Platzierung von Pending-Orders (Preispunkte).
- `MatchCount` – Anzahl aufeinanderfolgender Kerzen, die vor der Orderplatzierung erforderlich sind.
- `CandleType` – Kerzen-Zeitrahmen für die Analyse.

Die Standardwerte emulieren das originale MQL-Skript und verwenden 4-Stunden-Kerzen.
