# Volatility Adjusted Moving Average
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Technik modifiziert ein gleitendes Durchschnittsband um ein ATR-Vielfaches. Wenn der Preis über das angepasste Band hinausgeht, deutet das auf einen beschleunigten Trend hin.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 160%. Es funktioniert am besten auf dem Forex-Markt.

Long-Trades werden oberhalb des oberen Bandes eröffnet, Shorts unterhalb des unteren Bandes. Ein Rückkreuzen durch den Basis-gleitenden-Durchschnitt schließt die Position.

Da sich die Bänder mit der Volatilität ausdehnen, passen sich die Stops den Marktbedingungen an.

## Details

- **Einstiegskriterien**: Preis bricht über oder unter MA ± ATR-Multiplikator.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Preis kreuzt MA oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `MAPeriod` = 20
  - `ATRPeriod` = 14
  - `ATRMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MA, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

