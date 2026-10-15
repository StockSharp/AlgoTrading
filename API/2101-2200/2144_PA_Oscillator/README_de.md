# PA-Oszillator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein Port des MQL5-Experten **Exp_PA_Oscillator.mq5**. Sie wendet zwei exponentielle gleitende Durchschnitte (EMAs) auf die Schlusskurse der Kerzen an und analysiert die Ableitung ihrer Differenz.

## Logik

1. Schnellen und langsamen EMA berechnen.
2. Die Differenz zwischen ihnen berechnen und ihre Änderung gegenüber dem vorherigen Wert verfolgen.
3. Einen Farbcode für die Ableitung bestimmen:
   - **0** – Ableitung ist positiv und MACD steigt.
   - **1** – Ableitung ist null.
   - **2** – Ableitung ist negativ und MACD fällt.
4. Die Farben der zwei letzten abgeschlossenen Kerzen verwenden, um Signale zu generieren:
   - Vor zwei Balken war die Farbe `0` und der vorherige Balken wechselte von `0` weg → Long-Position öffnen und Short-Position schließen.
   - Vor zwei Balken war die Farbe `2` und der vorherige Balken wechselte von `2` weg → Short-Position öffnen und Long-Position schließen.

## Parameter

| Name | Beschreibung |
| ---- | ------------ |
| `FastLength` | Länge des schnellen EMA. |
| `SlowLength` | Länge des langsamen EMA. |
| `BuyPosOpen` | Öffnen von Long-Positionen aktivieren. |
| `SellPosOpen` | Öffnen von Short-Positionen aktivieren. |
| `BuyPosClose` | Schließen von Long-Positionen aktivieren. |
| `SellPosClose` | Schließen von Short-Positionen aktivieren. |
| `CandleType` | Kerzen-Zeitrahmen für Berechnungen. |

## Hinweise

- Es werden nur abgeschlossene Kerzen verarbeitet.
- Marktorders werden für Ein- und Ausstiege verwendet.
- Diese Implementierung legt den Fokus auf Verständlichkeit und Lernzwecke, nicht auf Profitabilität.
