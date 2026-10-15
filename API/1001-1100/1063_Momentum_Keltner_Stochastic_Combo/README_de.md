# Momentum Keltner Stochastic Combo-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die den Momentum-Vergleich mit einem Keltner-basierten Stochastic-Oszillator kombiniert.  
Positionen werden dynamisch basierend auf dem Eigenkapital skaliert und durch einen festen Stop-Loss geschützt.

## Details

- **Einstiegskriterien**:  
  - Long: `Momentum > 0` und `KeltnerStoch < Threshold`  
  - Short: `Momentum < 0` und `KeltnerStoch > Threshold`
- **Long/Short**: Beide  
- **Ausstiegskriterien**:  
  - Long: `KeltnerStoch > Threshold`  
  - Short: `KeltnerStoch < Threshold`
- **Stops**: Festes `SlPoints` unter/über dem Einstieg  
- **Standardwerte**:  
  - `MomLength` = 7  
  - `KeltnerLength` = 9  
  - `KeltnerMultiplier` = 0.5  
  - `Threshold` = 99  
  - `AtrLength` = 20  
  - `SlPoints` = 1185  
  - `EnableScaling` = true  
  - `BaseContracts` = 1  
  - `InitialCapital` = 30000  
  - `EquityStep` = 150000  
  - `MaxContracts` = 15  
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:  
  - Kategorie: Trendfolge  
  - Richtung: Beide  
  - Indikatoren: Momentum, EMA, ATR  
  - Stops: Ja  
  - Komplexität: Mittel  
  - Zeitrahmen: Mittelfristig  
  - Saisonalität: Nein  
  - Neuronale Netze: Nein  
  - Divergenz: Nein  
  - Risikolevel: Mittel
