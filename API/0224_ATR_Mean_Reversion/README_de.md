# ATR-Mean-Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die ATR-Mean-Reversion-Strategie misst, wie weit sich der Preis von einem gleitenden Durchschnitt relativ zur jüngsten Volatilität entfernt. Der ATR bietet eine adaptive Messgröße, sodass sich die Schwellen in aktiven Perioden ausdehnen und sich zusammenziehen, wenn die Märkte ruhig werden.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 109%. Sie funktioniert am besten auf dem Kryptomarkt.

Eine Long-Konfiguration entsteht, wenn der Preis um mehr als `Multiplier` mal den ATR unter dem gleitenden Durchschnitt schließt. Eine Short-Konfiguration erscheint, wenn der Preis um denselben Abstand über dem gleitenden Durchschnitt schließt. Positionen werden beendet, sobald der Preis zum gleitenden Durchschnitt zurückkehrt.

Diese Technik ist für kurzfristige Trader gedacht, die erwarten, dass die Preise nach übermäßigen Bewegungen umkehren. Der ATR-basierte Stop hält das Risiko proportional zu den aktuellen Marktbedingungen.

## Details
- **Einstiegskriterien**:
  - **Long**: Schluss < MA - Multiplier * ATR
  - **Short**: Schluss > MA + Multiplier * ATR
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg, wenn Schluss >= MA
  - **Short**: Ausstieg, wenn Schluss <= MA
- **Stops**: Ja, Stop-Loss standardmäßig bei etwa `2*ATR`.
- **Standardwerte**:
  - `MaPeriod` = 20
  - `AtrPeriod` = 14
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: MA, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
