# Strategie Expert ADC PL Stoch Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Expert ADC PL Stoch Strategy** ist eine Candlestick-Muster-Strategie, die vom ursprünglichen MQL5 Expert Advisor *Expert_ADC_PL_Stoch* umgewandelt wurde. Es sucht nach bullischen Piercing Line- und bärischen Dark Cloud Cover-Formationen auf fertigen Kerzen und bestätigt die Signale mit der %D-Linie eines Stochastic-Oszillators. The method is trend-following when the market retraces into an established move and requires the oscillator to be in extreme zones before opening positions. Positionsausstiege basieren auf Stochastic-Übergängen aus extremen Bereichen und spiegeln die abstimmungsbasierte Ausstiegslogik des Quellsystems wider.

## Handelslogik

1. Subscribe to a configurable candle type (default: 1-hour time frame).
2. Behalten Sie für jede fertige Kerze die letzten Kerzen bei, die für die Auswertung des Candlestick-Musters benötigt werden, und die aktuellen Stochastic %D-Werte.
3. **Long Entry**
   - The previous candle pair must form a Piercing Line pattern:
     - Die Kerze am Balken *t-1* ist bullisch mit einem Körper, der größer als die durchschnittliche Körpergröße ist.
     - Die Kerze am Balken *t-2* ist bärisch mit einem Körper, der über dem Durchschnitt liegt.
     - Die zinsbullische Kerze klafft unterhalb des bärischen Tiefs und schließt wieder innerhalb des bärischen Körpers, während der Gesamttrend gemäß dem Schlussdurchschnitt abwärtsgerichtet ist.
   - Der Stochastic %D-Wert auf Balken *t-1* muss unter dem Schwellenwert für den langen Einstieg liegen (Standard 30).
4. **Short Entry**
   - Das vorherige Kerzenpaar muss ein Dark Cloud Cover-Muster bilden:
     - Die Kerze am Balken *t-2* ist bullisch mit einem großen Körper.
     - Die Kerze bei Balken *t-1* öffnet über dem vorherigen Hoch und schließt wieder innerhalb des bullischen Körpers.
     - Der mittlere Preis der rückläufigen Kerze liegt über dem gleitenden Durchschnitt der Schlusskurse, was einen Aufwärtstrend vor der Umkehr signalisiert.
   - Der Stochastic %D auf Balken *t-1* muss über dem Short-Entry-Schwellenwert liegen (Standard 70).
5. **Exit Conditions**
   - Long positions are closed when the Stochastic %D on bar *t-1* crosses below either the upper (80) or lower (20) thresholds compared with bar *t-2*.
   - Short-Positionen werden geschlossen, wenn der Stochastic %D auf Balken *t-1* entweder den unteren (20) oder oberen (80) Schwellenwert im Vergleich zu Balken *t-2* überschreitet.
6. Alle Berechnungen werden an fertigen Kerzen durchgeführt; Es wird keine Intrabar-Verarbeitung verwendet.

## Parameter

| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `CandleType` | Zeitrahmen der zur Mustererkennung verwendeten Kerzen. | 1 Stunde |
| `StochasticLength` | Basislänge für den Stochastic-Oszillator. | 47 |
| `StochasticKPeriod` | Glättungslänge für die %K-Linie. | 9 |
| `StochasticDPeriod` | Glättungslänge für die %D-Linie. | 13 |
| `StochasticSlow` | Zusätzlicher Verlangsamungsfaktor, der auf den Oszillator angewendet wird. | 3 |
| `AverageBodyPeriod` | Number of candles used to measure the reference body size and close average. | 5 |
| `LongEntryThreshold` | Maximal zulässiger %D-Wert vor dem Einstieg in Long-Trades. | 30 |
| `ShortEntryThreshold` | Vor dem Eingehen von Short-Trades ist ein Mindestwert von %D erforderlich. | 70 |
| `ExitLowerThreshold` | Untere Grenze, die für Ausgangskreuzungen verwendet wird. | 20 |
| `ExitUpperThreshold` | Upper boundary used for exit crossovers. | 80 |

## Risikomanagement

- Die Strategie sendet Marktaufträge unter Verwendung des Basisstrategievolumens (Standard 1 Kontrakt/Lot).
- No automatic protective orders are configured; external risk management or `StartProtection` can be added if needed.
- Es wird jeweils nur eine Position verwaltet; Gegensignale schließen die aktive Position, bevor sie eine neue eröffnen.

## Notizen

- Durchschnittliche Kerzenkörper und Schlussdurchschnitte werden aus historischen Kerzen berechnet, um die MQL5-Abstimmungslogik genau nachzubilden.
- Stochastic-Werte werden pro fertigem Balken gespeichert, um die gleichen Offsets auszuwerten, die im ursprünglichen Expert Advisor verwendet wurden.
- Trades werden erst dann eröffnet und geschlossen, wenn die Strategie vollständig ausgearbeitet ist und der Handel durch die Basisklassenprüfungen zulässig ist.
