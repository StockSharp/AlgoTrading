# Low Volatility Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Mean-Reversion-Strategie wird nur in ruhigen Märkten aktiviert. Sie misst den ATR über ein Rückblickfenster und tritt ein, wenn die Volatilität unter einen Prozentsatz dieses Durchschnitts fällt und der Preis von seinem gleitenden Durchschnitt abweicht.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 139%. Es funktioniert am besten auf dem Aktienmarkt.

Durch den Handel gegen kleine Bewegungen in ruhigen Bedingungen zielt es darauf ab, Rückpraller zu erfassen, ohne großen Trends nachzujagen.

Positionen schließen, sobald der Preis den gleitenden Durchschnitt berührt oder der ATR-basierte Stop-Loss erreicht wird.

## Details

- **Einstiegskriterien**: Preis entfernt vom gleitenden Durchschnitt, während ATR unter dem Schwellenwert liegt.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Preis kehrt zur MA zurück oder Stop wird ausgelöst.
- **Stops**: Ja.
- **Standardwerte**:
  - `MAPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrLookbackPeriod` = 20
  - `AtrThresholdPercent` = 50m
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: ATR, MA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

