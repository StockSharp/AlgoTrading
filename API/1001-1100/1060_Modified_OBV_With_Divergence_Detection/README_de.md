# Modifizierter OBV mit Divergenz-Erkennung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie glättet den On-Balance Volume (OBV) mit einem wählbaren gleitenden Durchschnitt und erzeugt eine Signallinie. Trades entstehen, wenn der geglättete OBV die Signallinie kreuzt. Zusätzlich protokolliert die Strategie reguläre und versteckte Divergenzen zwischen Preis und OBV mithilfe von Fraktal-Erkennung.

## Details

- **Einstiegskriterien**: OBV-M kreuzt die Signallinie von unten/oben.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzter Kreuzungspunkt.
- **Stops**: Nein.
- **Standardwerte**:
  - `MaType` = Exponential
  - `ObvMaLength` = 7
  - `SignalLength` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Divergenz
  - Richtung: Beide
  - Indikatoren: OBV, MA
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
