# Strategie MarsiEaStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

`MarsiEaStrategy` repliziert die Logik des ursprünglichen MetaTrader MARSIEA-Expertenberaters innerhalb der StockSharp-Hochebene API. Die Strategie kombiniert einen einfachen gleitenden Durchschnitt mit einem Filter für den relativen Stärkeindex (RSI) und hält jeweils nur eine einzige Position. Schützende Stop-Loss- und Take-Profit-Orders werden genau wie die Quellimplementierung in Pips gemessen, während das gehandelte Volumen dynamisch anhand des Portfoliokapitals ermittelt wird.

## Handelslogik

1. **Datenvorbereitung**
   - Ein einfacher gleitender Durchschnitt (SMA) mit konfigurierbarer Länge läuft auf der ausgewählten Kerzenserie.
   - Ein RSI mit konfigurierbarem Zeitraum verwendet dieselben Kerzen.
   - Die Kerzenserie kann über den Parameter `CandleType` konfiguriert werden und ist standardmäßig auf einminütige Kerzen eingestellt.

2. **Eintrittsregeln**
   - Die Strategie erfordert, dass beide Indikatoren gebildet werden und keine offenen Positionen vorhanden sind.
   - **Langes Setup:** Der Schlusskurs liegt über SMA und der RSI liegt unter dem Überverkaufsschwellenwert.
   - **Short-Setup:** Der Schlusskurs liegt unter SMA und der RSI liegt über der überkauften Schwelle.
   - Es kann jeweils nur eine Position offen sein, was dem Expertenverhalten von MetaTrader entspricht.

3. **Ausgangsregeln**
   - Unmittelbar nach dem Eingehen eines Handels registriert die Strategie eine feste Stop-Loss- und Take-Profit-Distanz, beide definiert in Pips.
   - Es gibt keine zusätzlichen Austrittsbedingungen; Die Schutzanordnungen regeln die Positionsschließung.

## Risiko- und Positionsgrößenbestimmung

- `RiskPercent` steuert den Prozentsatz des aktuellen Portfoliowerts, der pro Trade riskiert wird.
- Der Pip-Wert wird aus `Security.PriceStep`, `Security.StepPrice` und der Anzahl der Ziffern berechnet und emuliert die `_Digits`-Prüfung aus MQL.
- Das Volumen wird auf den nächsten zulässigen `Security.VolumeStep` gerundet und berücksichtigt `Security.VolumeMin`, sofern verfügbar.
- Wenn die risikobasierte Größenbestimmung nicht berechnet werden kann (fehlende Instrumentenmetadaten oder Nullstopp), greift die Strategie auf die Eigenschaft `Volume` zurück (standardmäßig 1 Kontrakt/Lot).

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Für Indikatorberechnungen verwendete Kerzenserien. |
| `MaPeriod` | Länge des SMA-Indikators. |
| `RsiPeriod` | Lookback-Länge für RSI. |
| `RsiOverbought` | RSI-Schwellenwert, der einen überkauften Markt für Shorts definiert. |
| `RsiOversold` | RSI-Schwellenwert, der einen überverkauften Markt für Long-Positionen definiert. |
| `RiskPercent` | Prozentsatz des pro Trade riskierten Eigenkapitals. |
| `StopLossPips` | Stop-Loss-Distanz, ausgedrückt in Pips. |
| `TakeProfitPips` | Take-Profit-Distanz, ausgedrückt in Pips. |

## Hinweise zur Umstellung

- Die MetaTrader-Implementierung wird zu Bid/Ask-Preisen gehandelt; Dieser Port verwendet den Kerzenschluss als Einstiegsreferenz, da Intrabar-Ticks im hohen Level API nicht verfügbar sind.
- Die Pip-Größe folgt der gleichen Regel wie die MQL-Version: Fünf- oder dreistellige Symbole multiplizieren den Preisschritt mit zehn.
- `StartProtection()` wird einmal aufgerufen, sodass Stop-Loss- und Take-Profit-Orders von der Engine automatisch mit der offenen Position verknüpft werden.
- Die Strategie behält das ursprüngliche Verhalten bei, neue Einträge zu überspringen, während eine beliebige Position aktiv ist.
