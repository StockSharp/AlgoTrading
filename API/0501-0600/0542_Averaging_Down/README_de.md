# Strategie des Nachkaufens bei fallenden Kursen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Averaging-Down-Strategie kauft, wenn der Relative Strength Index (RSI) unter einen definierten Schwellenwert fällt. Jedes Signal wird zur bestehenden Long-Position hinzugefügt und mittelt den Einstiegspreis. Die Strategie steigt aus, wenn der Schlusskurs über das Hoch des vorherigen Balkens bricht.

## Details

- **Einstiegskriterien**:
  - RSI unter `RsiBuyThreshold`.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Schlusskurs überschreitet das Hoch des vorherigen Balkens.
- **Stops**: Keine.
- **Standardwerte**:
  - `RsiLength` = 10
  - `RsiBuyThreshold` = 33
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Long
  - Indikatoren: RSI
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
