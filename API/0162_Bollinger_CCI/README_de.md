# Bollinger Cci Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementierung der Strategie - Bollinger Bands + CCI. Kaufen, wenn der Preis unterhalb des unteren Bollinger Bands liegt und der CCI unter -100 (überverkauft) ist. Verkaufen, wenn der Preis oberhalb des oberen Bollinger Bands liegt und der CCI über 100 (überkauft) ist.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 73%. Die Strategie funktioniert am besten auf dem Kryptomarkt.

Bollinger Bands kartieren die Volatilitätsgrenzen, und der CCI misst den Abstand vom Mittelwert. Ausbrüche über eine Band mit CCI-Bestätigung lösen Trades aus.

Geeignet für volatile Märkte, in denen sich Trends schnell ausdehnen. ATR-basierte Stops werden zur Sicherheit angewendet.

## Details

- **Einstiegskriterien**:
  - Long: `Close < LowerBand && CCI < CciOversold`
  - Short: `Close > UpperBand && CCI > CciOverbought`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Preis kehrt zum mittleren Band zurück
- **Stops**: ATR-basiert mit `StopLoss`
- **Standardwerte**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `CciPeriod` = 20
  - `CciOversold` = -100m
  - `CciOverbought` = 100m
  - `StopLoss` = new Unit(2, UnitTypes.Absolute)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, CCI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

