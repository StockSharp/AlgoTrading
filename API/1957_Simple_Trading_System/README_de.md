# Einfaches Handelssystem-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert das Simple Trading System aus MetaTrader. Sie verwendet einen um mehrere Balken verschobenen gleitenden Durchschnitt und vergleicht den aktuellen Schlusskurs mit früheren Schlusskursen, um kurzfristige Trendumkehrungen zu erkennen. Ein Kaufsignal tritt auf, wenn der gleitende Durchschnitt unter seinem Wert `MaShift` Balken zuvor liegt und der Schlusskurs zwischen den Schlusskursen `MaShift` und `MaPeriod + MaShift` Balken zuvor liegt, während die Kerze bearish ist. Ein Verkaufssignal ist das Spiegelbild. Abhängig von den Parametern kann die Strategie Long- oder Short-Positionen öffnen und/oder schließen, wenn Signale auftreten. Optionale Stop-Loss- und Take-Profit-Niveaus können konfiguriert werden.

## Details

- **Einstiegskriterien:**
  - **Long**: `MA(t) <= MA(t+MaShift)` && `Close(t) >= Close(t+MaShift)` && `Close(t) <= Close(t+MaPeriod+MaShift)` && `Close(t) < Open(t)`
  - **Short**: `MA(t) >= MA(t+MaShift)` && `Close(t) <= Close(t+MaShift)` && `Close(t) >= Close(t+MaPeriod+MaShift)` && `Close(t) > Open(t)`
- **Long/Short**: Beide Seiten je nach `BuyPositionOpen` und `SellPositionOpen`.
- **Ausstiegskriterien**: Das entgegengesetzte Signal löst das Schließen aus, wenn `BuyPositionClose` oder `SellPositionClose` aktiviert ist.
- **Stops**: Optional. `StopLoss` und `TakeProfit` in absoluten Preiseinheiten über `StartProtection`.
- **Standardwerte:**
  - `MaType` = EMA
  - `MaPeriod` = 2
  - `MaShift` = 4
  - `PriceType` = Close
  - `CandleType` = 6-Stunden-Kerzen
  - `TakeProfit` = 2000
  - `StopLoss` = 1000
  - `Volume` = 1
- **Filter:**
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Gleitender Durchschnitt
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
