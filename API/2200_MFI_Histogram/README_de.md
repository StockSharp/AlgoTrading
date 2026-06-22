# MFI-Histogramm-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die MFI-Histogramm-Strategie verwendet den Money Flow Index (MFI), um überkaufte und überverkaufte Bedingungen über konfigurierbare Schwellenwerte zu erkennen. Der MFI kombiniert Preis und Volumen, um die Intensität von Kapitalzu- und -abflüssen zu messen. Wenn der Indikator von unten über das hohe Niveau steigt, interpretiert die Strategie dies als steigenden Kaufdruck, eröffnet eine Long-Position und schließt alle bestehenden Short-Positionen. Umgekehrt löst ein Fallen unter das niedrige Niveau einen Short-Einstieg aus und schließt bestehende Long-Positionen. Stop-Loss- und Take-Profit-Werte werden in Ticks über den integrierten Schutzmechanismus verwaltet.

Die Strategie arbeitet auf einem benutzerdefinierten Kerzen-Zeitrahmen (standardmäßig 4 Stunden) und basiert auf einem einzigen Indikator ohne zusätzliche Filter. Parameter ermöglichen die Optimierung des MFI-Zeitraums, der Schwellenwerte und der Risikolimits, wodurch das System an verschiedene Märkte und Volatilitätsregimes anpassbar ist.

## Details

- **Einstiegskriterien**:
  - **Long**: `MFI` kreuzt `HighLevel` von unten nach oben.
  - **Short**: `MFI` kreuzt `LowLevel` von oben nach unten.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Ein entgegengesetztes Signal erzeugt eine Umkehr.
  - Stop-Loss oder Take-Profit wird erreicht.
- **Stops**: `StopLoss` und `TakeProfit` in Ticks.
- **Standardwerte**:
  - `MFI Period` = 14
  - `HighLevel` = 60
  - `LowLevel` = 40
  - `Candle Type` = 4-hour
  - `StopLoss` = 1000 ticks
  - `TakeProfit` = 2000 ticks
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: Einzeln
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
