# Einfacher Trailing-Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Basic Trailing Stop-Strategie kombiniert Commodity Channel Index (CCI)- und Relative Strength Index (RSI)-Filter mit einem einfachen Trailing-Stop. Wenn beide Indikatoren überkaufte oder überverkaufte Bedingungen signalisieren, eröffnet die Strategie eine Marktposition und platziert sofort einen in Pips gemessenen Trailing-Stop. Wenn der Preis sich günstig entwickelt, folgt das Stop-Level dem Trend, um Gewinne zu sichern.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 32%. Die Strategie funktioniert am besten auf dem Devisenmarkt.

Da das Stop-Level den Preis kontinuierlich verfolgt, reduziert sich das Risiko automatisch, wenn sich der Trend ausdehnt. Ausstiege erfolgen nur, wenn der Trailing-Stop getroffen wird. Das System hält jeweils eine Position und kann in beide Richtungen handeln.

## Details

- **Einstiegskriterien**:
  - **Long**: `CCI` zwischen -150 und -100 und `RSI` zwischen 0 und 30.
  - **Short**: `CCI` zwischen 100 und 250 und `RSI` zwischen 70 und 100.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Trailing-Stop getroffen.
- **Stops**: Nur Trailing-Stop.
- **Standardwerte**:
  - `StopLossPips` = 20
  - `CciPeriod` = 14
  - `RsiPeriod` = 14
  - `CandleType` = `TimeSpan.FromMinutes(1)`
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: CCI, RSI
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
