# Coensio Swing-Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trendlinien-Ausbruchsstrategie mit benutzerdefinierten Trendlinien. Die Strategie berechnet lineare Projektionen aus den Steigungsparametern und Achsenabschnitten für bullische und bärische Linien. Wenn der Schlusskurs die projizierte Kauflinie um einen Schwellenwert überschreitet, wird eine Long-Position eröffnet. Wenn der Kurs unter die Verkaufslinie minus dem Schwellenwert fällt, wird eine Short-Position eingegangen.

Positionen werden durch Take-Profit- und Stop-Loss-Werte in Ticks geschützt. Ein optionaler Trailing-Stop aktualisiert den Schutz-Stop, wenn sich der Preis in die gewünschte Richtung bewegt. Eine zusätzliche Option schließt den Trade, wenn der Ausbruch bei der nächsten Kerze scheitert.

## Details

- **Einstiegskriterien**:
  - Long: `Close > BuyLine + EntryThreshold`
  - Short: `Close < SellLine - EntryThreshold`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss, Take-Profit, Trailing-Stop oder entgegengesetztes Signal
- **Stops**:
  - Take-Profit in Ticks
  - Stop-Loss in Ticks
  - Optionaler Trailing-Stop in Ticks
  - Optionaler Schlusskurs bei Fehlausbruch an der nächsten Kerze
- **Standardwerte**:
  - `EntryThreshold` = 15m
  - `StopLossTicks` = 50
  - `TakeProfitTicks` = 100
  - `EnableTrailing` = false
  - `TrailingStepTicks` = 5
  - `FalseBreakClose` = true
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `BuyLineSlope` = 0m
  - `BuyLineIntercept` = 0m
  - `SellLineSlope` = 0m
  - `SellLineIntercept` = 0m
- **Filter**:
  - Kategorie: Trendlinienausbruch
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
