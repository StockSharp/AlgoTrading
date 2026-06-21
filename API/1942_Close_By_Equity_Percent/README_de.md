# Strategie zum Schließen nach Kapital-Prozentsatz
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Risikomanagement-Strategie überwacht das Portfolio-Kapital und schließt jede offene Position, wenn das Kapital über das aktuelle Guthaben multipliziert mit einem benutzerdefinierten Multiplikator steigt. Sie ist darauf ausgelegt, Gewinne zu sichern, sobald der Kontowert einen gewünschten Prozentsatz über dem Basiswert erreicht.

Die Strategie führt regelmäßige Überprüfungen mithilfe von Kerzen durch und generiert selbst keine Handelseinstiege; sie verwaltet lediglich eine bestehende Position. Nach dem Schließen wird der Referenzkontostand aktualisiert, sodass der Prozess für nachfolgende Trades wiederholt werden kann.

## Details

- **Einstiegskriterien**: Keine (verwaltet bestehende Position).
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Kapital größer als `balance * EquityPercentFromBalance`.
- **Stops**: Nein.
- **Standardwerte**:
  - `EquityPercentFromBalance` = 1.2m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Risikomanagement
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig

