# Stoch Komposter Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein Port des MQL5-Experten **Exp_iStochKomposter**. Sie verwendet den Stochastischen Oszillator, um Momentum-Umkehrungen zu erkennen, und handelt, wenn die %K-Linie vordefinierte Schwellen kreuzt.

## Funktionsweise

- Berechnet den Stochastischen Oszillator auf dem gewählten Zeitrahmen.
- Generiert ein **Kauf**-Signal, wenn %K von unten über das untere Niveau (Standard 30) kreuzt.
- Generiert ein **Verkauf**-Signal, wenn %K von oben unter das obere Niveau (Standard 70) kreuzt.
- Bei jedem Signal schließt die Strategie jede entgegengesetzte Position und eröffnet eine neue Position in Signalrichtung mit Marktorders.
- Optionale Stop-Loss- und Take-Profit-Niveaus werden über `StartProtection` angewendet.

## Parameter

| Name | Beschreibung | Standard |
|------|--------------|----------|
| `KPeriod` | Berechnungsperiode der %K-Linie | 5 |
| `DPeriod` | Glättungsperiode der %D-Linie | 3 |
| `UpLevel` | Überkauft-Schwelle für Verkaufssignale | 70 |
| `DownLevel` | Überverkauft-Schwelle für Kaufsignale | 30 |
| `StopLoss` | Absoluter Stop-Loss in Preiseinheiten | 1000 |
| `TakeProfit` | Absoluter Take-Profit in Preiseinheiten | 2000 |
| `CandleType` | Zeitrahmen für Berechnungen | 1 Stunde |

## Hinweise

- Die Strategie arbeitet nur auf abgeschlossenen Kerzen.
- ATR-Niveaus aus dem Originalindikator werden nicht berechnet; sie dienten nur der Pfeilplatzierung in der MQL-Version.
- Die Positionsgröße wird durch die `Volume`-Eigenschaft der Strategie definiert.
