# Projektions-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie berechnet die durchschnittliche prozentuale Veränderung der letzten täglichen Eröffnungen und projiziert Ausbruchsniveaus um die Eröffnung des aktuellen Tages. Long-Positionen werden eröffnet, wenn der Preis über die obere Projektion ausbricht, während Short-Positionen bei einem Bruch unter die untere Projektion geöffnet werden. Schutz-Stops werden in der Nähe der gegenüberliegenden Seite der Projektion platziert.

## Details

- **Einstiegskriterien**:
  - **Long**: Preis kreuzt über `open + threshold`.
  - **Short**: Preis kreuzt unter `open - threshold`.
- **Ausstiegskriterien**:
  - **Long**: Preis fällt unter den Long-Stop.
  - **Short**: Preis steigt über den Short-Stop.
- **Stops**: Ja, basierend auf der durchschnittlichen Veränderung.
- **Parameter**:
  - `TargetMultiple` – Multiplikator für die durchschnittliche Veränderung (Standard 0.2).
  - `Threshold` – Prozentsatz der durchschnittlichen Veränderung zur Bildung von Ausbruchsniveaus (Standard 1.0).
  - `CalculationPeriod` – Anzahl der Tage für den Durchschnitt (Standard 5).
