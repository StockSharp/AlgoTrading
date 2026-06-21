# Color Zerolag JJRSX-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert die Logik des **ColorZerolagJJRSX** MetaTrader-Experten. Sie verwendet zwei geglättete RSI-Oszillatoren, um den originalen ColorZerolagJJRSX-Indikator anzunähern. Kreuzungen der schnellen und langsamen Linie erzeugen Handelssignale.

## Funktionsweise

- Wenn der schnelle Oszillator den langsamen Oszillator **von oben nach unten** kreuzt, schließt die Strategie alle Short-Positionen und öffnet optional eine neue Long-Position.
- Wenn der schnelle Oszillator den langsamen Oszillator **von unten nach oben** kreuzt, schließt die Strategie alle Long-Positionen und öffnet optional eine neue Short-Position.
- Schutzende Stop-Loss- und Take-Profit-Levels werden über den integrierten `StartProtection`-Mechanismus angewendet.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `FastPeriod` | Periode der schnellen JJRSX-Linie. |
| `SlowPeriod` | Periode der langsamen JJRSX-Linie. |
| `BuyOpen` | Öffnen von Long-Positionen erlauben. |
| `SellOpen` | Öffnen von Short-Positionen erlauben. |
| `BuyClose` | Bestehende Long-Positionen bei entgegengesetztem Signal schließen. |
| `SellClose` | Bestehende Short-Positionen bei entgegengesetztem Signal schließen. |
| `StopLoss` | Stop-Loss-Level in Preiseinheiten. |
| `TakeProfit` | Take-Profit-Level in Preiseinheiten. |
| `CandleType` | Zeitrahmen für Berechnungen. |

## Hinweise

- Die Implementierung verwendet integrierte Indikatoren und die High-Level `Bind`-API.
- Das Volumen wird aus der `Volume`-Eigenschaft der Strategie entnommen.
- Für diese Strategie ist keine Python-Version vorgesehen.

## Referenzen

Der originale MQL-Quellcode befindet sich in `MQL/13854` in diesem Repository.
