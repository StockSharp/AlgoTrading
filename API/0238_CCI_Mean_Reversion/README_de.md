# CCI Mean Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Der Commodity Channel Index (CCI) misst, wie weit sich der Preis von seinem statistischen Durchschnitt entfernt. Diese Strategie tritt ein, wenn CCI stark von seinem eigenen Mittelwert abweicht, und erwartet einen schnellen Rückprall, sobald der Schwung nachlässt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 151%. Er funktioniert am besten auf dem Aktienmarkt.

Ein Long-Trade erfolgt, wenn CCI unter den Durchschnitt minus `DeviationMultiplier` mal die Standardabweichung fällt. Ein Short-Trade wird eröffnet, wenn CCI über den Durchschnitt plus diesen Multiplikator steigt. Die Position wird beendet, wenn CCI wieder durch den Mittelwert kreuzt.

Dieses System eignet sich für kurzfristige Trader, die konträre Setups bevorzugen. Ein auf prozentualer Bewegung basierender Stop-Loss hilft, das Risiko zu begrenzen, wenn der Markt keine schnelle Umkehr zeigt.

## Details
- **Einstiegskriterien**:
  - **Long**: CCI < Avg - DeviationMultiplier * StdDev
  - **Short**: CCI > Avg + DeviationMultiplier * StdDev
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg wenn CCI > Avg
  - **Short**: Ausstieg wenn CCI < Avg
- **Stops**: Ja, prozentualer Stop-Loss.
- **Standardwerte**:
  - `CciPeriod` = 20
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: CCI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

