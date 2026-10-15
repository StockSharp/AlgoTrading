# 20/200 Expert-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet Trades auf Basis der Differenz zwischen den Eröffnungspreisen zweier vergangener Balken. Sie geht Long, wenn die Eröffnung bei Shift2 minus die Eröffnung bei Shift1 einen Schwellenwert überschreitet, und Short bei der entgegengesetzten Bedingung. Positionen werden nur zu einer bestimmten Stunde eröffnet und durch Take-Profit, Stop-Loss oder nach einer maximalen Haltezeit geschlossen.

## Details

- **Einstiegskriterien:**
  - Long: open[Shift2] - open[Shift1] > DeltaLong Punkte.
  - Short: open[Shift1] - open[Shift2] > DeltaShort Punkte.
- **Long/Short:** Beide.
- **Ausstiegskriterien:** Take-Profit, Stop-Loss oder maximale Haltezeit.
- **Stops:** Fester Stop-Loss und Take-Profit in Punkten.
- **Standardwerte:**
  - Shift1 = 6
  - Shift2 = 2
  - DeltaLong = 6 Punkte
  - DeltaShort = 21 Punkte
  - TakeProfitLong = 390 Punkte
  - StopLossLong = 1470 Punkte
  - TakeProfitShort = 320 Punkte
  - StopLossShort = 2670 Punkte
  - TradeHour = 14
  - MaxOpenTime = 504 Stunden
  - Volume = 0.1
  - Kerzen-Zeitrahmen = 1 Stunde
- **Filter:**
  - Kategorie: Momentum
  - Richtung: Long und Short
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Stündlich
  - Saisonalität: Zeitbasiert
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
