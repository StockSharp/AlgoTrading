# STLMCandle-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt basierend auf der Richtung der letzten abgeschlossenen Kerze.
Wenn der Schlusskurs über dem Eröffnungskurs liegt, wird eine Long-Position eröffnet und jede Short-Position geschlossen.
Wenn der Schlusskurs unter dem Eröffnungskurs liegt, wird eine Short-Position eröffnet und jede Long-Position geschlossen.
Sie unterstützt Stop-Loss- und Take-Profit-Levels und arbeitet mit einem konfigurierbaren Kerzen-Zeitrahmen.

## Parameter
- `CandleType` – Zeitrahmen der Kerzen für die Analyse.
- `StopLoss` – absoluter Stop-Loss-Wert in Preiseinheiten.
- `TakeProfit` – absoluter Take-Profit-Wert in Preiseinheiten.

## Hinweise
Die Strategie ist eine vereinfachte Anpassung des ursprünglichen MQL-Experten `STLMCandle`.
Sie nähert den Indikator durch Verwendung der Standard-Kerzen-Eröffnungs- und Schlusskurse an.
