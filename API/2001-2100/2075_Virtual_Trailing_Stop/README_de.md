# Virtueller Trailing-Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie emuliert einen virtuellen Trailing-Stop für Long- und Short-Positionen. Sie generiert keine Einstiegssignale; Orders müssen extern oder manuell geöffnet werden. Sobald eine Position vorhanden ist, verwaltet die Strategie einen Trailing-Stop, der dem Preis folgt, wenn er sich in eine günstige Richtung bewegt. Wenn der Preis das Stop-Level erreicht, wird die Position per Marktorder geschlossen.

## Parameter

- `StopLoss` – fester Stop-Loss-Abstand in Preisschritten.
- `TakeProfit` – fester Take-Profit-Abstand in Preisschritten.
- `TrailingStop` – Abstand vom aktuellen Preis zum Trailing-Stop.
- `TrailingStart` – minimaler Gewinn in Preisschritten, bevor das Trailing beginnt.
- `TrailingStep` – minimaler zusätzlicher Gewinn, der erforderlich ist, um das Trailing-Level zu verschieben.
- `CandleType` – Kerzenserie zur Verarbeitung der Preisdaten.

## Hinweise

Die Strategie abonniert Kerzen des angegebenen Typs und wertet die Trailing-Logik nur bei geschlossenen Kerzen aus.
