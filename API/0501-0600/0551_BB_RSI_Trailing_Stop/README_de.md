# BB RSI Trailing-Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert Bollinger Bands mit RSI-Momentum und schützt Trades mit einem bedingten Trailing-Stop.
Long-Positionen entstehen, wenn der Kurs die untere Band durchbricht und der RSI die überverkaufte Zone verlässt. Shorts werden ausgelöst, wenn die obere Band mit überkauftem RSI getroffen wird.

Der Stop-Loss beginnt mit einem festen Abstand und wechselt zu einem Trailing-Stop, sobald sich der Kurs um einen voreingestellten Versatz günstig bewegt hat.

## Details

- **Einstiegskriterien**: Bollinger-Band-Ausbruch mit RSI-Bestätigung
- **Long/Short**: Beide
- **Ausstiegskriterien**: Anfänglicher Stop-Loss oder Trailing-Stop
- **Stops**: Ja, dynamisches Trailing
- **Standardwerte**:
  - `BollingerPeriod` = 25
  - `BollingerDeviation` = 2
  - `RsiPeriod` = 14
  - `RsiOverbought` = 60
  - `RsiOversold` = 33
  - `StopLossPoints` = 50
  - `TrailOffsetPoints` = 99
  - `TrailStopPoints` = 40
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, RSI
  - Stops: Trailing
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
