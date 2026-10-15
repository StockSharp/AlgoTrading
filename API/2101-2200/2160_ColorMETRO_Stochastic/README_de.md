# ColorMETRO Stochastic-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine C#-Portierung des MQL5-Experten **exp_colormetro_stochastic.mq5**. Sie ersetzt den ursprünglichen ColorMETRO Stochastic-Indikator durch den integrierten `StochasticOscillator` von StockSharp und handelt auf Crossover-Ereignisse.

## Logik
- Abonniert standardmäßig 8-Stunden-Kerzen (konfigurierbar).
- Berechnet den Stochastik-Oszillator mit den Parametern:
  - %K-Periode (`KPeriod`)
  - %D-Periode (`DPeriod`)
  - Zusätzliche Glättung (`Slowing`)
- Speichert vorherige %K- und %D-Werte zur Crossover-Erkennung.
- **Kaufen** wenn %K %D nach oben kreuzt.
- **Verkaufen** wenn %K %D nach unten kreuzt.
- Wendet über `StartProtection` einen einfachen 2% Stop-Loss und Take-Profit an.

## Parameter
| Name | Beschreibung |
|------|--------------|
| `KPeriod` | Rückblick für %K-Linie (Standard 5). |
| `DPeriod` | Glättungsperiode für %D-Linie (Standard 3). |
| `Slowing` | Zusätzlicher Glättungswert (Standard 3). |
| `CandleType` | Zeitrahmen der Kerzen, Standard 8 Stunden. |

## Hinweise
Die ursprüngliche MQL-Version verwendete einen benutzerdefinierten ColorMETRO Stochastic-Indikator mit schnellen und langsamen Schrittlinien. Diese Portierung approximiert ihre Signale mit dem Standard-Stochastik-Oszillator.
