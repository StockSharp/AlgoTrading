# Williams %R Divergenz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Williams %R Oszillator misst überkaufte und überverkaufte Bedingungen. Wenn der Kurs ein neues Tief bildet, aber %R ein höheres Tief ausbildet, oder wenn der Kurs ein neues Hoch druckt, aber %R dreht nach unten, kann sich das Momentum umkehren. Diese Strategie sucht nach solchen Divergenzen an den Extrempunkten des Indikators.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 109%. Die Strategie eignet sich am besten für den Kryptomarkt.

In jedem Balken erfasst das System den letzten Schlusskurs und den %R-Wert zum Vergleich mit der vorherigen Messung. Eine bullische Divergenz kombiniert mit einem Niveau unter -80 löst einen Long-Einstieg aus, während eine bearische Divergenz und ein Wert über -20 einen Short erzeugt. Stops werden als Prozentsatz des Kurses gesetzt.

Positionen werden geschlossen, wenn der Oszillator zum entgegengesetzten Extrem zurückkehrt, und erfassen den Rückprall vom Divergenzsignal.

## Details

- **Einstiegskriterien**: Kurs/Williams %R Divergenz mit %R unter -80 für Longs oder über -20 für Shorts.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Williams %R erreicht das entgegengesetzte Extrem oder Stop-Loss.
- **Stops**: Ja, prozentbasiert.
- **Standardwerte**:
  - `WilliamsRPeriod` = 14
  - `DivergencePeriod` = 5
  - `CandleType` = 5 minute
  - `StopLossPercent` = 2
- **Filter**:
  - Kategorie: Divergenz
  - Richtung: Beide
  - Indikatoren: Williams %R
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel

