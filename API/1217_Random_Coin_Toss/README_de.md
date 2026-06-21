# Zufälliger Münzwurf-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese experimentelle Strategie wirft alle N Bars eine Münze und geht je nach Ergebnis long oder short. Das Risiko wird durch ATR-basierte Stop-Loss- und Take-Profit-Niveaus gesteuert.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 8%. Sie funktioniert am besten auf dem Kryptomarkt.

Die Idee ist, eine Basislinie für zufällige Einstiege bei gleichzeitig disziplinierten Ausstiegen zu schaffen.

## Details

- **Einstiegskriterien**: Alle `EntryFrequency` Bars wird eine Münze geworfen; Kopf geht long, Zahl geht short.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit erreicht.
- **Stops**: Ja.
- **Standardwerte**:
  - `AtrLength` = 14
  - `SlMultiplier` = 1m
  - `TpMultiplier` = 2m
  - `EntryFrequency` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Experimentell
  - Richtung: Beide
  - Indikatoren: ATR
  - Stops: Ja
  - Komplexität: Einfach
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch

