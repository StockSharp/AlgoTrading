# Fxscalper-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Bollinger-Band-Ausbruch-Scalping-Strategie, übersetzt vom MQL4-Experten "fxscalper".
Die Strategie abonniert Kerzendaten und Bollinger Bands. Wenn der Schlusskurs über das obere Band ausbricht, wird eine Long-Position eröffnet; wenn der Schlusskurs unter das untere Band fällt, wird eine Short-Position eröffnet. Positionen werden durch Stop-Loss- und Take-Profit-Niveaus geschützt.

## Details

- **Einstiegskriterien**:
  - Long: `Close > Upper Band`
  - Short: `Close < Lower Band`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gegensätzliches Signal oder Schutz-Stops
- **Stops**: Stop-Loss und Take-Profit
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2
  - `StopLoss` = 200m
  - `TakeProfit` = 150m
- **Filter**:
  - Kategorie: Bollinger Bands
  - Richtung: Beide
  - Indikatoren: Bollinger Bands
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
