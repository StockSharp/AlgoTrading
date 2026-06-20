# Gamma-gewichtetes Momentum-Modell für BTC-Futures
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie berechnet einen Gamma-gewichteten Durchschnittspreis (GWAP), um den Momentum in BTC-Futures zu erfassen. Long-Trades werden eröffnet, wenn der Preis über dem GWAP bleibt und die letzten drei Schlusskurse aufeinanderfolgend steigen. Short-Positionen werden eingegangen, wenn der Preis unter dem GWAP liegt und die letzten drei Schlusskurse aufeinanderfolgend fallen.

## Details

- **Einstiegskriterien**:
  - **Long**: Schlusskurs über GWAP und die letzten drei Schlusskurse steigen.
  - **Short**: Schlusskurs unter GWAP und die letzten drei Schlusskurse fallen.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Umgekehrtes Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `Length` = 60
  - `GammaFactor` = 0.75
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: GWAP
  - Stops: Keine
  - Komplexität: Niedrig
  - Zeitrahmen: 1m
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
