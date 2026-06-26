# Pull-Back-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Pull-Back-Strategie reproduziert die Logik des ursprünglichen MetaTrader "PULL BACK" Expert Advisors unter Verwendung der StockSharp High-Level-APIs. Der Ansatz sucht nach Pullbacks zu einem schnellen gewichteten gleitenden Durchschnitt auf einem höheren Zeitrahmen, bestätigt die Momentum-Stärke über mehrere Balken und handelt in Richtung des monatlichen MACD-Trends. Sobald eine Position eröffnet ist, wendet der Algorithmus Geldmanagement-Regeln an, die Stop-Loss, Take-Profit, Break-Even und Trailing-Stop-Handling umfassen.

## Daten und Indikatoren

- **Handelszeitrahmen:** benutzerwählbarer Kerzentyp (`CandleType`, Standard: 15-Minuten-Kerzen).
- **Bestätigungszeitrahmen:** höheres Zeitrahmen-Abonnement (`HigherCandleType`, Standard: 1-Stunden-Kerzen) verwendet für:
  - Schnelle/langsame gewichtete gleitende Durchschnitte.
  - Momentum-Indikator mit absolutem Abstand vom neutralen Wert (100).
  - Pull-Back-Erkennung, wenn die vorherige Kerze die schnelle WMA berührt.
- **MACD-Zeitrahmen:** separates Abonnement (`MacdCandleType`, Standard: 30-Tage-Kerzen) zum Lesen der MACD-Signallinienrichtung.
- **Indikatoren:**
  - Gewichteter Gleitender Durchschnitt (WMA) auf Handels- und höherem Zeitrahmen.
  - Momentum (konfigurierbarer Zeitraum) auf dem höheren Zeitrahmen.
  - Moving Average Convergence Divergence (MACD) auf dem langen Zeitrahmen.

## Handelslogik

### Long-Setup

1. Die schnelle WMA des höheren Zeitrahmens liegt über der langsamen WMA.
2. Die zuletzt abgeschlossene Kerze des höheren Zeitrahmens öffnete über der schnellen WMA und berührte sie mit ihrem Tief (Pull-Back-Bestätigung).
3. Mindestens eine der letzten drei absoluten Momentum-Messungen überschreitet `MomentumBuyThreshold`.
4. Die MACD-Hauptlinie liegt über ihrer Signallinie im MACD-Zeitrahmen.
5. Auf dem Handelszeitrahmen liegt die schnelle WMA über der langsamen WMA.

Wenn alle Regeln erfüllt sind, sendet die Strategie eine Market-Kauforder. Der Einstiegspreis wird aufgezeichnet, um Risikoparameter zu steuern.

### Short-Setup

1. Die schnelle WMA des höheren Zeitrahmens liegt unter der langsamen WMA.
2. Die neuere Kerze öffnete unter der schnellen WMA und berührte sie mit ihrem Hoch.
3. Einer der letzten drei Momentum-Werte überschreitet `MomentumSellThreshold`.
4. Die MACD-Hauptlinie liegt unter der Signallinie.
5. Die schnelle WMA des Handelszeitframens liegt unter der langsamen WMA.

Eine Market-Verkaufsorder wird gesendet, wenn die Bedingungen übereinstimmen.

## Positionsmanagement

- **Stop-Loss:** `StopLossTicks`-Abstand vom Einstieg (in absoluten Preis mit dem Kursschritt des Wertpapiers umgerechnet).
- **Take-Profit:** `TakeProfitTicks`-Abstand vom Einstieg.
- **Break-Even:** Wenn der Kurs um `BreakEvenTriggerTicks` fortschreitet, wird der Stop auf Einstieg plus `BreakEvenOffsetTicks` in Handelsrichtung verschoben, wenn `UseBreakEven` aktiviert ist.
- **Trailing-Stop:** Wenn `UseTrailingStop` true ist, folgt der Stop dem Kurs um `TrailingStopTicks`, sobald sich die Position im Gewinn befindet.
- **Ausstiegsprüfungen:** laufen bei jeder abgeschlossenen Handelszeitrahmkerze. Wenn Stop oder Ziel erreicht wird, schließt die Strategie die gesamte Position mit einer Market-Order.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `FastMaLength` | Schnelle WMA-Länge im Handelszeitrahmen (Standard: 6). |
| `SlowMaLength` | Langsame WMA-Länge im Handelszeitrahmen (Standard: 85). |
| `BounceSlowLength` | Langsame WMA-Länge im Bestätigungszeitrahmen (Standard: 200). |
| `MomentumLength` | Momentum-Lookback im höheren Zeitrahmen (Standard: 14). |
| `MomentumBuyThreshold` | Minimum |Momentum-100| für Long-Einstiege (Standard: 0.3). |
| `MomentumSellThreshold` | Minimum |Momentum-100| für Short-Einstiege (Standard: 0.3). |
| `StopLossTicks` | Stop-Loss-Abstand in Ticks (Standard: 200). |
| `TakeProfitTicks` | Take-Profit-Abstand in Ticks (Standard: 500). |
| `UseTrailingStop` | Trailing-Stop-Logik aktivieren (Standard: true). |
| `TrailingStopTicks` | Trailing-Stop-Abstand in Ticks (Standard: 400). |
| `UseBreakEven` | Break-Even-Anpassung aktivieren (Standard: true). |
| `BreakEvenTriggerTicks` | Gewinn-Trigger für Break-Even in Ticks (Standard: 300). |
| `BreakEvenOffsetTicks` | Offset zum Break-Even-Stop in Ticks (Standard: 300). |
| `MacdFastLength` | Schnelle EMA-Periode des MACD (Standard: 12). |
| `MacdSlowLength` | Langsame EMA-Periode des MACD (Standard: 26). |
| `MacdSignalLength` | Signal-EMA-Periode des MACD (Standard: 9). |
| `CandleType` | Kerzentyp des Handelszeitrahmens. |
| `HigherCandleType` | Kerzentyp des Bestätigungszeitrahmens. |
| `MacdCandleType` | Kerzentyp des MACD-Zeitrahmens. |

## Hinweise

- Die Strategie erwartet, dass `Security.PriceStep` befüllt ist, damit tick-basierte Risikokontrollen korrekt in Preisabstände übersetzt werden.
- Es wird immer nur eine Nettoposition gehalten; entgegengesetzte Signale werden ignoriert, bis die aktuelle Position geschlossen ist.
- Die Logik verarbeitet nur abgeschlossene Kerzen, um nicht auf Teildaten zu reagieren.
