# Strategie Künstliche-Intelligenz-Beschleuniger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert ein einfaches Perzeptron-Modell auf Basis von Bill Williams' **Acceleration/Deceleration Oscillator (AC)**. Vier Oszillatorwerte bei Verzögerungen von 0, 7, 14 und 21 Bars werden abgetastet und mit einstellbaren Gewichten multipliziert. Die gewichtete Summe dient als Entscheidungssignal: Positive Werte deuten auf bullischen Schwung hin, negative auf bärischen. Die Strategie kehrt ihre Position um, sobald das Signal das Vorzeichen wechselt, und setzt einen festen Stop-Loss vom Einstiegspreis.

Der AC selbst ergibt sich aus dem Awesome Oscillator (AO), indem ein 5-Perioden-Durchschnitt vom AO abgezogen wird. Dadurch ist die Strategie sensibel für Veränderungen in der Marktbeschleunigung.

## Details

- **Einstiegskriterien**:
  - **Long**: Perzeptron-Signal > 0.
  - **Short**: Perzeptron-Signal < 0.
- **Long/Short**: Beide Seiten; die Strategie kehrt um, wenn das Signal wechselt.
- **Ausstiegskriterien**:
  - Stop-Loss vom Einstiegspreis ausgelöst.
  - Umkehr, wenn Signal die Nulllinie kreuzt.
- **Stops**: Ja, fester Stop-Loss in Preiseinheiten.
- **Standardwerte**:
  - `X1` = 76
  - `X2` = 47
  - `X3` = 153
  - `X4` = 135
  - `StopLoss` = 8355
  - `CandleType` = 1-Minuten-Kerzen
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: AC (abgeleitet von AO)
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Kurzfristig
  - Neuronale Netze: Perzeptron
  - Risikolevel: Hoch
