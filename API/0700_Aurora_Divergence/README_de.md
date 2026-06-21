# Aurora-Divergenz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Divergenzen zwischen Preis und On-Balance Volume (OBV). Sie vergleicht die linearen Regressionssteigungen von Preis und OBV, um potenzielle Umkehrungen zu erkennen.

## Hauptmerkmale

- Vergleich linearer Regressionssteigungen für Divergenzsignale.
- Optionaler Z-Score-Filter zur Vermeidung überdehnter Preise.
- Gleitender-Durchschnitt-Filter auf höherem Zeitrahmen zur Trendbestätigung.
- ATR-basierter Volatilitätsschwellenwert und Risikomanagement mit dynamischem Stop und Ziel.
- Abkühlung nach jedem Handel und maximale Haltedauer in Bars.

## Parameter

| Name | Beschreibung |
|------|--------------|
| `CandleType` | Kerzen-Zeitrahmen für Hauptberechnungen. |
| `Lookback` | Periode für Steigungsberechnungen. |
| `ZLength` | Rückblickperiode für Mittelwert und Standardabweichung im Z-Score-Filter. |
| `ZThreshold` | Maximaler absoluter Z-Score für Einstiege. |
| `UseZFilter` | Z-Score-Filter aktivieren oder deaktivieren. |
| `HtfCandleType` | Höherer Zeitrahmen für Trend-Gleitenden-Durchschnitt. |
| `HtfMaLength` | Länge des gleitenden Durchschnitts im höheren Zeitrahmen. |
| `AtrLength` | ATR-Periode für Volatilität und Risiko. |
| `AtrThreshold` | Mindest-ATR-Wert für den Handel. |
| `StopAtrMultiplier` | ATR-Multiplikator für Stop-Loss-Distanz. |
| `ProfitAtrMultiplier` | ATR-Multiplikator für Take-Profit-Distanz. |
| `MaxBarsInTrade` | Maximale Bars zum Halten einer Position. |
| `CooldownBars` | Bars Wartezeit nach einem Handel. |

## Komplexität

Mittel
