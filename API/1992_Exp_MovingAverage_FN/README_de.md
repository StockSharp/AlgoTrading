# Exp Moving Average FN-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis von Richtungsumkehrungen der Steigung eines exponentiellen gleitenden Durchschnitts (EMA). Sie geht Long, wenn die EMA nach einem Rückgang nach oben dreht, und Short, wenn die EMA nach einem Anstieg nach unten dreht. Optionale Stop-Loss- und Take-Profit-Level werden in absoluten Preiseinheiten definiert.

## Details

- **Einstiegskriterien**:
  - **Long**: Die EMA-Steigung wechselt von fallend zu steigend.
  - **Short**: Die EMA-Steigung wechselt von steigend zu fallend.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Entgegengesetzte Steigungsumkehr.
  - Stop-Loss oder Take-Profit erreicht.
- **Stops**: Ja, mit absoluten Preisabständen.
- **Standardwerte**:
  - `EMA Length` = 12
  - `Stop Loss` = 1000
  - `Take Profit` = 2000
  - `Candle Type` = 4-Stunden-Zeitrahmen
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Einzeln (EMA)
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
