# Strategie VWAP Williams R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die VWAP Williams %R-Strategie konzentriert sich auf die Intraday-Reversion rund um den volumengewichteten Durchschnittspreis. Sie beobachtet, wenn der Preis vom VWAP abdriftet, während der Williams %R-Oszillator überverkaufte oder überkaufte Bereiche erreicht. Die Annahme ist, dass extreme Werte nahe dem VWAP oft zu einem Rückschlag zum Mittelwert führen.

Wenn der Oszillator unter -80 fällt und der Preis unter dem VWAP handelt, impliziert das Setup, dass der Verkaufsdruck nachlässt und eine Erholung folgen kann. Umgekehrt warnt ein Wert über -20, während der Preis über dem VWAP liegt, dass Käufer erschöpft sind und ein Rückgang wahrscheinlich ist. Die Strategie eröffnet Trades in Richtung einer potenziellen Rückkehr zum VWAP und wartet darauf, dass diese Bewegung abgeschlossen wird.

Dieser Ansatz sucht nach Intraday-Mean-Reversion. Ein prozentualer Stop-Loss ab jedem Ausführungspreis begrenzt ungünstige Bewegungen, während der Ausstieg am VWAP die erwartete Rückkehr zum Mittelwert abschließt.

## Details
- **Einstiegskriterien**:
  - **Long**: Der Schlusskurs liegt mindestens 0,1% unter dem Tages-VWAP und Williams %R kreuzt -80 nach unten.
  - **Short**: Der Schlusskurs liegt mindestens 0,1% über dem Tages-VWAP und Williams %R kreuzt -20 nach oben.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Long-Position schließen, wenn der Preis über VWAP steigt
  - **Short**: Short-Position schließen, wenn der Preis unter VWAP fällt
- **Stops**: Ja.
- **Standardwerte**:
  - `WilliamsRPeriod` = 14
  - `CooldownBars` = 60
  - `StopLossPercent` = 2%
  - `CandleType` = TimeSpan.FromMinutes(30)
- **Filter**:
  - Kategorie: Gemischt
  - Richtung: Beide
  - Indikatoren: VWAP Williams R
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

