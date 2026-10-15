# ABH_BH_MFI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **ABH_BH_MFI-Strategie** ist eine StockSharp-High-Level-Portierung des MetaTrader-Expertenberaters „Expert_ABH_BH_MFI“. Der Algorithmus kombiniert bullische und bärische Harami-Kerzenmuster mit Bestätigung durch den Money Flow Index (MFI). Long-Trades werden ausgelöst, wenn sich in einem fallenden Markt ein bullischer Harami bildet, während der MFI weiterhin niedrig bleibt. Short-Trades erfordern einen bärischen Harami in einem steigenden Markt und einen erhöhten MFI. Die ursprüngliche MQL-Implementierung basierte auf der Signalinfrastruktur von MetaTrader; Diese Konvertierung behält die Entscheidungslogik bei, drückt sie jedoch mit den Kerzenabonnements, der Indikatorbindung und den Positionsverwaltungshelfern von StockSharp aus.

## Handelslogik
### 1. Harami-Mustererkennung
- Die Strategie speichert die beiden zuletzt abgeschlossenen Kerzen.
- Ein **bullischer Harami** erfordert:
  - Vor zwei Kerzen gab es eine lange schwarze (bärische) Kerze, deren Körper größer als die durchschnittliche Körperlänge ist.
  - Die jüngste Kerze ist bullisch und ihr Eröffnungs-/Schlusskurs wird vom Körper der vorherigen bärischen Kerze verschlungen.
  - Der Mittelpunkt der älteren Kerze liegt unter dem einfachen gleitenden Durchschnitt der Schlusskurse, was auf einen vorherrschenden Abwärtstrend hinweist.
- Ein **bärischer Harami** spiegelt diese Anforderungen mit invertierten Farben und dem Mittelpunkt über dem gleitenden Durchschnitt wider, um einen Aufwärtstrend zu bestätigen.

### 2. Bestätigung des Geldflussindex
- Das MFI verwendet den konfigurierbaren `MfiPeriod` (Standard **37**), um die ursprünglichen Oszillatoreinstellungen zu reproduzieren.
- Bei langen Einträgen muss der zuletzt abgeschlossene MFI-Wert unter `BullishThreshold` (Standard **40**) bleiben, um eine Erschöpfung des Kapitalzuflusses sicherzustellen.
- Bei Short-Einträgen muss der MFI über `BearishThreshold` (Standard **60**) bleiben, um die Erschöpfung des Kaufdrucks anzuzeigen.

### 3. Ausstiegsregeln durch MFI-Crossovers
- Aktive Long-Positionen werden geschlossen, wenn der MFI entweder `ExitLowerLevel` (Standard **30**) oder `ExitUpperLevel` (Standard **70**) überschreitet, was den MetaTrader-Bedingungen `MFI(1) > level && MFI(2) < level` entspricht.
- Aktive Short-Positionen werden geschlossen, wenn der MFI die überkaufte Zone überschreitet oder unter das überverkaufte Niveau fällt, was den ursprünglichen Short-Ausstiegsklauseln entspricht.

### 4. Risikomanagement
- Die Strategie wendet optional `StartProtection` mit Stop-Loss- und Take-Profit-Offsets an, ausgedrückt in Preisschritten. Wenn Sie den entsprechenden Parameter auf Null setzen, wird der Schutzabstand deaktiviert und die MetaTrader-Standardwerte reproduziert.
- Die Positionsgröße verwendet die Basiseigenschaft `Volume`; Durch das Umkehren von Positionen werden automatisch genügend Kontrakte hinzugefügt, um sie abzuflachen und in der neuen Richtung wieder zu öffnen, genau wie der Quellenexperte.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `CandleType` | 1-stündiger Zeitrahmen | Primäre Kerzenserien auf Muster und MFI analysiert. |
| `MfiPeriod` | 37 | Rückblick auf den Money-Flow-Index-Indikator. |
| `BodyAveragePeriod` | 11 | Länge der einfachen gleitenden Durchschnitte, die die Körpergröße und den Schlusstrend messen. |
| `BullishThreshold` | 40 | Maximal zulässiger MFI-Wert vor der Eröffnung von Long-Trades. |
| `BearishThreshold` | 60 | Erforderlicher Mindest-MFI-Wert vor der Eröffnung von Short-Trades. |
| `ExitLowerLevel` | 30 | Niedrigeres MFI-Crossover-Niveau für Positionsausstiege. |
| `ExitUpperLevel` | 70 | Oberer MFI-Crossover-Level für Positionsausstiege. |
| `StopLossPoints` | 0 | Optionale Stop-Loss-Distanz in Preisschritten (0 deaktiviert). |
| `TakeProfitPoints` | 0 | Optionale Take-Profit-Distanz in Preisschritten (0 deaktiviert). |

## Implementierungshinweise
- Kerzendaten werden über `SubscribeCandles(CandleType)` empfangen und nur verarbeitet, wenn der Kerzenstatus `Finished` ist, wodurch die Übereinstimmung mit der geschlossenen Balkenlogik des MQL-Experten sichergestellt wird.
- Der MFI-Indikator ist direkt mit `.Bind(_mfi, ProcessCandle)` gebunden, sodass der Handler gebrauchsfertige Dezimalwerte erhält, ohne `GetValue` aufzurufen.
- Zwei zusätzliche einfache gleitende Durchschnitte replizieren die Hilfsfunktionen `AvgBody` und `CloseAvg` aus dem Code MetaTrader. Ihre Ergebnisse werden zwischengespeichert, um Abfragen historischer Indikatoren zu vermeiden.
- Bei Ausstiegs- und Einstiegsentscheidungen rufen Sie `IsFormedAndOnlineAndAllowTrading()` ab, bevor Sie Aufträge senden, und bleiben dabei im Einklang mit den von StockSharp empfohlenen Handelssicherheitsprüfungen.

## Unterschiede zum MetaTrader Expert
- Die Geldverwaltung wird auf das Basisstrategievolumen vereinfacht. Das ursprüngliche „Fixed Lot“-Modul wurde in den Positionsgrößen-Helfer von StockSharp übersetzt, der die gleiche Funktionalität ohne separate Klassen abdeckt.
- Die Trailing-Stop-Komponente MetaTrader (`TrailingNone`) hatte keine Logik; Die StockSharp-Version verzichtet daher auf alle nachgestellten Aktionen, behält aber optionale feste Risikoziele bei.
- Die Protokollierung ist standardmäßig minimal; Sie können es mit `LogInfo`-Aufrufen erweitern, wenn Sie ausführliche Handelsdiagnosen benötigen.

## Nutzungstipps
1. Konfigurieren Sie die gewünschte Sicherheit und weisen Sie `CandleType` zu, bevor Sie mit der Strategie beginnen.
2. Passen Sie optional die MFI- und Ausstiegsschwellen an, um sie an unterschiedliche Volatilitätsregime anzupassen.
3. Geben Sie `StopLossPoints`/`TakeProfitPoints` ungleich Null an, wenn der Broker explizite Schutzanweisungen verlangt; andernfalls belassen Sie sie bei Null, um ohne harte Ziele zu handeln.
4. Überwachen Sie die von der Strategie erstellten Diagrammbereiche, um Kerzen, den MFI-Indikator und ausgeführte Trades zu visualisieren.
