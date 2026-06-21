# Wochenfaktor-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementiert das Weekly-Factor-Muster, das von Andrea Unger beschrieben wurde. Die Strategie handelt Ausbrüche über das Sitzungshoch oder -tief, wenn die Fünf-Tage-Spanne Kompression zeigt.

## Details
- **Einstiegskriterien**: Nach Sitzungsbeginn, wenn Weekly-Factor-Bedingung erfüllt und Kurs über Sitzungshoch -> Long; unter Sitzungstief -> Short.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Schließen bei neuer Sitzung oder nach zwei Tagen mit profitabler Position.
- **Stops**: Keine.
- **Standardwerte**:
  - `RangeFilter` = 0.5
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: Weekly factor
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: 15m
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
