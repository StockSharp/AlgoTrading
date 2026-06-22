# RSI-Schwellenwert-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konvertiert den MetaTrader *Exp_RSI* Experten in StockSharp. Die Strategie öffnet und schließt Positionen, wenn der Relative Strength Index (RSI) vordefinierte Überkauf- und Überverkauft-Niveaus kreuzt.

## Details

- **Einstiegskriterien**:
  - **Long**: RSI kreuzt über `RSI Low Level`.
  - **Short**: RSI kreuzt unter `RSI High Level`.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - Gegensignal oder Stop-Parameter.
- **Stops**: Take Profit und Stop Loss in absoluten Preiseinheiten.
- **Standardwerte**:
  - `RSI Period` = 14
  - `RSI High Level` = 60
  - `RSI Low Level` = 40
  - `Stop Loss` = 1000
  - `Take Profit` = 2000
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: Einzeln
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: H4
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
