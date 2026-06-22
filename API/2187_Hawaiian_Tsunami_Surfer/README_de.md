# Hawaiian Tsunami Surfer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie sucht nach plötzlichen Momentum-Spitzen und handelt gegen sie. Sie berechnet die prozentuale Änderung des Schlusskurses über eine Kerze mithilfe eines Momentum-Indikators. Wenn die prozentuale Änderung einen kleinen Schwellenwert überschreitet, gilt die Bewegung als "Tsunami". Die Strategie verkauft nach einem starken Aufwärtsschub und kauft nach einem starken Abwärtsschub. Schutz-Stop-Loss und Take-Profit werden über StartProtection in Preisschritten angewendet.

## Details

- **Einstiegskriterien**:
  - Verkaufen, wenn Momentum-Prozentsatz > `TsunamiStrength`.
  - Kaufen, wenn Momentum-Prozentsatz < `-TsunamiStrength`.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Schutz-Stop-Loss oder Take-Profit.
- **Stops**: Ja, über StartProtection.
- **Standardwerte**:
  - `MomentumPeriod` = 1
  - `TsunamiStrength` = 0.24
  - `TakeProfitPoints` = 500
  - `StopLossPoints` = 700
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Momentum
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
