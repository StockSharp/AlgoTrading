# Stochastic Automatisierte Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt mit dem **Stochastic-Oszillator** auf dem ausgewählten Kerzen-Zeitrahmen. Sie wartet darauf, dass %K und %D in extreme Zonen eintreten, und agiert dann bei Kreuzungen zum Öffnen von Positionen. Fester Take-Profit und Stop-Loss schützen jeden Trade, während ein Trailing-Stop Gewinne sichert.

## Logik

1. **Einstieg**
   - **Long:**
     - Sowohl %K als auch %D lagen vor zwei Kerzen unter `OverSold`.
     - %D lag vor zwei Kerzen über %K und vor einer Kerze unter %K.
     - %D steigt.
   - **Short:**
     - Sowohl %K als auch %D lagen vor zwei Kerzen über `OverBought`.
     - %D lag vor zwei Kerzen unter %K und vor einer Kerze über %K.
     - %D fällt.
2. **Ausstieg**
   - Die Position wird geschlossen, wenn Stochastic die extreme Zone verlässt oder %D in die entgegengesetzte Richtung dreht.
   - Ein Trailing-Stop wird ausgelöst, wenn der Kurs um `TrailingStop` zurückläuft.
   - Globale `TakeProfit`- und `StopLoss`-Level werden auf jeden Trade angewendet.

## Parameter

| Name | Beschreibung |
|------|--------------|
| `CandleType` | Zeitrahmen für Stochastic-Berechnungen. |
| `KPeriod` | Lookback-Periode für die %K-Linie. |
| `DPeriod` | Glättungsperiode für die %D-Linie. |
| `Slowing` | Zusätzliche Glättung für %K. |
| `OverBought` | Oberer Schwellenwert für überkauften Markt. |
| `OverSold` | Unterer Schwellenwert für überverkauften Markt. |
| `TakeProfit` | Abstand vom Einstieg zum Gewinnziel (Preiseinheiten). |
| `StopLoss` | Abstand vom Einstieg zum Schutz-Stop (Preiseinheiten). |
| `TrailingStop` | Trailing-Abstand, sobald der Trade im Gewinn liegt (Preiseinheiten). |

## Indikatoren

- `StochasticOscillator`

## Hinweise

- Kommentare im Code sind auf Englisch.
- Die Python-Version wird absichtlich weggelassen.
