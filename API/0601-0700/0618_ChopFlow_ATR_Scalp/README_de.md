# Strategie ChopFlow ATR Scalp
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

ChopFlow ATR Scalp steigt ein, wenn der Markt choppy Bedingungen verlässt und der OBV seine EMA kreuzt. Ausstiege verwenden symmetrische ATR-basierte Stops und Ziele.

Das Ziel ist es, schnelle Bewegungen während der frühen Trendentstehung zu erfassen.

## Details

- **Einstiegskriterien**: `Choppiness < ChopThreshold` und OBV über/unter seiner EMA.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: ATR-Stop oder Take-Profit-Abstand.
- **Stops**: Ja.
- **Standardwerte**:
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `ChopLength` = 14
  - `ChopThreshold` = 60
  - `ObvEmaLength` = 10
  - `SessionInput` = "1700-1600"
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Scalping
  - Richtung: Beide
  - Indikatoren: ATR, Choppiness, OBV
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
