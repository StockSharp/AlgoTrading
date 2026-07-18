# BTCUSD Momentum-Strategie nach Abnormalen Tagen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie misst die Tagesrendite als `(close - open) / open` und vergleicht sie mit einem gleitenden Durchschnitt und einer Standardabweichung über einen konfigurierbaren Zeitraum. Überschreitet die Rendite den oberen Schwellenwert, wird eine Long-Position eröffnet; fällt sie unter den unteren Schwellenwert, wird eine Short-Position eröffnet. Alle Positionen werden zum Schlusskurs des nächsten Tages geschlossen.

## Details

- **Einstiegskriterien**:
  - Rendite > Mittelwert + k × Std → Long.
  - Rendite < Mittelwert - k × Std → Short.
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Alle Positionen zum Schlusskurs des nächsten Tages schließen.
- **Stops**: Keine
- **Standardwerte**:
  - Lookback-Periode = 5
  - Schwellenwert für abnormale Rendite (k) = 1.6
  - Kapital pro Trade = 1000
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: SMA, StandardDeviation
  - Stops: Keine
  - Komplexität: Niedrig
  - Zeitrahmen: Langfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
