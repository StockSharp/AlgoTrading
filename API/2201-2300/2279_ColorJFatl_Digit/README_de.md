# ColorJFatl Digit Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet die Steigungsrichtung eines Jurik Moving Average (JMA), um Trades zu generieren. Der JMA approximiert den "ColorJFatl_Digit"-Indikator aus dem ursprünglichen MQL5-Experten. Eine Long-Position wird eröffnet, wenn der JMA aufwärts dreht, während eine Short-Position eröffnet wird, wenn der JMA abwärts dreht. Entgegengesetzte Positionen werden geschlossen, wenn sich die Steigung umkehrt.

Das System handelt in beide Richtungen und verwendet standardmäßig keine harten Stops. Es eignet sich für Instrumente, bei denen Trendwechsel durch einen glatten adaptiven gleitenden Durchschnitt erfasst werden können.

## Details

- **Einstiegskriterien**:
  - **Long**: JMA-Steigung wechselt von negativ zu positiv.
  - **Short**: JMA-Steigung wechselt von positiv zu negativ.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: JMA-Steigung wird negativ.
  - **Short**: JMA-Steigung wird positiv.
- **Stops**: Standardmäßig keine.
- **Standardwerte**:
  - `JMA Length` = 5
  - `Timeframe` = 4 Stunden
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Einzeln
  - Stops: Nein
  - Komplexität: Einfach
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
