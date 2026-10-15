# Martingail Expert Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
- Portierung des MetaTrader 5 Expert Advisors **MartingailExpert.mq5**.
- Verwendet einen Stochastik-Oszillator-Crossover mit konfigurierbaren %K-, %D- und Verlangsamungsparametern zum Öffnen von Positionen.
- Implementiert ein martingaleartiges Grid mit Durchschnitts- und Ausbruchseinträgen, die das Volumen geometrisch skalieren.
- Entwickelt für Netto-Portfolios – die Strategie hält eine einzige aggregierte Long- oder Short-Position.

## Handelslogik
### Einstiegskriterien
1. Die Strategie verarbeitet geschlossene Kerzen des `CandleType`-Zeitrahmens.
2. Stochastik-Werte werden von der vorherigen abgeschlossenen Kerze genommen, um den MQL-Aufruf `iStochastic(..., 1)` zu imitieren.
3. Ein Long-Einstieg wird ausgelöst, wenn:
   - Das vorherige %K größer als das vorherige %D ist.
   - Das vorherige %D über `BuyLevel` liegt.
   - Keine offene Position vorhanden ist.
4. Ein Short-Einstieg wird ausgelöst, wenn:
   - Das vorherige %K unter dem vorherigen %D liegt.
   - Das vorherige %D unter `SellLevel` liegt.
   - Keine offene Position vorhanden ist.
5. Alle Marktorders verwenden den normalisierten `Volume`-Wert (gerundet auf den nächsten `Security.VolumeStep`).

### Positions-Skalierung
- `ProfitPips` definiert den Abstand (in Pips), der erforderlich ist, um eine weitere Basisposition in Gewinnrichtung hinzuzufügen.
  - Bei Long: wenn das Kerzenhoch `lastEntryPrice + ProfitPips * positionCount` erreicht, wird eine neue Order mit dem Basis-`Volume` gesendet.
  - Bei Short: wenn das Kerzentief `lastEntryPrice - ProfitPips * positionCount` erreicht, wird eine Basisorder gesendet.
- `StepPips` definiert den Durchschnittsabstand (in Pips) zur Anwendung des Martingale-Multiplikators.
  - Bei Long: wenn das Kerzentief `lastEntryPrice - StepPips` berührt, entspricht das nächste Ordervolumen `lastVolume * Multiplier`.
  - Bei Short: wenn das Kerzenhoch `lastEntryPrice + StepPips` berührt, wird dieselbe Martingale-Dimensionierung angewendet.
- Jeder ausgeführte Trade aktualisiert `lastEntryPrice`, `lastVolume` und die interne Zählung aktiver Positionen.

### Ausstiegslogik
- Der zuletzt ausgeführte Trade-Preis wird pro Richtung gespeichert.
- Wenn der Preis `lastEntryPrice ± ProfitPips` erreicht (Kerzenhochs für Longs und -tiefs für Shorts), werden alle offenen Positionen per Marktorder geschlossen.
- Sobald die aggregierte Position auf null zurückgeht, werden die Martingale-Zustandsvariablen zurückgesetzt.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `Volume` | `0.03` | Basis-Lotgröße für die Erstorder und gewinnbasierte Ergänzungen. |
| `Multiplier` | `1.6` | Martingale-Multiplikator für Durchschnittseinträge. |
| `StepPips` | `25` | Pip-Abstand, der Durchschnittsorders gegen den Trend auslöst. |
| `ProfitPips` | `9` | Pip-Abstand für Gewinnausstieg und Ausbruchs-Ergänzungen. |
| `KPeriod` | `5` | Lookback-Periode der stochastischen %K-Berechnung. |
| `DPeriod` | `3` | Glättungsperiode für die stochastische %D-Linie. |
| `Slowing` | `3` | Auf die %K-Linie angewendete Glättung (langsame Stochastik). |
| `BuyLevel` | `20` | Mindestwert von %D, der für Long-Einträge erforderlich ist. |
| `SellLevel` | `55` | Höchstwert von %D, der für Short-Einträge erforderlich ist. |
| `CandleType` | 5-Minuten-Zeitrahmen | Zeitrahmen zum Erstellen von Kerzen und Indikatoren. |

## Implementierungshinweise
- Der Pip-Abstand wird aus `Security.PriceStep` berechnet. Instrumente mit 3 oder 5 Dezimalstellen werden automatisch angepasst, indem der Preisschritt mit 10 multipliziert wird, um der ursprünglichen MQL-Pip-Logik zu entsprechen.
- Volumina werden auf den nächsten `Security.VolumeStep` abgerundet. Werte, die unter den handelbaren Mindestschritt fallen, werden ignoriert.
- Die Strategie verlässt sich auf Kerzenhochs und -tiefs, um Intra-Bar-Trigger zu approximieren, da die High-Level-API auf abgeschlossenen Kerzen operiert.
- `OnOwnTradeReceived` verfolgt tatsächliche Ausführungspreise und -volumina, um die Martingale-Eskalationssequenz originalgetreu zu reproduzieren.

## Verwendungshinweise
- Richten Sie `CandleType` am Zeitrahmen der ursprünglichen MetaTrader-Vorlage aus (üblicherweise M5), um ein ähnliches Verhalten zu erzielen.
- Stellen Sie sicher, dass die Instrumenten-Metadaten (Preisschritt, Volumenschritt) ausgefüllt sind; andernfalls passen Sie `Volume`, `StepPips` und `ProfitPips` manuell an die Broker-Spezifikationen an.
- Erwägen Sie die Aktivierung eines externen Risikomanagements (Stop-Losses oder Kapitallimits), da die Martingale-Logik das Exposure bei nachteiligen Bewegungen absichtlich erhöht.

## Unterschiede zum originalen Expert Advisor
- Die StockSharp-Version verarbeitet abgeschlossene Kerzen statt jeden Tick; Schwellenwertprüfungen verwenden Kerzenhochs/-tiefs zur Approximation des Intra-Bar-Verhaltens.
- MetaTrader-spezifische Kontomargenprüfungen sind in StockSharp High-Level-Strategien nicht verfügbar; stellen Sie sicher, dass ausreichend Kapital extern konfiguriert ist.
- Orderausführung und Positions-Tracking nutzen das Netting-Modell von StockSharp; der Hedging-Modus wird nicht unterstützt.
