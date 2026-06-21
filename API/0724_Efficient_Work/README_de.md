# Efficient Work-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet gleitende Durchschnitte auf kurzen, mittleren und langen Horizonten. Eine Long-Position wird eröffnet, wenn der schnelle Durchschnitt über beiden längeren Durchschnitten liegt, und eine Short-Position, wenn er darunter liegt.

## Details

- **Einstiegskriterien**:
  - **Long**: `fast MA > medium MA` und `fast MA > high MA`.
  - **Short**: `fast MA < medium MA` und `fast MA < high MA`.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Ein entgegengesetztes Signal löst eine Umkehr aus.
- **Stops**: Keine.
- **Standardwerte**:
  - `MA Period` = 20
  - `Medium TF Multiplier` = 5
  - `High TF Multiplier` = 10
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
