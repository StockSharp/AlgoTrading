# Stalin-Indikator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert die Logik des „Stalin"-Indikators aus MQL5.
Sie verwendet ein Paar exponentieller gleitender Durchschnitte (EMAs) und einen optionalen RSI-Filter.
Ein Long-Signal erscheint, wenn die schnelle EMA die langsame EMA von unten nach oben kreuzt und der RSI über 50 liegt.
Ein Short-Signal erscheint, wenn die schnelle EMA die langsame EMA von oben nach unten kreuzt und der RSI unter 50 liegt.

Signale können durch eine erforderliche Preisbewegung bestätigt und durch den Abstand zum letzten Signal gefiltert werden.
Positionen werden mit Marktaufträgen eröffnet und bei entgegengesetzten Signalen umgekehrt.

## Details

- **Einstiegskriterien**:
  - **Long**: `FastEMA(t-1) < SlowEMA(t-1)` && `FastEMA(t) > SlowEMA(t)` && `RSI(t) > 50`.
  - **Short**: `FastEMA(t-1) > SlowEMA(t-1)` && `FastEMA(t) < SlowEMA(t)` && `RSI(t) < 50`.
- **Bestätigen**: Der Handel wird verzögert, bis sich der Preis um `Confirm` Punkte vom Ausbruchsniveau bewegt.
- **Flat-Filter**: Neue Signale werden ignoriert, wenn sie näher als `Flat` Punkte am Preis des vorherigen Signals liegen.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `FastLength` = 14.
  - `SlowLength` = 21.
  - `RsiLength` = 17.
  - `Confirm` = 0 Punkte (deaktiviert).
  - `Flat` = 0 Punkte (deaktiviert).
  - `CandleType` = 1-Stunden-Kerzen.
- **Filter**:
  - Kategorie: Trendfolge.
  - Richtung: Beide.
  - Indikatoren: Mehrere.
  - Stops: Nein.
  - Komplexität: Moderat.
  - Zeitrahmen: Mittelfristig.
  - Saisonalität: Nein.
  - Neuronale Netze: Nein.
  - Divergenz: Nein.
  - Risikolevel: Mittel.
