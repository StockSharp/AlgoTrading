# Strategie mit anpassbarem MA und alternierenden Extrempunkten
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie verwendet Bollinger-Bänder, um den anpassbaren gleitenden Durchschnitt mit alternierenden Extrempunkten zu emulieren. Eine Long-Position wird eröffnet, wenn der Preis über das obere Band ausbricht, während eine Short-Position eröffnet wird, wenn der Preis unter das untere Band fällt. Der Überschreitungszustand wechselt ab und verhindert aufeinanderfolgende Trades in dieselbe Richtung.

## Details

- **Einstiegskriterien**:
  - Long gehen, wenn das Kerzenhoch über das obere Band kreuzt.
  - Short gehen, wenn das Kerzentief unter das untere Band kreuzt.
- **Ausstiegskriterien**:
  - Ausbruch des gegenüberliegenden Bandes.
- **Indikatoren**: Bollinger-Bänder (SMA + Standardabweichung).
- **Standardwerte**:
  - Length = 50
  - Multiplier = 2
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Zeitrahmen: Kurz-/mittelfristig
  - Risikolevel: Mittel
