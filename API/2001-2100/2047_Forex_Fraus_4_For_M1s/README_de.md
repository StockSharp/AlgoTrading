# Forex Fraus 4 For M1s-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konvertierung der MQL4-Strategie #13643. Der ursprüngliche Expert Advisor eröffnet Trades, wenn der Williams-%R-Indikator extreme Niveaus berührt und dann zurückkreuzt. Diese C#-Version verwendet die High-Level-API von StockSharp.

Die Strategie arbeitet auf 1-Minuten-Kerzen und reagiert auf zwei Schlüsselniveaus:
- Ein Long-Signal wird generiert, nachdem Williams %R über -99.9 steigt, nachdem er darunter war.
- Ein Short-Signal erscheint, wenn Williams %R unter -0.1 fällt, nachdem er darüber war.

Positionen werden durch feste Stops, Ziele oder Trailing Stop geschlossen. Ein Zeitfilter kann den Handel auf ein bestimmtes Intraday-Fenster beschränken.

## Details

- **Einstiegskriterien**  
  - Long: `WilliamsR` kreuzt `BuyThreshold` (-99.9) nach oben, nachdem er darunter war.  
  - Short: `WilliamsR` kreuzt `SellThreshold` (-0.1) nach unten, nachdem er darüber war.
- **Long/Short**: Beide
- **Ausstiegskriterien**  
  - Preis erreicht Stop-Loss (`StopLoss`) oder Take-Profit (`TakeProfit`)  
  - Trailing Stop (`TrailingStop`) bei Aktivierung
- **Stops**: Schrittbasiert
- **Standardwerte**  
  - `WprPeriod` = 360  
  - `BuyThreshold` = -99.9  
  - `SellThreshold` = -0.1  
  - `StopLoss` = 0  
  - `TakeProfit` = 0  
  - `UseProfitTrailing` = true  
  - `TrailingStop` = 30  
  - `TrailingStep` = 1  
  - `UseTimeFilter` = false  
  - `StartHour` = 7  
  - `StopHour` = 17  
  - `Volume` = 0.01  
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**  
  - Kategorie: Trendumkehr  
  - Richtung: Beide  
  - Indikatoren: Williams %R  
  - Stops: Ja  
  - Komplexität: Grundlegend  
  - Zeitrahmen: Intraday (M1)  
  - Saisonalität: Nein  
  - Neuronale Netze: Nein  
  - Divergenz: Nein  
  - Risikolevel: Mittel
