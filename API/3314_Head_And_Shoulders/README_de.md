# Head-and-Shoulders-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Head-and-Shoulders-Strategie** ist ein direkter Port des MetaTrader-Expert-Advisors "HEAD AND SHOULDERS" (MQL ID 26066). Der ursprüngliche Roboter kombiniert Erkennung des Kopf-Schulter-Musters mit Momentum-, Moving-Average- und MACD-Filtern und verwaltet Positionen mit Trailing Stops, Equity-Schutz und Break-even-Regeln. Diese StockSharp-Implementierung konzentriert sich auf die diskretionäre Logik der Ein- und Ausstiegsengine über die High-Level-API und bietet saubere Indikatorbindungen sowie automatisiertes Risikomanagement über `StartProtection`.

## Handelslogik
1. **Mustererkennung**
   - Verwendet ein Fünf-Bars-Fraktalfenster zur Annäherung von Swing-Hochs und -Tiefs und spiegelt damit die fraktalbasierte Mustererkennung des Quell-EA.
   - Bestätigt ein *bärisches* Kopf-Schulter-Muster, wenn drei sequenzielle Fraktalhochs erscheinen und das mittlere Hoch (der Kopf) beide Schultern um eine konfigurierbare Dominanzschwelle übersteigt.
   - Bestätigt ein *invertiertes* Kopf-Schulter-Muster, wenn drei sequenzielle Fraktaltiefs entstehen und das mittlere Tief ausreichend unter beiden Schultern liegt.
   - Die Nackenlinie wird aus den jüngsten Fraktaltiefs (bärisches Muster) oder -hochs (bullisches Muster) zwischen Schultern und Kopf berechnet.
2. **Momentum- und Trendfilter**
   - Schnelle und langsame einfache gleitende Durchschnitte müssen mit der erwarteten Trendrichtung übereinstimmen.
   - Absolutes Momentum (Differenz zwischen aktuellem Wert und Rückblickperiode) muss eine Schwelle überschreiten und in dieselbe Richtung wie der Trade zeigen.
   - Der MACD-Wert muss zur Ausbruchsrichtung passen, um Gegentrendsignale zu vermeiden.
3. **Ausbruchsausführung**
   - Long-Trades werden ausgelöst, wenn der Schlusskurs über die bullische Nackenlinie ausbricht und alle Filter übereinstimmen.
   - Short-Trades werden ausgelöst, wenn der Schlusskurs unter die bärische Nackenlinie bricht und bärische Filter ausgerichtet sind.
4. **Positionsverwaltung**
   - Positionen steigen aus, wenn die Nackenlinie in Gegenrichtung verletzt wird oder die gleitenden Durchschnitte und MACD ihre Ausrichtung verlieren.
   - Optionale Schutzorders werden über den integrierten `StartProtection`-Helper mit Stop-Loss-, Take-Profit- und Trailing-Stop-Parametern in Preisschritten konfiguriert.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `CandleType` | 1H-Zeitrahmen | Primäre Kerzenserie für die Mustererkennung. |
| `OrderVolume` | `1` | Basisordergröße. |
| `FastMaLength` / `SlowMaLength` | `6` / `85` | Längen der Moving-Average-Trendfilter. |
| `MomentumPeriod` | `14` | Rückblickperiode für den Momentum-Indikator. |
| `MomentumThreshold` | `0.3` | Minimales absolutes Momentum zur Bestätigung. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | `12`, `26`, `9` | MACD-Konfiguration. |
| `ShoulderTolerancePercent` | `5` | Maximal erlaubte Abweichung zwischen linker und rechter Schulter. |
| `HeadDominancePercent` | `2` | Mindestbetrag, um den der Kopf jede Schulter übersteigen muss. |
| `StopLossSteps`, `TakeProfitSteps`, `TrailingStopSteps` | `100`, `200`, `0` | Schutzordergrößen in Preisschritten (null deaktiviert eine Komponente). |

Alle mit `Param()` erstellten Parameter stellen Metadaten für die UI bereit und können über den StockSharp-Optimierer optimiert werden.

## Unterschiede zum Original-Experten
- Entfernt MetaTrader-spezifische Equity-Stop-, Trailing- und Orderänderungsroutinen zugunsten der integrierten Portfolio-Schutzmechanismen von StockSharp.
- Konzentriert sich ausschließlich auf Marktorders und High-Level-API-Aufrufe (`BuyMarket` / `SellMarket`).
- Vereinfacht Hilfsfunktionen wie Alerts, Push-Benachrichtigungen und das Zeichnen grafischer Objekte; die StockSharp-Version protokolliert Erkennungen stattdessen mit `LogInfo`.
- Die Mustererkennung behält den Geist der ursprünglichen fraktalbasierten Logik bei, wurde aber neu geschrieben, um direkten Datenarray-Zugriff und Orderticket-Manipulation zu vermeiden.

## Nutzungshinweise
- Da die Strategie auf abgeschlossenen Kerzen basiert, stellen Sie sicher, dass Datenabonnements fertige Bars liefern (`CandleStates.Finished`).
- Trailing-Schutz verwendet Preisschritte; prüfen Sie, dass `Security.PriceStep` die Tickgröße des Instruments widerspiegelt, bevor Trailing Stops aktiviert werden.
- Der Musterdetektor speichert nur jüngste Fraktale, um unbegrenzte Sammlungen zu vermeiden, und eignet sich daher für lange Live-Sitzungen.
- Für zusätzliche Bestätigungsebenen (z. B. MACD auf höherem Zeitrahmen wie im Original-EA) erweitern Sie die Strategie mit zusätzlichen Abonnements nach demselben Binding-Ansatz.

## Referenzen
- MetaTrader Expert Advisor: `HEAD AND SHOULDERS.mq4` (MQL ID 26066).
- StockSharp-Dokumentation zu [High-Level-Strategien](https://doc.stocksharp.com/topics/strategy/highlevel.html) und [Indikatorbindung](https://doc.stocksharp.com/topics/strategy/highlevel/bind.html).
