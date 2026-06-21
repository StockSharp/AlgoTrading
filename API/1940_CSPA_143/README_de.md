# CSPA-1.43-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine Adaption des MQL-Expertenberaters **CSPA-1_43**. Sie misst die Stärke eines Währungspaares mit dem Relative Strength Index (RSI). Wenn das Paar ausreichend stark oder schwach wird, eröffnet die Strategie eine Position in Richtung des vorherrschenden Momentums und schließt sie, wenn das Momentum nachlässt.

## Logik

- Kerzen des ausgewählten Wertpapiers abonnieren.
- Den RSI-Wert für jede abgeschlossene Kerze berechnen.
- Eine Long-Position öffnen, wenn RSI über den oberen Schwellenwert steigt.
- Eine Short-Position öffnen, wenn RSI unter den unteren Schwellenwert fällt.
- Die aktuelle Position schließen, wenn RSI in die neutrale Zone zurückkehrt.

## Parameter

| Name | Beschreibung | Standard |
|------|--------------|----------|
| `StrengthPeriod` | Periode des RSI-Indikators. | `14` |
| `Threshold` | Abstand vom neutralen RSI-Niveau von 50 zur Signalgenerierung. | `10` |
| `CandleType` | Zeitrahmen der Kerzen. | `1 Stunde` |

## Hinweise

- Die Strategie verwendet die High-Level-API mit automatischer Indikatorbindung.
- Orders werden mit Market-Orders ausgeführt (`BuyMarket` und `SellMarket`).
- Nur abgeschlossene Kerzen werden verarbeitet.
