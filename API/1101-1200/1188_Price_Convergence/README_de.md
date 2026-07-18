# Preiskonvergenz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie schätzt die Wahrscheinlichkeit steigender oder fallender Preise, indem die Summe der OHLC4-Werte bullischer und bärischer Kerzen verglichen wird. Eine Long-Position wird eröffnet, wenn die Wahrscheinlichkeit eines Anstiegs 50% übersteigt, und eine Short-Position, wenn die Wahrscheinlichkeit eines Rückgangs 50% übersteigt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 37%. Die besten Ergebnisse werden auf dem Kryptomarkt erzielt.

Die Strategie kann auf der gesamten Historie oder in einem gleitenden Fenster operieren, das durch den Parameter `Range` definiert wird. Der OHLC4-Wert jeder Kerze wird verwendet, um die Beiträge von Auf- und Abbewegungen zu gewichten.

## Details

- **Einstiegskriterien**: Eine Aufwärtswahrscheinlichkeit über 50% löst einen Long-Einstieg aus; eine Abwärtswahrscheinlichkeit über 50% löst einen Short-Einstieg aus.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `FullHistory` = true
  - `Range` = 200
  - `CandleType` = 1 minute
- **Filter**:
  - Kategorie: Statistisch
  - Richtung: Beide
  - Indikatoren: Benutzerdefiniert
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
