# Spread By-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Spread By verwendet einen gleitenden Durchschnitt mit Standardabweichungsbändern, um Preisextreme zu handeln.
Kauft, wenn der Preis unter das untere Band fällt, und verkauft, wenn der Preis über das obere Band steigt.

## Details

- **Einstiegskriterien**: Preis bewegt sich über ±1 Standardabweichung vom gleitenden Durchschnitt hinaus
- **Long/Short**: Beide
- **Ausstiegskriterien**: Preis kehrt zum gleitenden Durchschnitt zurück
- **Stops**: Nein
- **Standardwerte**:
  - `Length` = 100
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: SMA, StandardDeviation
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
