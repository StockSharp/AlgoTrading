# Kurzfristiger Long-Trade-Finder für Bullische Divergenz
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie sucht nach bullischen Divergenzen zwischen Preis und RSI. Wenn der Preis ein tieferes Tief markiert, aber der RSI ein höheres Tief innerhalb eines festgelegten Pivot-Bereichs bildet und der stündliche RSI unter 40 liegt, geht die Strategie eine Long-Position ein. Die Position wird geschlossen, wenn der RSI über einen Schwellenwert steigt, eine bärische Divergenz erscheint oder der Stop-Loss ausgelöst wird.

- **Einstiegsbedingungen**:
  - Aktuelles Tief liegt unter dem Preis des vorherigen Pivot-Tiefs.
  - RSI bildet ein höheres Tief unter `RsiBullConditionMin` und der vorherige Pivot tritt innerhalb von 5–50 Bars auf.
  - Stündlicher RSI liegt unter `RsiHourEntryThreshold`.
  - Schlusskurs liegt unter dem Preis des vorherigen Pivot-Tiefs.
- **Ausstiegsbedingungen**:
  - RSI kreuzt `SellWhenRsi` nach oben.
  - Bärische Divergenz: Preis markiert ein höheres Hoch, während RSI ein niedrigeres Hoch markiert.
  - Stop-Loss über `StartProtection` bei `StopLossPercent` aktiviert.
- **Indikatoren**: RSI.
