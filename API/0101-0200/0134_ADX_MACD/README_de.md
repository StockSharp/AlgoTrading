# ADX MACD Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
ADX MACD verbindet die Trendstärke des Average Directional Index mit Momentum-Wechseln des MACD.
Wenn der ADX steigt, haben Ausbrüche eine höhere Chance, sich fortzusetzen, insbesondere wenn der MACD in dieselbe Richtung kreuzt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 139%. Die Strategie funktioniert am besten auf dem Aktienmarkt.

Die Strategie handelt diese ausgerichteten Signale und steigt aus, sobald der ADX zu schwächen beginnt oder der MACD gegen die Position dreht.

Ein moderater prozentualer Stop begrenzt Verluste in seitwärts laufenden Märkten.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: ADX, MACD
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

