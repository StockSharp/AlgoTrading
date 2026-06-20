# Volumen Mean-Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses System sucht nach ungewöhnlich hohem oder niedrigem Handelsvolumen im Verhältnis zu seinem historischen Durchschnitt. Signifikante Volumenspitzen kehren oft zurück, wenn sich die Aktivität normalisiert, und bieten potenzielle Gegentrades.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 76%. Die Strategie funktioniert am besten im Forex-Markt.

Ein Long-Einstieg erfolgt, wenn das Volumen unter den Durchschnitt minus `DeviationMultiplier` mal die Standardabweichung fällt und der Preis unter dem gleitenden Durchschnitt liegt. Ein Short-Einstieg erfolgt, wenn das Volumen über das obere Band steigt und der Preis über dem Durchschnitt liegt. Trades werden geschlossen, sobald das Volumen in Richtung seines mittleren Niveaus zurückkehrt.

Die Strategie eignet sich für Trader, die auf Erschöpfung nach Volumenspitzen achten. Ein prozentualer Stop-Loss schützt vor Szenarien, in denen das Volumen weiter in dieselbe Richtung expandiert.

## Details
- **Einstiegskriterien**:
  - **Long**: Volume < Avg - DeviationMultiplier * StdDev && Close < MA
  - **Short**: Volume > Avg + DeviationMultiplier * StdDev && Close > MA
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg wenn volume > Avg
  - **Short**: Ausstieg wenn volume < Avg
- **Stops**: Ja, prozentualer Stop-Loss.
- **Standardwerte**:
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2m
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Volume
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
