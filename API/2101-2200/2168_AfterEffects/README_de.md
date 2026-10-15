# AfterEffects-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die AfterEffects-Strategie basiert auf der Idee, dass Marktpreise Nachwirkungen zeigen können.
Sie berechnet ein Signal aus dem aktuellen Schlusskurs und den Eröffnungskursen von `p` und `2p` Bars zuvor:

`signal = Close - 2 * Open[p] + Open[2p]`

Ein positives Signal öffnet eine Long-Position, ein negatives Signal öffnet eine Short-Position.
Wenn `Random` auf wahr gesetzt wird, wird das Signal umgekehrt.

Nach dem Einstieg setzt die Strategie einen Stop-Loss `StopLoss` Punkte vom Einstieg entfernt.
Wenn sich der Preis `2 * StopLoss` Punkte in die günstige Richtung bewegt:

- ändert das Signal das Vorzeichen, wird die Position mit doppeltem Volumen umgekehrt;
- andernfalls wird der Stop-Loss auf das neue Niveau nachgezogen.

## Details

- **Einstiegskriterien**: `signal > 0` für Long, `signal < 0` für Short.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Stop-Loss.
- **Stops**: Trailing.
- **Standardwerte**:
  - `StopLoss` = 500
  - `Period` = 3
  - `Random` = false
  - `Volume` = 1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Benutzerdefinierte Formel
  - Stops: Trailing
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
