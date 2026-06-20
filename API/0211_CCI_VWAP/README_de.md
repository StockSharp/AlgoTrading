# CCI VWAP Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Der CCI VWAP-Ansatz versucht, Intraday-Umkehrungen zu erfassen, wenn Momentum und Preis vom volumengewichteten Durchschnittspreis abweichen. Durch die Beobachtung des Commodity Channel Index neben dem VWAP-Level misst das System die Stärke der jüngsten Bewegungen im Verhältnis zu einem fairen Wert.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 70%. Sie funktioniert am besten auf dem Aktienmarkt.

Ein Kaufsignal entsteht, wenn der CCI unter -100 fällt und der Markt unter dem VWAP handelt, was darauf hinweist, dass der Verkaufsdruck erschöpft sein könnte. Ein Short entsteht, wenn der CCI über +100 steigt und der Preis über dem VWAP liegt, was eine überdehnte Rally hervorhebt, die anfällig für einen Rücksetzer ist. Positionen werden geschlossen, sobald der Preis den VWAP in die entgegengesetzte Richtung zurückerobert.

Diese Strategie eignet sich für Daytrader, die extreme Positionen handeln möchten, aber dennoch auf objektive Levels für Ausstiege vertrauen. Der definierte Stop-Loss hilft, das Risiko zu managen, wenn das Momentum nicht schnell zur Mitte revertiert.

## Details
- **Einstiegskriterien**:
  - **Long**: CCI < -100 && Price < VWAP (oversold below VWAP)
  - **Short**: CCI > 100 && Price > VWAP (overbought above VWAP)
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Long schließen, wenn der Preis über den VWAP steigt
  - **Short**: Short schließen, wenn der Preis unter den VWAP fällt
- **Stops**: Ja.
- **Standardwerte**:
  - `CciPeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Gemischt
  - Richtung: Beide
  - Indikatoren: CCI VWAP
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

