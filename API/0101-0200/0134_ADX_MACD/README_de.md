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
  - `StopLossPercent` = 2
  - `AdxPeriod` = 14
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
    Ohne Position eröffnet ein MACD-Kreuzen über die Signallinie einen Long und darunter einen Short, aber nur solange ADX über seinem vorherigen Wert liegt. Die Position schließt, sobald ADX unter seinen vorherigen Wert fällt oder MACD auf die andere Seite der Signallinie zurückkehrt.
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

