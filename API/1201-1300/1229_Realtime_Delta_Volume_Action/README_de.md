# Strategie für Echtzeit-Delta-Volumen-Aktion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verfolgt den Unterschied zwischen Kauf- und Verkaufsvolumen innerhalb jeder Kerze. Ein Trade wird eröffnet, wenn das Volumen-Delta einen Schwellenwert überschreitet.

## Details

- **Einstiegskriterien**: Volumen-Delta über/unter dem Schwellenwert.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegenläufiges Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `DeltaThreshold` = 100
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Volume Delta
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
