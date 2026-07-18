# Trendline-Bounce-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Märkte respektieren häufig Trendlinien, die durch frühere Swing-Hochs oder -Tiefs gezogen werden. Diese Strategie passt automatisch Regressionslinien an die jüngste Kursentwicklung an und sucht nach Kerzen, die von diesen Linien in Richtung des dominanten Trends abprallen.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 124%. Die Strategie eignet sich am besten für den Devisenmarkt.

Jüngste Kerzen werden gespeichert, um aufwärts oder abwärts geneigte Unterstützungs- und Widerstandslinien zu berechnen. Wenn der Kurs eine Trendlinie berührt und eine Kerze den Abprall bestätigt, während sie auf der richtigen Seite eines gleitenden Durchschnitts bleibt, eröffnet das System einen Trade. Der Stop wird als Prozentsatz des Kurses gesetzt und ein Ausstieg erfolgt beim Kreuzen des gleitenden Durchschnitts.

Indem nur in der vorherrschenden Richtung gehandelt und auf eine klare Reaktion an Unterstützung oder Widerstand gewartet wird, versucht die Methode, Fortsetzungsbewegungen zu erfassen, ohne Ausbrüchen hinterherauszulaufen.

## Details

- **Einstiegskriterien**: Kurs berührt berechnete Trendlinie und Kerze schließt in Trendrichtung ober-/unterhalb des MA.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Kurs kreuzt den gleitenden Durchschnitt oder Stop-Loss.
- **Stops**: Ja, prozentbasiert.
- **Standardwerte**:
  - `TrendlinePeriod` = 20
  - `MAPeriod` = 20
  - `BounceThresholdPercent` = 0.5
  - `CandleType` = 5 minute
  - `StopLossPercent` = 2
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: MA, Trendlines
  - Stops: Ja
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

