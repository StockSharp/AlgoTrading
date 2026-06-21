# Strategie: Ausbruch aus dem vorherigen Hoch/Tief
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ausbruchsstrategie, die das vorherige Hoch und Tief einer Kerze im gewählten Zeitrahmen überwacht. Eine Long-Position wird eröffnet, wenn die neue Kerze über dem vorherigen Hoch schließt, während eine Short-Position eröffnet wird, wenn der Schlusskurs unter das vorherige Tief fällt. Ein Trailing-Stop und ein fester Take-Profit steuern das Risiko und sichern Gewinne ab.

Die Methode zielt darauf ab, starke Richtungsbewegungen nach einer Konsolidierung zu erfassen. Trailing-Stops halten das Risiko eng, wenn sich der Kurs in die günstige Richtung bewegt.

## Details

- **Einstiegskriterien**:
  - Long: `Close > PreviousHigh`
  - Short: `Close < PreviousLow`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Stop-Loss oder Take-Profit
- **Stops**: Absolut mit Trailing über `StopLoss` und `TakeProfit`
- **Standardwerte**:
  - `StopLoss` = 50m
  - `TakeProfit` = 1000m
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja (Trailing)
  - Komplexität: Anfänger
  - Zeitrahmen: Langfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
