# Exp-MA-Rundungskanal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie rundet einen gleitenden Durchschnitt auf einen festen Tick-Schritt und baut einen ATR-basierten Kanal darum herum auf. Wenn die vorherige Kerze oberhalb des oberen Bands schließt, eröffnet die Strategie eine Long-Position. Wenn die vorherige Kerze unterhalb des unteren Bands schließt, wird eine Short-Position eröffnet. Entgegengesetzte Signale schließen bestehende Positionen. Stop-Loss und Take-Profit werden in Ticks definiert und automatisch verwaltet.

## Details

- **Einstiegskriterien**:
  - **Long**: Vorheriger Schlusskurs liegt über dem oberen gerundeten Band.
  - **Short**: Vorheriger Schlusskurs liegt unter dem unteren gerundeten Band.
- **Ausstiegskriterien**:
  - **Long**: Vorheriger Schlusskurs liegt unter dem unteren Band.
  - **Short**: Vorheriger Schlusskurs liegt über dem oberen Band.
- **Indikatoren**:
  - Exponentieller gleitender Durchschnitt.
  - Average True Range für die Kanalbreite.
- **Stops**: Ja, fester Stop-Loss und Take-Profit in Ticks.
- **Standardwerte**:
  - `MA period` = 12.
  - `ATR period` = 12.
  - `ATR factor` = 1.
  - `MA round` = 500 Ticks.
  - `Stop loss` = 1000 Ticks.
  - `Take profit` = 2000 Ticks.
  - `Timeframe` = 4 Stunden.

## Filter

- Kategorie: Trendfolge
- Richtung: Beide
- Indikatoren: Mehrere
- Stops: Ja
- Komplexität: Moderat
- Zeitrahmen: Mittelfristig
- Saisonalität: Nein
- Neuronale Netze: Nein
- Divergenz: Nein
- Risikolevel: Moderat
