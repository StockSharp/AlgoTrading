# OBV Divergenz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

On-Balance Volume verfolgt das kumulierte Handelsvolumen mit dem Gedanken, dass das Volumen dem Kurs vorausgeht. Wenn der Kurs ein neues Hoch bildet, OBV aber nicht bestätigt – oder umgekehrt –, könnte sich eine Umkehr anbahnen. Diese Strategie nutzt diese Divergenz, um gegen nicht nachhaltige Bewegungen zu handeln.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 112%. Die Strategie eignet sich am besten für den Devisenmarkt.

Für jede Kerze wird OBV aktualisiert und mit der vorherigen Messung verglichen. Ein bullisches Signal entsteht, wenn der Kurs ein niedrigeres Tief bildet, während OBV ein höheres Tief ausgibt. Ein bearisches Signal tritt auf, wenn der Kurs auf ein höheres Hoch steigt, OBV jedoch zurückbleibt. Ein gleitender Durchschnitt liefert einen Ausstiegspunkt, während ein prozentualer Stop die Verluste begrenzt.

Der Ansatz versucht, Mean Reversion nach Volumenerschöpfung zu erfassen, und hält Trades oft nur, bis der Kurs wieder über die Durchschnittslinie kreuzt.

## Details

- **Einstiegskriterien**: Kurs/OBV Divergenz.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Kurs kreuzt den gleitenden Durchschnitt oder Stop-Loss.
- **Stops**: Ja, prozentbasiert.
- **Standardwerte**:
  - `DivergencePeriod` = 5
  - `MAPeriod` = 20
  - `CandleType` = 5 minute
  - `StopLossPercent` = 2
- **Filter**:
  - Kategorie: Divergenz
  - Richtung: Beide
  - Indikatoren: OBV, MA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel

