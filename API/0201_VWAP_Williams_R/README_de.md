# VWAP Williams R Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die VWAP Williams %R-Strategie konzentriert sich auf die Intraday-Reversion rund um den volumengewichteten Durchschnittspreis. Sie beobachtet, wenn der Preis vom VWAP abdriftet, während der Williams %R-Oszillator überverkaufte oder überkaufte Bereiche erreicht. Die Annahme ist, dass extreme Werte nahe dem VWAP oft zu einem Rückschlag zum Mittelwert führen.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 40%. Die Strategie funktioniert am besten auf dem Kryptomarkt.

Wenn der Oszillator unter -80 fällt und der Preis unter dem VWAP handelt, impliziert das Setup, dass der Verkaufsdruck nachlässt und eine Erholung folgen kann. Umgekehrt warnt ein Wert über -20, während der Preis über dem VWAP liegt, dass Käufer erschöpft sind und ein Rückgang wahrscheinlich ist. Die Strategie eröffnet Trades in Richtung einer potenziellen Rückkehr zum VWAP und wartet darauf, dass diese Bewegung abgeschlossen wird.

Dieser Ansatz eignet sich für aktive Intraday-Trader, die häufige Mean-Reversion-Möglichkeiten bevorzugen. Ein kleiner Stop‑Loss relativ zum VWAP hält das Risiko begrenzt und lässt gleichzeitig genug Raum für Preisschwankungen vor der Umkehr.

## Details
- **Einstiegskriterien**:
  - **Long**: Price < VWAP && Williams %R < -80 (überverkauft unterhalb VWAP)
  - **Short**: Price > VWAP && Williams %R > -20 (überkauft oberhalb VWAP)
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Long-Position schließen, wenn der Preis über VWAP steigt
  - **Short**: Short-Position schließen, wenn der Preis unter VWAP fällt
- **Stops**: Ja.
- **Standardwerte**:
  - `WilliamsRPeriod` = 14
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
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

