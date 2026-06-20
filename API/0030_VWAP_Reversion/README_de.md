# VWAP Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

VWAP Reversion-Strategie, die bei Abweichungen vom volumengewichteten Durchschnittspreis handelt

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 127%. Sie funktioniert am besten am Aktienmarkt.

VWAP Reversion handelt Abweichungen vom volumengewichteten Durchschnittspreis. Wenn der Preis zu weit über oder unter den VWAP abweicht, handelt die Strategie gegen die Bewegung und steigt beim Rückprall aus.

Da der VWAP typische Transaktionsniveaus widerspiegelt, locken extreme Abweichungen den Preis oft zurück in seine Richtung. Einige Trader kombinieren dieses Signal mit Intraday-Trendfiltern für höhere Wahrscheinlichkeiten.


## Details

- **Einstiegskriterien**: Signale basierend auf RSI, VWAP.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `DeviationPercent` = 2.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: RSI, VWAP
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

