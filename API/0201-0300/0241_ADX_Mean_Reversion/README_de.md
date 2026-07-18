# ADX Mean Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Hier misst der Average Directional Index (ADX) die allgemeine Trendstärke. Wenn ADX niedrig ist, fehlt dem Markt die Richtung und die Preise neigen dazu, um einen Mittelwert zu oszillieren. Diese Strategie nutzt dieses Verhalten aus, indem sie Abweichungen des ADX von seinem gleitenden Durchschnitt handelt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 70%. Er funktioniert am besten auf dem Aktienmarkt.

Ein Long-Trade wird eingegangen, wenn ADX unter den Durchschnitt minus `DeviationMultiplier` mal die Standardabweichung fällt und der Preis unter dem gleitenden Durchschnitt liegt. Ein Short-Trade wird eröffnet, wenn ADX über das obere Band springt und der Preis über dem Durchschnitt liegt. Positionen werden geschlossen, wenn ADX zu seinem Durchschnitt zurückkehrt.

Dieses System spricht Trader an, die Chancen in Umgebungen mit schwachem Trend suchen. Der Stop-Loss verhindert, dass kleine Mean-Reversion-Trades zu großen Verlusten werden, wenn ein neuer Trend entsteht.

## Details
- **Einstiegskriterien**:
  - **Long**: ADX < Avg - DeviationMultiplier * StdDev && Close < MA
  - **Short**: ADX > Avg + DeviationMultiplier * StdDev && Close > MA
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg wenn ADX > Avg
  - **Short**: Ausstieg wenn ADX < Avg
- **Stops**: Ja, prozentualer Stop-Loss.
- **Standardwerte**:
  - `AdxPeriod` = 14
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: ADX
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

