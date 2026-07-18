# DealersTradeZeroLag MACD-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie portiert den MetaTrader Expert Advisor "Dealers Trade v 7.91 ZeroLag MACD" auf die StockSharp High-Level-API. Sie verfolgt die Steigung eines Zero-Lag-MACDs, um zu entscheiden, ob der Markt in einer Akkumulationsphase für Longs oder Shorts ist, und baut ein Positions-Grid mit adaptivem Abstand und Risikomanagement auf. Der Standard-Zeitrahmen sind Vier-Stunden-Kerzen, wie vom ursprünglichen Autor empfohlen, aber jeder von StockSharp unterstützte Kerzentyp kann ausgewählt werden.

## Handelslogik
- **Signalerkennung.** Zwei Zero-Lag-exponentielle gleitende Durchschnitte (schnell und langsam) erzeugen eine MACD-Linie. Wenn der MACD im Vergleich zum vorherigen Balken steigt, behandelt die Strategie den Markt als bullisch; wenn er fällt, behandelt sie ihn als bärisch. Das Signal kann über den Parameter `ReverseCondition` invertiert werden.
- **Positions-Grid.** Der Algorithmus skaliert in die erkannte Richtung. Abstände zwischen Einstiegen werden in Pips gemessen und nach jedem Füllen mit `IntervalCoefficient` multipliziert. Die Lot-Größe wird bei jedem zusätzlichen Einstieg mit `LotMultiplier` multipliziert, was das Martingal-Schema der MQL-Version nachahmt.
- **Volumenkontrolle.** Wenn `BaseVolume` größer als null ist, wird es als anfängliche Ordermenge verwendet. Andernfalls leitet der Engine die Größe aus `RiskPercent`, der Stop-Distanz und den Instrument-Schritt-Parametern ab. Jedes berechnete Volumen wird gegen die Instrument-Grenzen geprüft und durch `MaxVolume` gedeckelt.
- **Ordermanamgent.** Jeder Einstieg kann mit einem Stop-Loss, Take-Profit und Trailing-Stop (alles in Pips) ausgestattet werden. Die Take-Profit-Distanz wird für aufeinanderfolgende Einstiege mit `TakeProfitCoefficient` multipliziert, um Ziele zu erweitern.
- **Kontoschutz.** Wenn die Gesamtanzahl offener Positionen `PositionsForProtection` überschreitet und ihr kombinierter Gewinn `SecureProfit` erreicht, schließt die Strategie den Trade mit dem größten Gewinn, um Gewinne zu sichern. Wenn die Gesamtanzahl der Positionen `MaxPositions` übersteigt, schließt sie den schlechtesten Trade, bevor neue Einstiege akzeptiert werden.

## Positionshandling
- Stops, Trailing-Logik und Ziele werden bei abgeschlossenen Kerzen mit Schlusskurs, Hoch- und Tiefpreisen bewertet.
- Alle offenen Positionen werden mit eigenem Volumen, Einstiegspreis und Trailing-Zustand verfolgt. Der letzte Füllpreis wird wiederverwendet, um den Mindestabstand für zukünftige Einstiege durchzusetzen.
- Wenn der Kontostand unter `MinimumBalance` fällt, hält die Strategie sich selbst an, um Übertrading auf kleinen Konten zu vermeiden.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `BaseVolume` | Anfängliche Ordergröße. Auf null setzen, um risikobasiertes Sizing über `RiskPercent` zu aktivieren. |
| `RiskPercent` | Prozentsatz des Portfolio-Eigenkapitals, das riskiert wird, wenn die Positionsgröße aus der Stop-Distanz abgeleitet wird. |
| `MaxPositions` | Maximale Anzahl gleichzeitig offener Einstiege. |
| `IntervalPips` | Anfänglicher Abstand zwischen Grid-Einstiegen in Pips. |
| `IntervalCoefficient` | Multiplikator, der nach jedem zusätzlichen Einstieg auf den Abstand angewendet wird. |
| `StopLossPips` | Stop-Loss-Distanz in Pips. Auf null setzen, um zu deaktivieren. |
| `TakeProfitPips` | Basis-Take-Profit-Distanz in Pips. Pro Einstieg mit `TakeProfitCoefficient` multipliziert. |
| `TrailingStopPips` / `TrailingStepPips` | Trailing-Stop-Distanz und erforderlicher Vorschub, bevor der Trail angepasst wird. |
| `TakeProfitCoefficient` | Multiplikator für das Erweitern von Take-Profit-Distanzen bei späteren Einstiegen. |
| `SecureProfit` | Gewinnschwelle, die den Kontoschutz auslöst, sobald genügend Positionen offen sind. |
| `AccountProtection` | Aktiviert automatisches Gewinnsichern durch Schließen des besten Trades. |
| `PositionsForProtection` | Mindestanzahl offener Positionen, die erforderlich ist, bevor der Kontoschutz aktiv wird. |
| `ReverseCondition` | Invertiert die MACD-Steigungs-Interpretation. |
| `FastLength`, `SlowLength`, `SignalLength` | Perioden der Zero-Lag-exponentiellen gleitenden Durchschnitte. |
| `MaxVolume` | Obergrenze für das Volumen eines einzelnen Einstiegs. |
| `LotMultiplier` | Multiplikativer Faktor für das Skalieren der Positionsgröße mit jedem Grid-Einstieg. |
| `MinimumBalance` | Mindest-Kontostand, der für die Fortsetzung des Handels erforderlich ist. |
| `CandleType` | Kerzendatentyp für Berechnungen. |

## Verwendungshinweise
1. Verbinden Sie die Strategie mit einem Portfolio und einem Instrument, bevor Sie sie starten.
2. Überprüfen Sie den Instrument-Schritt und die Preiseinstellungen, um sicherzustellen, dass Pip-Konvertierungen korrekt sind.
3. Die Standard-Parameter replizieren das Verhalten des ursprünglichen Expert Advisors, können aber durch StockSharp-Optimierer optimiert werden.
4. Eine Python-Übersetzung ist für diese Strategie nicht enthalten.
