# Burg Extrapolator Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die Burg Extrapolator Strategie repliziert den MetaTrader Expert Advisor "Burg Extrapolator" mit der StockSharp High-Level-API. Das System wendet ein mit der Burg-Methode gelöstes autoregressives (AR) Modell an, um zukünftige Eröffnungspreise vorherzusagen. Handelsentscheidungen werden durch die Amplitude der Prognosepfad-Amplitude gesteuert: Wenn die vorhergesagte Distanz zwischen zukünftigen Hochs und Tiefs die konfigurierten Schwellenwerte überschreitet, öffnet oder schließt die Strategie Positionen.

## Kernlogik

1. **Datenvorbereitung**
   - Sammelt `PastBars` Eröffnungspreise bei jeder fertigen Kerze.
   - Transformiert optional die Reihe in logarithmische Momentum- oder Rate-of-Change-Werte.
   - Normalisiert Preise durch Subtraktion des gleitenden Durchschnitts bei Verwendung von Rohpreisen.
2. **Autoregressives Modelling**
   - Schätzt AR-Koeffizienten durch die Burg-Methode mit einer durch `ModelOrderFraction` bestimmten Ordnung.
   - Extrapoliert mehrere Schritte voraus (Prognosehorizont = `PastBars - order - 1`) und rekonstruiert Preisvorhersagen.
3. **Signalgenerierung**
   - Verfolgt die maximalen und minimalen vorhergesagten Preise.
   - Wenn der Prognose-Swing `MinProfitPips` überschreitet, wird ein Einstiegssignal in der jeweiligen Richtung generiert.
   - Wenn der Prognose-Swing `MaxLossPips` überschreitet, wird ein Ausstiegssignal für bestehende Positionen ausgegeben.
4. **Orderausführung**
   - Positionen werden mit Marktorders unter Verwendung des berechneten risikobasierten Volumens eröffnet.
   - Wenn ein Stop oder ein entgegengesetztes Signal auftritt, schließt die Strategie Positionen mit Marktorders.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `RiskPercent` | Pro Trade riskierter Eigenkapitalprozentsatz. Dient zur Ordergrößenbestimmung, wenn ein Stop-Loss-Abstand verfügbar ist. |
| `MaxPositions` | Maximales kumuliertes Volumen als Vielfaches der zulässigen Ordergröße pro Richtung. |
| `MinProfitPips` | Mindest-vorhergesagter Gewinn-Swing (in Pips), der zum Öffnen neuer Positionen erforderlich ist. |
| `MaxLossPips` | Maximal zulässiger vorhergesagter Drawdown (in Pips), der Positionsausstiege auslöst. |
| `TakeProfitPips` | Statischer Take-Profit-Abstand (in Pips). Auf null setzen zum Deaktivieren. |
| `StopLossPips` | Statischer Stop-Loss-Abstand (in Pips). Erforderlich für Risikosizing. |
| `TrailingStopPips` | Trailing-Stop-Abstand (in Pips). Funktioniert nur wenn Stop-Loss aktiviert ist. |
| `PastBars` | Anzahl historischer Balken als Eingabe in das Burg-Modell. |
| `ModelOrderFraction` | Fraktion von `PastBars`, die die AR-Ordnung definiert (ganzzahlige Kürzung). |
| `UseMomentum` | Aktiviert logarithmische Momentum-Vorverarbeitung (`log(p[i]/p[i-1])`). |
| `UseRateOfChange` | Aktiviert Rate-of-Change-Vorverarbeitung (`p[i]/p[i-1]-1`), wenn Momentum deaktiviert ist. |
| `OrderVolume` | Fallback-Ordergröße, wenn risikobasiertes Sizing nicht berechnet werden kann. |
| `CandleType` | Datentyp (Zeitrahmen) der für Berechnungen verwendeten Kerzen. |

## Handelsregeln

- **Einstieg**: Wenn der vorhergesagte Pfad einen Swing größer als `MinProfitPips` anzeigt, eine Long-Position öffnen, wenn der höchste projizierte Preis zuerst kommt, oder eine Short-Position, wenn die niedrigste Projektion zuerst erscheint.
- **Ausstieg**: Positionen schließen, wenn der Prognose-Swing `MaxLossPips` überschreitet oder das entgegengesetzte Einstiegssignal erkannt wird.
- **Schutz**: Verwendet `StartProtection` zur Konfiguration optionaler Stop-Loss, Take-Profit und Trailing Stop in absoluten Kurseinheiten abgeleitet von Pips.
- **Positionssizing**: Wenn sowohl `StopLossPips` als auch `RiskPercent` positiv sind, wird das Tradingvolumen als `risk_amount / (stop_distance)` berechnet. Andernfalls wird `OrderVolume` verwendet.

## Implementierungshinweise

- Arbeitet ausschließlich mit fertigen Kerzen, um Look-Ahead-Bias zu vermeiden.
- Vermeidet Indikator-`GetValue`-Aufrufe durch direkte Verarbeitung von Werten innerhalb des `Bind`-Callbacks.
- Respektiert die Konventionen der StockSharp High-Level-API mit `SubscribeCandles` und `StartProtection` für das Risikomanagement.
- Trailing-Logik spiegelt den Original-EA durch Aktivierung plattformverwalteter Trailing Stops wider.

## Verwendungstipps

- `PastBars` und `ModelOrderFraction` sorgfältig wählen; hohe Ordnungen können zu Overfitting oder instabilen Prognosen führen.
- Der Prognosehorizont entspricht `PastBars - order - 1`; sicherstellen, dass der Horizont mindestens einige Balken beträgt, indem `ModelOrderFraction` unter 1 gehalten wird.
- Momentum- und ROC-Modi erfordern positive Preise. Instrumente, die null kreuzen können, sollten beim Rohpreismodus bleiben.
- Für Märkte mit fraktionalen Pips skaliert die Strategie die Pip-Größe automatisch unter Verwendung der Dezimalstellen des Instruments (×10 für 3 oder 5 Dezimalstellen).

## Einschränkungen

- Das AR-Modell setzt Stationarität voraus; starke Trends oder Regimewechsel können die Genauigkeit reduzieren.
- Prognosebasierte Signale sind empfindlich gegenüber Rauschen – in Betracht ziehen, mit zusätzlichen Filtern zu kombinieren, wenn im Live-Handel verwendet.
- Genaues Risikosizing erfordert Portfoliobewertung und einen gültigen Stop-Loss-Abstand; andernfalls werden Standardvolumina verwendet.
