# London BreakOut Classic Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Ausbrüche der London-Session anhand der asiatischen Range. Das Hoch und Tief zwischen 00:00 und 06:55 UTC bilden eine Box. Nach 07:00 UTC eröffnet ein Ausbruch über das Hoch eine Long-Position und ein Ausbruch unter das Tief eine Short-Position. Der Stop-Loss wird in der Mitte der Box platziert und der Take-Profit verwendet einen konfigurierbaren Chance-Risiko-Faktor.

## Details

- **Einstiegskriterien**:
  - Long: Kurs bricht über das Hoch der asiatischen Session.
  - Short: Kurs bricht unter das Tief der asiatischen Session.
- **Ausstiegskriterien**:
  - Stop-Loss oder Take-Profit.
  - Ende des Handelsfensters.
- **Stops**: Ja.
- **Standardwerte**:
  - Asiatische Session: 00:00–06:55 UTC.
  - Handelssession: 07:00–16:00 UTC.
  - CRV = 1.
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
