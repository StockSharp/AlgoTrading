# Liquidex-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ausbruchsstrategie, die eintritt, wenn der Preis die Keltner-Kanal-Bänder verlässt, und das Risiko mit Stop-Loss, Take-Profit, Break-Even und Trailing Stop verwaltet.

## Details

- **Einstiegskriterien**:
  - Long: Schluss oberhalb des oberen Keltner-Bandes.
  - Short: Schluss unterhalb des unteren Keltner-Bandes.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Stop-Loss- oder Take-Profit-Level erreicht.
  - Stop nach Gewinnschwelle auf Break-Even verschoben.
  - Trailing Stop aktiviert.
- **Stops**: Ja.
- **Standardwerte**:
  - `KcPeriod` = 10
  - `UseKcFilter` = true
  - `StopLoss` = 30
  - `TakeProfit` = 0
  - `MoveToBe` = 15
  - `MoveToBeOffset` = 2
  - `TrailingDistance` = 5
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filter**:
  - Kategorie: Kanal
  - Richtung: Beide
  - Indikatoren: Keltner
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
