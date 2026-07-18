# Delta RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis des **Delta RSI**-Indikators. Zwei RSI-Indikatoren mit unterschiedlichen Perioden werden verglichen:

- Der **schnelle RSI** reagiert schnell auf Preisänderungen.
- Der **langsame RSI** dient als Trendfilter.

Eine Long-Position wird in der Bar nach einem **Up**-Signal eröffnet, wenn:

1. Der langsame RSI über dem Schwellenwert `Level` liegt.
2. Der schnelle RSI höher ist als der langsame RSI.
3. Die vorherige Bar den Up-Zustand zeigte und die aktuelle Bar nicht mehr im Up-Zustand ist.

Eine Short-Position wird in der Bar nach einem **Down**-Signal eröffnet, wenn:

1. Der langsame RSI unter `100 - Level` liegt.
2. Der schnelle RSI niedriger ist als der langsame RSI.
3. Die vorherige Bar den Down-Zustand zeigte und die aktuelle Bar nicht mehr im Down-Zustand ist.

Optionale Flags ermöglichen das separate Aktivieren oder Deaktivieren des Öffnens und Schließens von Long- und Short-Positionen.

## Parameter

| Name | Beschreibung |
|------|--------------|
| `FastPeriod` | Periode des schnellen RSI. |
| `SlowPeriod` | Periode des langsamen RSI. |
| `Level` | Schwellenwert für den langsamen RSI. |
| `BuyPosOpen` / `SellPosOpen` | Öffnen von Long/Short-Positionen erlauben. |
| `BuyPosClose` / `SellPosClose` | Schließen von Long/Short-Positionen erlauben. |
| `CandleType` | Zeitrahmen der Eingabe-Kerzen. |

Die Strategie abonniert Kerzen des gewählten Zeitrahmens, berechnet beide RSI-Werte und verarbeitet Signale bei jeder abgeschlossenen Kerze. Wenn ein Signal erscheint, schließt die Strategie optional die entgegengesetzte Position und eröffnet eine neue in der Signalrichtung.
