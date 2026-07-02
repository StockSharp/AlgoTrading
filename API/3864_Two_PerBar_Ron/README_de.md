# TwoPerBar Ron-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Der ursprüngliche MetaTrader-Experte „TwoPerBar“ von Ron Thompson eröffnet **zwei Marktorders zu Beginn jedes neuen Balkens** – eine Long- und eine Short-Order. Immer wenn ein Zweig ein festes Cash-Ziel erreicht (`ProfitMade * Point` im MQL-Code), wird er geschlossen, und bei der Eröffnung des nächsten Balkens wird das verbleibende Engagement liquidiert, bevor ein neues abgesichertes Paar erstellt wird. Wenn der vorherige Balken mit offenen Positionen endete, wird die Lotgröße bis zu einer Sicherheitsobergrenze (`LotLimit`) verdoppelt. Der StockSharp-Port reproduziert dieses Verhalten mithilfe der High-Level-Strategie API, Level-1-Kursen zur Bid/Ask-Überwachung und expliziter Nachverfolgung der beiden abgesicherten Zweige.

## Handelsablauf
1. **Balkenerkennung** – `SubscribeCandles(CandleType)` benachrichtigt die Strategie, wenn die konfigurierte Kerzenserie endet. Eine abgeschlossene Kerze markiert den Beginn eines neuen Balkens, genau wie der `Time[0]`-Wechsel von MetaTrader.
2. **Gewinnprüfung** – Snapshots der Ebene 1 (Bid/Ask) werden kontinuierlich überwacht. Sobald sich der beste Geld- oder Briefkurs weit genug vom erfassten Einstiegspreis entfernt, wird das Matching-Leg mit `SellMarket` oder `BuyMarket` geschlossen.
3. **Zwangsliquidation** – zu Beginn eines neuen Balkens werden alle verbleibenden Abschnitte zum Marktwert geschlossen. Dies spiegelt die `OrderClose`-Schleife im MQL-Skript wider.
4. **Volumenskalierung** – wenn im vorherigen Zyklus aktive Trades stattfanden, wird die Lotgröße mit `VolumeMultiplier` multipliziert (Standard: `2`). Andernfalls wird es auf `BaseVolume` zurückgesetzt. Der Wert wird gegen den Lautstärkeschritt des Instruments normalisiert und durch `MaxVolume` und den Austausch `Security.MaxVolume` begrenzt.
5. **Hedge-Erstellung** – zwei Marktaufträge werden über `BuyMarket` und `SellMarket` gesendet. Jeder Zweig merkt sich sein Zielvolumen, die tatsächlich gefüllte Größe und den gewichteten durchschnittlichen Füllpreis, sodass die Gewinnprüfungen auf der Grundlage präziser Informationen erfolgen.

## Risiko- und Geldmanagement
- **Martingale-Stilskalierung** – die Verdoppelung der Menge nach einem unvollendeten Zyklus ahmt die ursprüngliche Martingal-Größe nach. Wenn sich beide Beine während der Bar schließen, wird die Sequenz auf das Basislos zurückgesetzt.
- **Pro-Leg-Gewinnziele** – `ProfitTargetPoints` übersetzt die Eingabe MetaTrader `ProfitMade`. Der Wert wird mit der Punktgröße des Instruments multipliziert und mit dem Geld-/Briefkurs verglichen, um zu entscheiden, wann ein Abschnitt beendet werden soll.
- **Börsenkonformität** – `NormalizeVolume` stellt sicher, dass generierte Lose die Instrumente `VolumeStep` und `MinVolume` respektieren. Übergroße Werte lösen eine Rücksetzung auf eine handelbare Menge aus.
- **Hedged Accounting** – die Strategie verfügt über eine eigene Liste von Zweigen, da StockSharp-Portfolios normalerweise nur Nettopositionen offenlegen. Dadurch können Umgebungen, die abgesicherte Konten unterstützen, dasselbe Verhalten verfolgen.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-Minuten-Kerzen | Primärer Zeitrahmen, der signalisiert, wann ein neuer Balken begonnen hat. |
| `BaseVolume` | `decimal` | `0.1` | Anfängliche Losgröße für einen brandneuen Zyklus. |
| `VolumeMultiplier` | `decimal` | `2` | Der Multiplikator wird angewendet, nachdem ein Balken mit offenen Positionen endet. |
| `MaxVolume` | `decimal` | `12.8` | Harte Obergrenze für die Martingal-Losgröße. |
| `ProfitTargetPoints` | `decimal` | `19` | Gewinnziel ausgedrückt in Punkten; mit der Punktgröße des Instruments multipliziert und mit Geld-/Briefkursen verglichen. |

## Unterschiede zur MQL-Version
- Verwendet `SubscribeLevel1()` anstelle von Tick-by-Tick-`Bid`/`Ask`-Globalen, behält aber die gleiche Logik basierend auf den besten Kursen bei.
- Bestellungen werden über StockSharp-Hilfsmethoden (`BuyMarket`, `SellMarket`) gesendet, sodass alle börsenspezifischen Rundungen automatisch erfolgen.
- Bei der Volume-Verarbeitung werden `VolumeStep`, `MinVolume` und `MaxVolume` berücksichtigt, während das ursprüngliche Skript mit rohen Doppelwerten arbeitete.
- Der StockSharp-Port speichert intern Streckeninformationen. Konnektoren, die im Netting-Modus ausgeführt werden, können Absicherungen immer noch abflachen. Stellen Sie daher sicher, dass Ihr Broker gegensätzliche Positionen unterstützt.

## Anwendungstipps
- Ordnen Sie `BaseVolume` einer gültigen Losgröße für das ausgewählte Instrument zu. andernfalls überspringt der Normalisierungsschritt den Handel.
- Halten Sie `ProfitTargetPoints` an der Punktgröße des Symbols ausgerichtet – übermäßig große Werte werden selten innerhalb eines einzelnen Balkens erreicht.
- Da die Strategie gegensätzliche Marktaufträge sendet, führen Sie sie auf Demo-Datenquellen oder Absicherungskonten aus, bevor Sie in Produktionsumgebungen wechseln.
- Hängen Sie die Strategie an ein Diagramm an: `OnStarted` fügt Kerzen und ausgeführte Trades zum visuellen Diagramm hinzu, um die Überwachung zu erleichtern.
