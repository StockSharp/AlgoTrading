# XMA Bereichskanal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die einen oberen und unteren Kanal aus gleitenden Durchschnitten der Höchst- und Tiefstkurse aufbaut. Ein Ausbruch über das obere Band löst einen Long-Einstieg aus, während ein Ausbruch unter das untere Band einen Short-Einstieg auslöst. Das Modell spiegelt das Verhalten des originalen MQL-Experten "XMA Range Channel" wider.

## Details

- **Einstiegskriterien**:
  - Long: `Close > UpperChannel`
  - Short: `Close < LowerChannel`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gegenteiliges Signal
- **Stops**: Nein
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
  - `Length` = 7
- **Filter**:
  - Kategorie: Kanalausbruch
  - Richtung: Beide
  - Indikatoren: SMA auf High/Low
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Swing
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
