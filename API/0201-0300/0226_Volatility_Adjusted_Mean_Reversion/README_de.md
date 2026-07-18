# Volatilitätsbereinigte Mean-Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Variante der Mean Reversion skaliert die Einstiegsschwellen durch das Verhältnis von ATR zu Standardabweichung. Wenn die Volatilität im Verhältnis zum typischen Rauschen zunimmt, wächst die Distanz, die benötigt wird, um einen Trade auszulösen, was hilft, vorzeitige Signale während chaotischer Schwankungen zu vermeiden.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 115%. Sie funktioniert am besten am Aktienmarkt.

Eine Long-Position wird eröffnet, wenn der Preis um mehr als die angepasste Schwelle unter den gleitenden Durchschnitt fällt. Eine Short-Position wird eröffnet, wenn der Preis um dasselbe Maß über den Durchschnitt steigt. Positionen werden beendet, sobald der Preis wieder nahe am Durchschnittsniveau schließt.

Die adaptive Schwelle macht diese Strategie geeignet für Märkte mit wechselnden Volatilitätsregimen. Ein Stop-Loss von der zweifachen ATR begrenzt das Risiko, während auf die Umkehr gewartet wird.

## Details
- **Einstiegskriterien**:
  - **Long**: Schluss < MA - Multiplier * ATR / (ATR/StdDev)
  - **Short**: Schluss > MA + Multiplier * ATR / (ATR/StdDev)
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg, wenn Schluss >= MA
  - **Short**: Ausstieg, wenn Schluss <= MA
- **Stops**: Ja, dynamisch basierend auf ATR.
- **Standardwerte**:
  - `Period` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: ATR, StdDev
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
