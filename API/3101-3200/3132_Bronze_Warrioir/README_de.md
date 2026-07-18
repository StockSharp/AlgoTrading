# Bronze Warrioir-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
- Konvertierung des MetaTrader 5-Experten *Bronze Warrioir.mq5* in die StockSharp High-Level-API.
- Handelt ein einzelnes Symbol mit fertigen Kerzen und kombiniert CCI, Williams %R und einen proprietären „DayImpuls"-Oszillator.
- Fokussiert auf das Erfassen von Momentum-Ausbrüchen, wenn DayImpuls-Anstieg, Williams-%R-Extreme und CCI-Werte übereinstimmen.

## Indikator-Stack
- **Commodity Channel Index (CCI)** – klassischer CCI mit dem konfigurierten `IndicatorPeriod`. Long-Signale erfordern einen Wert unter `-CciLevel`; Short-Signale benötigen einen Wert über `CciLevel`.
- **Williams %R** – auf demselben Zeitraum angewendet. Ein Wert über `WilliamsLevelUp` bestätigt überkauftes Terrain, Werte unter `WilliamsLevelDown` bestätigen überverkaufte Niveaus.
- **DayImpuls Oszillator** – Replik des enthaltenen benutzerdefinierten Indikators. Wandelt jeden Kerzenkörper in Punkte um (Schlusskurs minus Eröffnung geteilt durch den Instrument-Punktwert) und wendet zwei aufeinanderfolgende exponentielle gleitende Durchschnitte mit demselben Zeitraum an. Steigende Werte zeigen wachsenden Bullen-Druck; fallende Werte zeigen Bären-Druck.

## Handelslogik
1. **Eigenkapitalschutz** – bevor Signale generiert werden, akkumuliert die Strategie den schwebenden PnL der aktuellen Exposition. Steigt er über `ProfitTarget` oder fällt unter `LossTarget`, werden alle offenen Positionen sofort geschlossen.
2. **Eintriegsfilter** – fertige Kerzen sind obligatorisch. Der Algorithmus benötigt einen gespeicherten DayImpuls-Wert der vorherigen Kerze, um den ursprünglichen Look-Back mit `custom[1]` zu emulieren.
3. **Short-Setup** – ausgelöst wenn:
   - Keine aktive Short-Exposition vorhanden.
   - DayImpuls liegt über `DayImpulsLevel` und ist größer als sein vorheriger Wert (positives Momentum).
   - Williams %R liegt über `WilliamsLevelUp` (überkauft) und CCI ist größer als `CciLevel`.
   - Orders verwenden `TradeVolume` plus offenes Long-Volumen für eine Umkehrung in einer einzigen Transaktion im StockSharp-Netting-Modell.
4. **Long-Setup** – symmetrische Bedingungen:
   - Keine aktive Long-Exposition.
   - DayImpuls liegt unter `DayImpulsLevel` und ist kleiner als sein vorheriger Wert (fallendes Momentum).
   - Williams %R liegt unter `WilliamsLevelDown` und CCI ist kleiner als `-CciLevel`.
   - Verwendet `TradeVolume` plus ausstehende Short-Volumen für eine vollständige Umkehrung wenn nötig.
5. **Hedge-artige Umkehrungen** – wenn nur eine gerichtete Exposition vorhanden ist und der schwebende PnL das Band `[-PredTarget / 2, PredTarget]` verlässt, validiert der EA den Martingale-Schritt über den `LotCoefficient`-Parameter. Im StockSharp-Port bleibt die Validierung erhalten, aber die tatsächliche Ausführung führt eine Schließen-und-Umkehren-Order durch, da die Plattform Nettopositionen statt unabhängiger Hedge-Tickets führt.

## Risikomanagement
- `StopLossPips` und `TakeProfitPips` werden mithilfe des `PriceStep` des Instruments in Preisabstände umgewandelt. Für 3- oder 5-stellige Forex-Symbole wird ein zusätzlicher Faktor von 10 angewendet, um MetaTrader-„Pips" zu emulieren.
- Beide Werte werden an den High-Level-`StartProtection`-Helper übergeben, der automatische Stop-Loss- und Take-Profit-Niveaus an die aktive Position anfügt.
- Die Strategie führt intern Long/Short-Volumen-Tracking, damit `GetOpenPnL` mit der MetaTrader-Berechnung übereinstimmt, die `Commission + Swap + Profit` für jedes Ticket summiert.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `TradeVolume` | Basis-Ordervolumen in Lots. | `1` |
| `StopLossPips` | Schutz-Stop in Pips, umgerechnet in Preisabstand. | `50` |
| `TakeProfitPips` | Gewinnziel in Pips, umgerechnet in Preisabstand. | `50` |
| `IndicatorPeriod` | Zeitraum für CCI, Williams %R und DayImpuls. | `14` |
| `CciLevel` | Absoluter CCI-Schwellenwert für Trades. | `150` |
| `WilliamsLevelUp` | Williams %R Überkauft-Niveau (negativer Wert). | `-15` |
| `WilliamsLevelDown` | Williams %R Überverkauft-Niveau (negativer Wert). | `-85` |
| `DayImpulsLevel` | DayImpuls-Schwellenwert zur Trennung bullischer/bärischer Regime. | `50` |
| `ProfitTarget` | Schwebender Gewinnziel in Kontowährung. | `100` |
| `LossTarget` | Schwebende Verlustgrenze in Kontowährung. | `-100` |
| `PredTarget` | Band zur Auslösung von Mittelungskehrungen. | `40` |
| `LotCoefficient` | Vom EA geerbter Validierungskoeffizient. | `2` |
| `CandleType` | Für alle Indikatoren verwendeter Zeitrahmen. | `15m`-Kerzen |

## Implementierungshinweise
- Der DayImpuls Oszillator ist als innere Indikatorklasse eingebettet und spiegelt die ursprüngliche doppelte EMA-Glättungslogik wider.
- Da StockSharp-Strategien Nettopositionen verwalten, werden simultane Long/Short-Hedges der MQL-Version emuliert, indem Schließ- und Eröffnungsvolumen innerhalb derselben Marktorder kombiniert werden.
- Die Strategie funktioniert nur mit fertigen Kerzen und verwendet `IsFormedAndOnlineAndAllowTrading()`, um den globalen Strategie-Lebenszyklus zu respektieren.
- Long/Short-Durchschnittspreise werden über `OnOwnTradeReceived` verfolgt, damit Teilschließungen und Umkehrungen den schwebenden PnL korrekt aktualisieren.
