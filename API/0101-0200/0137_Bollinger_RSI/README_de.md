# Bollinger RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Bollinger RSI kombiniert die Überdehnung der Bollinger Bänder mit RSI-Momentum-Signalen.
Wenn der Kurs außerhalb der Bänder schließt, aber der RSI eine Divergenz zeigt, ist oft eine Umkehr nahe.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 148%. Die Strategie funktioniert am besten auf dem Forex-Markt.

Das System geht bei dieser Divergenz gegen den Trend vor und steigt aus, sobald der Kurs wieder in die Bänder eintritt oder der RSI zurückkreuzt.

Ein enger prozentualer Stop begrenzt das Risiko, falls die Volatilität weiter zunimmt.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, RSI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

