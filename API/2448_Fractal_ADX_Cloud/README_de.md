# Fractal ADX Wolke
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie nähert sich dem originalen MQL-Expert `Fractal_ADX_Cloud` an, indem sie den Average Directional Index-Indikator in StockSharp verwendet. Sie arbeitet mit Vier-Stunden-Kerzen und analysiert die Kreuzung der +DI- und -DI-Komponenten. Wenn die bullische Komponente (+DI) über die bearische (-DI) steigt, schließt die Strategie alle Short-Positionen und kann eine neue Long-Position eröffnen. Wenn -DI über +DI steigt, wird die Logik für Short-Trades gespiegelt.

Stop-Loss- und Take-Profit-Schutz werden in absoluten Preiseinheiten angewendet. Zusätzliche Parameter ermöglichen das separate Aktivieren oder Deaktivieren des Öffnens und Schließens von Positionen in jede Richtung.

## Details

- **Einstiegskriterien**: Kreuzung der +DI- und -DI-Linien des ADX.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Stop.
- **Stops**: Ja, mit absoluten Preisabständen.
- **Standardwerte**:
  - `AdxPeriod` = 30
  - `StopLoss` = 1000m
  - `TakeProfit` = 2000m
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `CandleType` = TimeSpan.FromHours(4)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: ADX
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: 4h
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
