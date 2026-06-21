# Larry Conners VIX Reversal II-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie handelt auf Basis des RSI des VIX-Index. Eine Long-Position wird eröffnet, wenn der VIX-RSI den Überkauft-Level nach oben kreuzt. Eine Short-Position wird eröffnet, wenn der RSI den Überverkauft-Level nach unten kreuzt. Positionen werden nach einer Mindesthaltedauer geschlossen.

## Details

- **Einstiegskriterien**:
  - **Long**: RSI(VIX) kreuzt `Overbought level` nach oben.
  - **Short**: RSI(VIX) kreuzt `Oversold level` nach unten.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Position nach `Min holding days` bis `Max holding days` schließen.
- **Stops**: Keine.
- **Standardwerte**:
  - `RSI period` = 25
  - `Overbought level` = 61
  - `Oversold level` = 42
  - `Min holding days` = 7
  - `Max holding days` = 12
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
