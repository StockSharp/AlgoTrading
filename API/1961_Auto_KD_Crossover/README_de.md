# Auto KD Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Auto KD Crossover-Strategie repliziert das ursprüngliche MQL5-Beispiel `autoKD_EA`.  
Sie verwendet den `StochasticOscillator`-Indikator, um Kauf- und Verkaufssignale basierend auf Kreuzungen der %K- und %D-Linien zu generieren.

Die Basisberechnung verwendet die RSV-Formel:
`RSV = (Close - LowestLow) / (HighestHigh - LowestLow) * 100`
wobei das höchste Hoch und das niedrigste Tief über `KDPeriod` Balken berechnet werden.  
Die %K-Linie ist ein gleitender Durchschnitt des RSV mit Länge `KPeriod`; %D ist ein gleitender Durchschnitt von %K mit Länge `DPeriod`.

## Parameter
| Name | Beschreibung | Standard |
|------|--------------|----------|
| `KDPeriod` | Anzahl der Balken für die RSV-Basisperiode. | 30 |
| `KPeriod` | Glättungsperiode für die %K-Linie. | 3 |
| `DPeriod` | Glättungsperiode für die %D-Linie. | 6 |
| `CandleType` | Typ und Zeitrahmen der für Berechnungen verwendeten Kerzen. | 5-Minuten-Zeitrahmen |
| `Volume` | Von `Strategy` vererbtes Order-Volumen. | `Strategy.Volume` |

Alle Parameter sind für die Optimierung verfügbar.

## Handelslogik
1. Abonnieren der ausgewählten Kerzenserie und Berechnen des Stochastik-Oszillators.
2. Wenn der vorherige Wert von %K unter %D lag und der aktuelle %K über %D kreuzt, wird eine Long-Position eröffnet.
3. Wenn der vorherige Wert von %K über %D lag und der aktuelle %K unter %D kreuzt, wird eine Short-Position eröffnet.
4. Die Strategie hält jeweils nur eine Position. Kreuzungen in der entgegengesetzten Richtung schließen die Position und öffnen die entgegengesetzte Seite.
5. `StartProtection()` aktiviert die von StockSharp bereitgestellten Standard-Verlust-/Gewinnschutzmechanismen.

## Visualisierung
Die Strategie zeigt automatisch Kerzen, den Stochastik-Indikator und ausgeführte Trades im Chart an.

## Hinweise
- Funktioniert mit jedem Instrument und Zeitrahmen.
- Parameter sollten an die Volatilität des ausgewählten Marktes angepasst werden.
