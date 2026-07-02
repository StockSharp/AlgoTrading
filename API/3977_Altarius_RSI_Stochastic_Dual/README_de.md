# Altarius RSI Stochastic Duale Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Altarius RSI Stochastic Dual Strategy ist eine Umsetzung des MetaTrader Expertenberaters `AltariusRSIxampnSTOH`. Die Logik kombiniert zwei stochastische Oszillatoren mit einem kurzperiodischen RSI-Filter. Die langsame Stochastik identifiziert die Trendrichtung und überkaufte/überverkaufte Zonen, während die schnelle Stochastik die Momentumstärke misst. Exits basieren auf RSI und der langsamen stochastischen Signallinie, um erfolgreiche Trades zu verfolgen und Verluste zu reduzieren. Zusätzliche Geldverwaltungsfunktionen spiegeln die ursprüngliche MQL-Logik wider, indem sie die Positionsgröße nach Verlusten reduzieren und ein Eigenkapital-Inanspruchnahmelimit durchsetzen.

## Handelslogik

1. **Datenquelle** – Die Strategie funktioniert mit konfigurierbaren Kerzen (standardmäßige 15-Minuten-Balken). Alle Berechnungen basieren auf Candle-Close-Daten.
2. **Eintrittsbedingungen**
   - **Langer Setup**: Die langsame stochastische Hauptlinie (15,8,8) liegt über ihrer Signallinie, aber immer noch unter `BuyStochasticLimit` (standardmäßig 50). Die schnelle Stochastik (10,3,3) zeigt Impuls mit einer absoluten Differenz zwischen Haupt- und Signallinien über `StochasticDifferenceThreshold` (standardmäßig 5).
   - **Kurze Einrichtung**: Die langsame stochastische Hauptlinie liegt unter ihrer Signallinie, bleibt aber über `SellStochasticLimit` (standardmäßig 55). Die schnelle Stochastik muss erneut eine Differenz aufweisen, die größer als die Impulsschwelle ist.
3. **Exit Conditions**
   - **Langer Ausstieg**: Wird ausgelöst, wenn RSI (Periode 4) `ExitRsiHigh` (60) überschreitet und die langsame stochastische Signallinie unter ihren vorherigen Wert fällt, während sie über `ExitStochasticHigh` (70) bleibt.
   - **Kurzer Ausstieg**: Wird ausgelöst, wenn RSI unter `ExitRsiLow` (40) fällt und die langsame stochastische Signallinie über ihren vorherigen Wert steigt, während sie unter `ExitStochasticLow` (30) bleibt.
   - **Risikoausstieg**: Wenn der variable PnL unter den zulässigen Eigenkapital-Drawdown (`MaximumRiskPercent`) fällt, werden alle Positionen sofort abgeflacht.
4. **Positionsgröße** – Beginnt mit `BaseVolume` und reduziert die effektive Größe nach aufeinanderfolgenden Verlustgeschäften über `DecreaseFactor`. Volumenbeschränkungen des Brokers werden mithilfe der Sicherheitsvolumenschritte und -grenzen berücksichtigt.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `BaseVolume` | Basisauftragsgröße vor Risikomanagementanpassungen. |
| `MaximumRiskPercent` | Prozentsatz des Kontokapitals, der verloren gehen kann, bevor die Strategie Positionen zwangsweise schließt. |
| `DecreaseFactor` | Divider, der steuert, wie schnell sich die Positionsgröße nach aufeinanderfolgenden Verlusten verringert. |
| `RsiPeriod` | RSI Länge, die für Ausstiegsentscheidungen verwendet wird. |
| `SlowStochasticPeriod`, `SlowStochasticK`, `SlowStochasticD` | Konfiguration für den langsamen stochastischen Oszillator, der die Trendrichtung bestimmt. |
| `FastStochasticPeriod`, `FastStochasticK`, `FastStochasticD` | Konfiguration für den schnellen stochastischen Oszillator, der den Impuls misst. |
| `StochasticDifferenceThreshold` | Mindestabstand zwischen schnellen stochastischen Haupt- und Signalleitungen zur Bestätigung des Impulses. |
| `BuyStochasticLimit`, `SellStochasticLimit` | Langsame stochastische Niveaus, die die akzeptable Handelszone für neue Positionen definieren. |
| `ExitRsiHigh`, `ExitRsiLow` | RSI Level, die Long- oder Short-Exits vorbereiten. |
| `ExitStochasticHigh`, `ExitStochasticLow` | Langsame stochastische Signalpegel, die Exits abschließen. |
| `CandleType` | Kerzendatenquelle für Indikatorberechnungen. |

## Notizen

- Die Strategie handelt jeweils nur eine Position und spiegelt das ursprüngliche Verhalten des Expertenberaters wider.
- Volumenanpassungen und Drawdown-Schutz werden anhand der aktuellen Portfolioinformationen berechnet, die in StockSharp verfügbar sind.
- Bei der Diagrammvisualisierung werden Kerzen, sowohl stochastische Oszillatoren als auch Handelsmarkierungen gezeichnet, wenn ein Diagrammbereich verfügbar ist.
