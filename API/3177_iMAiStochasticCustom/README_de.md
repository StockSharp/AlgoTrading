# iMA iStochastic Custom-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie repliziert den MetaTrader Expert **„iMA iStochastic Custom"** innerhalb des StockSharp-Frameworks. Sie kombiniert eine gleitende Durchschnittshülle mit einem stochastischen Oszillatorfilter. Der Handel findet auf abgeschlossenen Kerzen des ausgewählten Zeitrahmens (`CandleType`) statt. Alle Kommentare verwenden die gleiche Nomenklatur wie der ursprüngliche Advisor.

Schlüsselkomponenten:

1. **Gleitender Durchschnittshülle** – der Basis-MA wird um `LevelUpPips` und `LevelDownPips` (in Pips) verschoben, um Widerstands- und Unterstützungsbänder zu bilden. Die Mittelungsmethode entspricht MetaTrader-Optionen: Simple, Exponential, Smoothed (SMMA) und Linear Weighted (LWMA).
2. **Stochastischer Oszillator** – %K, %D und Glättungslängen folgen den Originalparametern. Zwei Schwellenwerte (`StochasticLevel1` und `StochasticLevel2`) validieren überkaufte/überverkaufte Bedingungen.
3. **Geldverwaltung** – der ursprüngliche `lot`/`risk`-Selektor wird über den Parameter `ManagementMode` erhalten. Im `FixedLot`-Modus entspricht die Ordergröße `VolumeValue`. Im `RiskPercent`-Modus riskiert die Strategie den konfigurierten Prozentsatz des Portfolio-Eigenkapitals gegen den Stop-Loss-Abstand, reproduzierend das Verhalten von `CMoneyFixedMargin`.
4. **Schutzmaßnahmen** – Stop-Loss-, Take-Profit- und Trailing-Abstände werden in Pips eingegeben. Trailing wird auf abgeschlossenen Kerzen aktualisiert, was die MQL-Logik widerspiegelt und gleichzeitig mit dem Ereignismodell von StockSharp kompatibel bleibt.

## Handelslogik
Long- und Short-Signale sind symmetrisch:

- **Kauf**, wenn der Kerzen-Schlusskurs über der oberen Hülle (`ma + LevelUpPips`) liegt und eine der stochastischen Linien über `StochasticLevel1` liegt.
- **Verkauf**, wenn der Kerzen-Schlusskurs unter der unteren Hülle (`ma + LevelDownPips`) liegt und eine der stochastischen Linien unter `StochasticLevel2` liegt.
- Das Setzen von `ReverseSignals = true` tauscht die Einstiegsrichtung.

Nur eine Nettoposition ist gleichzeitig aktiv. Wenn das Signal wechselt, sendet die Strategie eine Order, die groß genug ist, um das aktuelle Exposure zu neutralisieren und eine neue Position in entgegengesetzter Richtung zu eröffnen.

## Risikokontrolle und Ausstiege
- **Stop-Loss / Take-Profit** – Abstände in Pips werden über den `PriceStep` des Instruments konvertiert. Sie werden auf jeder fertigen Kerze anhand von Hoch/Tief geprüft.
- **Trailing-Stop** – beginnt, nachdem sich der Preis um `TrailingStopPips` zugunsten der Position bewegt hat. Es erfordert eine zusätzliche Verbesserung von `TrailingStepPips` vor jeder Anpassung, genau wie die MQL-Trailing-Routine.
- **Geldverwaltung** – im Risikomodus ist die Positionsgröße `equity * VolumeValue / 100 / perUnitRisk`, wobei `perUnitRisk` der monetäre Verlust pro Lot bis zum Stop-Loss ist.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Zeitrahmen für Berechnungen. |
| `StopLossPips`, `TakeProfitPips` | Schutzabstände in Pips. |
| `TrailingStopPips`, `TrailingStepPips` | Trailing-Aktivierung und -Schritt (Pips). Null setzen zum Deaktivieren. |
| `ManagementMode`, `VolumeValue` | Feste Lot- oder Risikoprozent-Dimensionierung. |
| `MaPeriod`, `MaShift`, `MaMethod` | Gleitdurchschnittslänge, Bar-Shift und Methode (SMA/EMA/SMMA/LWMA). |
| `LevelUpPips`, `LevelDownPips` | Obere/untere Hüllversätze in Pips. Negative Werte sind für das untere Band erlaubt. |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing` | Oszillatorkonfiguration. |
| `StochasticLevel1`, `StochasticLevel2` | Bestätigungsniveaus für Kauf-/Verkaufsprüfungen. |
| `ReverseSignals` | Richtung aller Trades umkehren. |

## Implementierungshinweise
- Kerzen, Indikatoren und Orders sind über die High-Level-API (`SubscribeCandles().BindEx(...)`) verbunden.
- Die Pip-Größe passt sich automatisch an 3/5-stellige Forex-Symbole an, indem der `PriceStep` bei Bedarf multipliziert wird.
- Trailing-Logik läuft auf abgeschlossenen Kerzen. Wenn Intrabar-Trailing erforderlich ist, die Logik in Tick-Daten einhängen.
- Kein Python-Port bereitgestellt; der `PY`-Ordner ist wie gewünscht absichtlich nicht vorhanden.

## Unterschiede zur MetaTrader-Version
- Die Risikodimensionierung ist explizit und basiert auf StockSharp Portfolio-Metriken anstelle der `CMoneyFixedMargin`-Hilfsklasse. Die resultierenden Lots stimmen mit dem Originalverhalten überein, wenn Stop-Loss aktiviert ist; bei null Stop-Loss bleibt die Positionsgröße null, was die MQL-Schutzmaßnahme widerspiegelt.
- Schutzprüfungen (Stop-Loss, Take-Profit, Trailing) werden auf abgeschlossenen Kerzen ausgewertet, da StockSharp-Strategien ereignisgesteuert sind. Dies hält die Logik deterministisch und entspricht Backtesting-Anforderungen.
- Das Logging wird auf StockSharp-Standardausgabe vereinfacht; das verbose `InpPrintLog`-Flag wird nicht übernommen.

Verwenden Sie diese Strategie als direkten Drop-in-Ersatz beim Migrieren von MetaTrader zu StockSharp Designer oder Runner. Parameter an das Zielinstrument und den Zeitrahmen anpassen.
