# Color JSatl Digit-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie konvertiert den MQL5-Experten "Exp_ColorJSatl_Digit" nach StockSharp. Sie digitalisiert die Steigung des Jurik Moving Average (JMA), um jede Kerze als aufwärts oder abwärts zu klassifizieren. Ein Zustandswechsel von 0 auf 1 markiert einen entstehenden Aufwärtstrend, während ein Wechsel von 1 auf 0 einen Abwärtstrend signalisiert.

Der Algorithmus abonniert Kerzen eines gewählten Zeitrahmens und bindet einen JMA-Indikator. Wenn der JMA nach oben dreht, öffnet die Strategie eine Long-Position und schließt jede Short-Position. Wenn der JMA nach unten dreht, öffnet sie eine Short-Position und schließt jede Long-Position. Der optionale Parameter `DirectMode` invertiert die Signale für den Contra-Trend-Handel.

Positionen werden durch prozentbasierte Stop-Loss- und Take-Profit-Level geschützt. Alle Parameter werden über `StrategyParam` definiert und können optimiert werden.

## Details

- **Einstiegskriterien**
  - **Long**: JMA dreht nach oben (`prev > prevPrev` && `current >= prev`) und `DirectMode` ist wahr. Im Umkehrmodus öffnet eine Abwärtsdrehung die Long-Position.
  - **Short**: JMA dreht nach unten (`prev < prevPrev` && `current <= prev`) und `DirectMode` ist wahr. Im Umkehrmodus öffnet eine Aufwärtsdrehung die Short-Position.
- **Ausstiegskriterien**: Das entgegengesetzte Signal löst sofort eine Marktorder in die andere Richtung aus. Schutzorders können Positionen ebenfalls schließen.
- **Stops**: Prozentualer Stop-Loss und Take-Profit über `StartProtection`.
- **Standardwerte**
  - `JMA Length` = 30
  - `Candle Type` = 4-Stunden-Kerzen
  - `Stop Loss %` = 1
  - `Take Profit %` = 2
  - `Direct Mode` = true
- **Filter**
  - Kategorie: Trendfolge
  - Richtung: Beide (umkehrbar)
  - Indikatoren: Jurik Moving Average
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Moderat
