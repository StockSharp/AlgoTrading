# The MasterMind 2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie kombiniert den **Stochastic Oscillator** und **Williams %R**, um extreme überverkaufte und überkaufte Bedingungen zu identifizieren.
Eine Long-Position wird eröffnet, wenn die Signallinie des Stochastic unter **3** fällt und Williams %R unter **-99.9** liegt.
Eine Short-Position wird eröffnet, wenn die Signallinie des Stochastic über **97** steigt und Williams %R über **-0.1** liegt.

Die Risikosteuerung umfasst einen initialen Stop Loss und Take Profit, einen Trailing Stop mit einstellbarem Schritt und einen optionalen Break-Even-Auslöser, der den Stop nach ausreichendem Gewinn auf den Einstiegspreis verschiebt.

## Parameter

- `LotSize` - Handelsvolumen in Kontrakten.
- `StochasticPeriod` - Periode für den Stochastic Oscillator.
- `StochasticK` - Glättung der %K-Linie.
- `StochasticD` - Glättung der %D-Linie (Signal).
- `WilliamsRPeriod` - Periode für Williams %R.
- `StopLossPoints` - Initialer Stop Loss in Preispunkten.
- `TakeProfitPoints` - Initialer Take Profit in Preispunkten.
- `TrailingStopPoints` - Trailing-Stop-Abstand in Punkten.
- `TrailingStepPoints` - Minimale günstige Bewegung vor Aktualisierung des Trailing Stops.
- `BreakEvenPoints` - Abstand in Punkten zum Verschieben des Stops auf Break-Even.
- `CandleType` - Typ und Zeitrahmen der für Berechnungen verwendeten Kerzen.

## Handelslogik

1. **Einstiegssignale**
   - **Kaufen** wenn Stochastic-Signal < 3 und Williams %R < -99.9.
   - **Verkaufen** wenn Stochastic-Signal > 97 und Williams %R > -0.1.
2. **Ausstiegssignale**
   - Entgegengesetzte Einstiegssignale schließen bestehende Positionen.
   - Stop Loss, Take Profit, Break-Even und Trailing Stop werden bei jeder Kerze angewendet.

## Hinweise

- Funktioniert mit jedem Instrument, das die erforderlichen Indikatoren unterstützt.
- Für Lernzwecke und weitere Experimente konzipiert.
