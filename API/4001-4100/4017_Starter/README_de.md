# Starter-Strategie 2005
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Starter 2005-Strategie** ist eine StockSharp High-Level-API-Konvertierung des klassischen MetaTrader 4-Expertenberaters `Starter.mq4` aus dem Jahr 2005. Das ursprüngliche System kombinierte einen Laguerre-Oszillator, einen exponentiellen Steigungsfilter für den gleitenden Durchschnitt und eine Commodity Channel Index-Bestätigung. Dieser Port hält den Entscheidungsbaum intakt und passt gleichzeitig die Geldverwaltung und -ausführung an die StockSharp-Konventionen an:

- Ein Laguerre RSI-Proxy repliziert den `iCustom("Laguerre")`-Puffer, der zwischen 0 und 1 schwankt.
- Ein 5-Perioden-EMA, der auf dem Medianpreis berechnet wird, liefert die gleiche steigende/fallende Steigungsbestätigung, die vom MT4-Experten verwendet wird.
- Ein 14-Perioden-CCI, gemessen an den Schlusskursen, filtert schwache Setups heraus, genau wie die ursprüngliche `Alpha`-Variable.
- Die adaptive Losgrößenroutine spiegelt die historische `LotsOptimized()`-Funktion wider, einschließlich streifenbasierter Reduzierungen nach aufeinanderfolgenden Verlusten.
- Positionsausstiege werden entweder dadurch ausgelöst, dass Laguerre die Extremzone verlässt oder dass der Handel eine konfigurierbare Gewinndistanz von `Point * Stop` erreicht.

## Handelslogik
1. **Indikatorvorbereitung**
   - Der Laguerre-RSI-Wert wird durch einen vierstufigen Laguerre-Filter mit konfigurierbarem `Gamma` rekonstruiert.
   - Die Länge von EMA beträgt standardmäßig fünf Kerzen und wird auf `(High + Low) / 2` angewendet, um `PRICE_MEDIAN` in MQL4 abzugleichen.
   - Der Zeitraum CCI beträgt bei Schlusskursen standardmäßig 14, und ein sehr kleiner Schwellenwert (`±5`) wird beibehalten, um dem alten Code treu zu bleiben.
2. **Lange Einrichtung**
   - Laguerre muss nahe Null liegen (`LaguerreEntryTolerance` emuliert den strengen `== 0`-Vergleich).
   - EMA muss im Vergleich zur vorherigen fertigen Kerze steigen.
   - CCI muss unter `-CciThreshold` fallen.
3. **Kurze Einrichtung**
   - Laguerre muss nahe bei einem sitzen (`1 - LaguerreEntryTolerance` entspricht ungefähr `== 1`).
   - EMA muss fallen.
   - CCI muss über `+CciThreshold` steigen.
4. **Ausgänge**
   - Long-Positionen schließen, wenn Laguerre über `LaguerreExitHigh` steigt (Standardwert `0.9`) oder wenn der Preis vom Einstiegspunkt aus um `TakeProfitPoints * PriceStep` steigt.
   - Shorts schließen, wenn Laguerre unter `LaguerreExitLow` (Standard `0.1`) fällt oder wenn der Preis um die gleiche Distanz fällt.
   - Jede andere manuelle flache Position setzt den internen Status automatisch zurück, um veraltete Eingabedaten zu verhindern.

## Geldmanagement
Der `CalculateOrderVolume`-Helfer reproduziert das MT4-`LotsOptimized()`-Verhalten:

1. **Risikobasierte Dimensionierung** – Eigenkapital multipliziert mit `MaximumRisk` wird durch `RiskDivider` dividiert (Standard 500, wie in der ursprünglichen `/500`-Regel). Dividiert durch den aktuellen Preis ergibt dies die risikoadjustierte Losgröße.
2. **Fallback-Lot** – Wenn die Risikodimensionierung eine kleinere Zahl als `BaseVolume` ergibt, behält der Algorithmus das Basislos.
3. **Reduzierung der Verluststrähne** – Nach zwei oder mehr aufeinanderfolgenden Verlustgeschäften wird das Volumen um `volume * losses / DecreaseFactor` reduziert, was genau der MQL-Schleife entspricht, die den Handelsverlauf überprüft hat.
4. **Normalisierung** – Volumina werden auf `VolumeStep` des Instruments normalisiert und zwischen `MinVolume` und `MaxVolume` eingeklemmt, um abgelehnte Bestellungen zu vermeiden.

Die aufeinanderfolgende Verlustverfolgung wird nach jedem gewinnbringenden Ausstieg zurückgesetzt und nach verlorenen Trades erhöht. Break-Even-Ergebnisse lassen den Schalter unberührt und spiegeln das ursprüngliche Verhalten wider, bei dem Null-Gewinn-Tickets ignoriert wurden.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `BaseVolume` | `decimal` | `1.2` | Die Mindestlosgröße wird verwendet, wenn die Risikogröße eine geringere Menge nahelegt. |
| `MaximumRisk` | `decimal` | `0.036` | Anteil des Eigenkapitals, der für eine neue Position vor Anwendung des Teilers exponiert wird. |
| `RiskDivider` | `decimal` | `500` | Auf das Risikokapital angewendeter Divisor, der die ursprüngliche `AccountFreeMargin() * MaximumRisk / 500`-Regel reproduziert. |
| `DecreaseFactor` | `decimal` | `2` | Streak-Divisor, der verwendet wird, um das Volumen nach aufeinanderfolgenden Verlusten zu schrumpfen. |
| `MaPeriod` | `int` | `5` | EMA Länge des Kerzenmittelpreises. |
| `CciPeriod` | `int` | `14` | Rückblick auf den Commodity Channel Index. |
| `CciThreshold` | `decimal` | `5` | Absoluter CCI-Pegel, der zum Auslösen eines Signals erforderlich ist. |
| `LaguerreGamma` | `decimal` | `0.66` | Glättungsfaktor des Laguerre-Filters. |
| `LaguerreEntryTolerance` | `decimal` | `0.02` | Eine Toleranz von etwa 0/1 wird verwendet, um die ursprünglichen Gleichheitsprüfungen nachzuahmen. |
| `LaguerreExitHigh` | `decimal` | `0.9` | Oberes Ausstiegsniveau für Long-Positionen. |
| `LaguerreExitLow` | `decimal` | `0.1` | Niedrigeres Ausstiegsniveau für Short-Positionen. |
| `TakeProfitPoints` | `decimal` | `10` | Gewinnziel ausgedrückt in Preispunkten (`Point * Stop` in MQL). |
| `CandleType` | `DataType` | `TimeFrame(5m)` | Von der Strategie verarbeitetes Kerzenabonnement. |

## Hinweise zur Implementierung
- Laguerre RSI wird inline mithilfe der vierstufigen Rekursion des ursprünglichen Indikators implementiert; Es sind keine Anrufe an `GetValue()` erforderlich.
- Die Indikatoren EMA und CCI werden im Kerzenrückruf manuell aktualisiert, um sicherzustellen, dass der Medianpreis-Feed mit der Option `PRICE_MEDIAN` von MetaTrader übereinstimmt.
- Markteintritte berücksichtigen die Flags `AllowLong()`/`AllowShort()` und stellen sicher, dass keine aktiven Aufträge ausstehen, wobei das Einzelpositionsdesign der Quelle EA erhalten bleibt.
- Die Verfolgung der Handelsergebnisse nutzt den Entscheidungspreis der Kerze (letzter Preis, Schlusskurs oder Eröffnungskurs), um die PnL-Richtung abzuschätzen und den Verluststreak-Zähler zu verwalten.
- Englische Inline-Kommentare beschreiben jeden wichtigen Entscheidungsblock, um zukünftige Wartungsarbeiten zu erleichtern.

## Anwendungstipps
- Der ursprüngliche EA war für Intraday-FX-Charts gedacht; Beginnen Sie mit liquiden Instrumenten, die kleine Preisschritte bieten, damit das 10-Punkte-Gewinnziel mit einem Pip übereinstimmt.
- Da das MT4-Skript immer nur eine Position hält, führen Sie die Strategie in Umgebungen aus, in denen Teilfüllungen und gleichzeitige Aufträge unwahrscheinlich sind (historische Tests oder liquide Märkte).
- Passen Sie `LaguerreEntryTolerance` an, wenn der Oszillator in Ihrem Datensatz selten genau 0 oder 1 berührt.
- Stimmen Sie `RiskDivider` und `DecreaseFactor` gemeinsam ab, um Risikowachstum und Verlustminderung in Einklang zu bringen.
