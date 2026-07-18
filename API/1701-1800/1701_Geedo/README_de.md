# Geedo-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Zeitbasierte Strategie, die Eröffnungskurse zweier vergangener Bars zu einer bestimmten Stunde vergleicht. Liegt der ältere Bar über dem neueren um einen Schwellenwert, wird ein Short-Trade eröffnet. Liegt der neuere Bar über dem älteren, wird ein Long-Trade eröffnet. Jede Position verwendet festen Stop-Loss und Take-Profit und wird nach einer maximalen Haltezeit geschlossen.

## Details

- **Einstiegskriterien**: Um `TradeTime` werden die Eröffnungskurse `T1` und `T2` Bars zurück verglichen. Wenn `Open[T1] - Open[T2]` `DeltaShort` übersteigt, verkaufen; wenn `Open[T2] - Open[T1]` `DeltaLong` übersteigt, kaufen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop-Loss, Take-Profit oder `MaxOpenTime` Stunden nach dem Einstieg.
- **Stops**: Fester Stop-Loss und Take-Profit in Punkten.
- **Standardwerte**:
  - `TakeProfitLong` = 39
  - `StopLossLong` = 147
  - `TakeProfitShort` = 15
  - `StopLossShort` = 6000
  - `TradeTime` = 18
  - `T1` = 6
  - `T2` = 2
  - `DeltaLong` = 6
  - `DeltaShort` = 21
  - `Volume` = 0.01
  - `MaxOpenTime` = 504
- **Filter**:
  - Kategorie: Zeitbasiert
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Fest
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday (1h)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
