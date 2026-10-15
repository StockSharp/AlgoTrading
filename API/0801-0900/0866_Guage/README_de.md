# Gauge-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie spiegelt die TradingView-Gauge-Bibliothek wider, indem sie die Preisposition zwischen einem benutzerdefinierten Minimum und Maximum misst. Wenn der Prozentsatz die obere oder untere Schwelle kreuzt, werden Trades in der entsprechenden Richtung eröffnet.

## Details

- **Einstiegskriterien**:
  - **Long**: Gauge-Verhältnis über der oberen Schwelle.
  - **Short**: Gauge-Verhältnis unter der unteren Schwelle.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Ein entgegengesetztes Signal erzeugt einen Ausstieg.
- **Stops**: Keine.
- **Standardwerte**:
  - Min value = 0, Max value = 100.
  - Upper threshold = 75%, Lower threshold = 25%.
- **Filter**:
  - Kategorie: Bereich / Werkzeug
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
