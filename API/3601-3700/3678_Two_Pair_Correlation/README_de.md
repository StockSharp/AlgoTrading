# Zwei-Paar-Korrelationsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Zwei-Paar-Korrelationsstrategie** portiert den MetaTrader-Expertenberater *"2-Paar-Korrelation EA"* (Paket `MQL/52043`) auf die StockSharp-Hochebene API. Es überwacht die Geldkurse zweier stark korrelierender Kryptosymbole (BTCUSD als Primärzweig und ETHUSD als Absicherungszweig) und führt einen marktneutralen Handel durch, wenn deren Spread von einem konfigurierbaren Schwellenwert abweicht.

### Kernworkflow

1. **Risk Gating** – das Portfolioeigenkapital wird kontinuierlich überwacht. Wenn der Rückgang vom historischen Höchstwert `MaxDrawdownPercent` überschreitet, werden neue Geschäfte ausgesetzt, bis sich das Eigenkapital wieder über `RecoveryPercent` des Höchstwerts erholt.
2. **Volatilitätsfilter** – beide Instrumente speisen einen 5-minütigen Kerzenstrom in einen `AverageTrueRange`-Indikator der Länge `AtrPeriod` ein. Der Handel wird übersprungen, wenn einer von ATR den Wert `PriceDifferenceThreshold * 0.01` überschreitet, was die „Hohe Volatilitätspause“ aus dem Code MQL nachahmt.
3. **Spread-Erkennung** – Die Strategie abonniert Level-1-Daten für beide Instrumente und bewertet die Geld-Preis-Spanne bei jeder Aktualisierung. Bei `Bid(BTCUSD) - Bid(ETHUSD) > PriceDifferenceThreshold` wird BTCUSD gekauft und ETHUSD verkauft. Wenn der Spread unter `-PriceDifferenceThreshold` fällt, werden die Positionen umgekehrt (Short BTCUSD, Long ETHUSD).
4. **Dynamische Lot-Größe** – das Volumen pro Bein wird aus `RiskPercent` des aktuellen Portfolio-Eigenkapitals dividiert durch die synthetische Stop-Distanz `StopLossPips * PriceStep` abgeleitet. Das Ergebnis wird mit den Börsenvolumenbeschränkungen normalisiert, bevor Orders gesendet werden.
5. **Basket-Exit** – der gesamte variable Gewinn beider Zweige wird in der Kontowährung erfasst. Sobald es `MinimumTotalProfit` erreicht, schließt die Strategie das gesamte Paar, unabhängig von der Einstiegsrichtung.

## Erforderliche Marktdaten

- **Level1** (bester Geld-/Briefkurs) sowohl für das primäre Wertpapier (`Security`) als auch für das Hedge-Wertpapier (`SecondSecurity`).
- **Kerzen** vom Typ `AtrCandleType` (standardmäßig 5-Minuten-Zeitrahmen) für dieselben zwei Instrumente zur Versorgung des ATR-Filters.

Stellen Sie sicher, dass die Wertpapiere aussagekräftige Werte für `PriceStep`, `StepPrice`, `VolumeStep` und minimale/maximale Volumenwerte aufweisen, damit die Losgröße und die Gewinnumrechnung das MetaTrader-Verhalten widerspiegeln.

## Parameter

| Name | Typ | Standard | Beschreibung |
| ---- | ---- | ------- | ----------- |
| `SecondSecurity` | `Security` | — | Absicherungsinstrument (ETHUSD im Original EA). |
| `MaxDrawdownPercent` | `decimal` | `20` | Drawdown-Schwellenwert, der neue Trades pausiert. |
| `RiskPercent` | `decimal` | `2` | Portfolioanteil, der pro Trade für die Positionsgröße riskiert wird. |
| `PriceDifferenceThreshold` | `decimal` | `100` | Zur Eröffnung des Paares ist eine Divergenz zwischen Geldkurs und Kurs erforderlich. |
| `MinimumTotalProfit` | `decimal` | `0.30` | Gewinnziel in Kontowährung für den Abschluss beider Legs. |
| `AtrPeriod` | `int` | `14` | ATR Länge für den Volatilitätsfilter. |
| `RecoveryPercent` | `decimal` | `95` | Prozentsatz des Spitzenkapitals, der erforderlich ist, um den Handel nach einem Drawdown wieder aufzunehmen. |
| `StopLossPips` | `int` | `50` | Synthetischer Stopp, der zur Umwandlung von `RiskPercent` in Lose verwendet wird. |
| `AtrCandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | Für die ATR-Berechnung verwendete Kerzenserie. |

## Dateien

- `CS/TwoPairCorrelationStrategy.cs` – Strategieumsetzung basierend auf dem High-Level API.
- `README.md` – diese Dokumentation (Englisch).
- `README_zh.md` – Dokumentation auf Chinesisch.
- `README_ru.md` – Dokumentation auf Russisch.
