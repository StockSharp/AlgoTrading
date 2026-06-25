# Exp XWAMI MMRec (ID 2956)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung

Die Strategie repliziert den MetaTrader-Expertenberater **Exp_XWAMI_MMRec** und kombiniert den benutzerdefinierten XWAMI-Momentum-Indikator mit einem Geldmanagement-"Zähler". Momentum wird als Differenz zwischen dem aktuellen Preis und dem Preis vor `Period` Balken gemessen. Diese Differenz wird durch vier konfigurierbare Glättungsstufen geleitet; die dritte und vierte Stufe bilden die `Up`- und `Down`-Puffer des ursprünglichen Indikators. Kreuzungen zwischen den beiden Puffern treiben Positionsumkehrungen an.

Jede Stufe kann mehrere Glättungsalgorithmen emulieren: einfache/exponentielle/geglättete/linear gewichtete gleitende Durchschnitte, Jurik JJMA/JurX, Tillson T3, VIDYA (mit EMA angenähert) und Kaufmans AMA. Die Strategie arbeitet mit einer einzigen aggregierten Position und unterstützt sowohl Long- als auch Short-Trades. Das Risiko wird nach aufeinanderfolgenden Verlusten reduziert, indem aktuelle Trade-Ergebnisse gegen die `BuyTotalTrigger`/`SellTotalTrigger`-Fenster verglichen und Verluste relativ zu `BuyLossTrigger`/`SellLossTrigger` gezählt werden.

Schutzende Stops folgen der MetaTrader-Implementierung: `StopLossPoints` und `TakeProfitPoints` werden in Symbol-Punkten (`Security.PriceStep`) gemessen. Wenn ein Stop oder Ziel innerhalb des Signal-Zeitrahmens berührt wird, wird die Position sofort geschlossen und das Trade-Ergebnis in die Geldmanagement-Historie aufgenommen.

## Parameter

| StockSharp-Eigenschaft | Standard | Originaleingang | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | H1-Zeitrahmen | `InpInd_Timeframe` | Zeitrahmen für den Kerzenaufbau des Indikators. |
| `Period` | 1 | `iPeriod` | Abstand (in Balken) zwischen aktuellem Preis und Vergleichspreis bei der Momentum-Berechnung. |
| `Method1` / `Length1` / `Phase1` | `T3`, `4`, `15` | `XMethod1`, `XLength1`, `XPhase1` | Glättungsmethode, Länge und Phase für Stufe 1. Phase wird nur von Jurik/JurX/T3 verwendet. |
| `Method2` / `Length2` / `Phase2` | `Jjma`, `13`, `15` | `XMethod2`, `XLength2`, `XPhase2` | Einstellungen für die zweite Glättungsstufe. |
| `Method3` / `Length3` / `Phase3` | `Jjma`, `13`, `15` | `XMethod3`, `XLength3`, `XPhase3` | Einstellungen für die dritte Glättungsstufe (Indikator-`Up`-Puffer). |
| `Method4` / `Length4` / `Phase4` | `Jjma`, `4`, `15` | `XMethod4`, `XLength4`, `XPhase4` | Einstellungen für die vierte Glättungsstufe (Indikator-`Down`-Puffer). |
| `AppliedPrice` | `Close` | `IPC` | Preisquelle für die Momentum-Berechnung. Alle MetaTrader-Preisoptionen werden reproduziert, einschließlich beider TrendFollow-Varianten und des Demark-Preises. |
| `SignalBar` | 1 | `SignalBar` | Index der historischen Kerze zur Kreuzungsauswertung (`0` = aktuell fertiger Balken). |
| `AllowBuyOpen` / `AllowSellOpen` | `true` | `BuyPosOpen`, `SellPosOpen` | Aktiviert Long- bzw. Short-Einstiege. |
| `AllowBuyClose` / `AllowSellClose` | `true` | `BuyPosClose`, `SellPosClose` | Aktiviert erzwungene Ausstiege wenn das entgegengesetzte Signal erscheint. |
| `NormalVolume` | `0.1` | `MM` | Standard-Lot/Volumengröße nach profitablen oder neutralen Serien. |
| `ReducedVolume` | `0.01` | `SmallMM_` | Reduziertes Lot nach zu vielen Verlusten. |
| `BuyTotalTrigger` / `BuyLossTrigger` | `5` / `3` | `BuyTotalMMTriger`, `BuyLossMMTriger` | Anzahl der untersuchten letzten Long-Trades und maximale Verluste im Fenster vor der Volumenreduzierung. |
| `SellTotalTrigger` / `SellLossTrigger` | `5` / `3` | `SellTotalMMTriger`, `SellLossMMTriger` | Gleiche Logik für Short-Positionen. |
| `StopLossPoints` | `1000` | `StopLoss_` | Stop-Loss-Abstand in Punkten. |
| `TakeProfitPoints` | `2000` | `TakeProfit_` | Take-Profit-Abstand in Punkten. |

## Verhalten

1. Anmelden für die gewünschte Kerzen-Serie und nur fertige Kerzen auswerten.
2. Preisdifferenz berechnen (`AppliedPrice` jetzt vs. vor `Period` Balken). Wenn genug Historie verfügbar ist, die Differenz durch die vier Glättungsstufen leiten.
3. Die dritte (`Up`) und vierte (`Down`) Stufen-Ausgaben speichern. Wenn `Up` und `Down` bei `SignalBar + 1` (vorheriger Balken) kreuzen, wechselt die Strategie die Richtung. Bei `Up > Down` werden Short-Positionen geschlossen und eine Long-Position eröffnet wenn `Up <= Down` auf dem Signal-Balken. Die entgegengesetzte Logik behandelt bearishe Signale.
4. Die Positionsgröße wird vom Zähler ausgewählt: Die letzten `BuyTotalTrigger` (oder `SellTotalTrigger`) Trade-Gewinne werden geprüft. Wenn mindestens `BuyLossTrigger` (oder `SellLossTrigger`) davon negativ sind, verwendet der nächste Trade `ReducedVolume`; andernfalls `NormalVolume`.
5. Bei einer Long-Position werden Stop-Loss- und Take-Profit-Abstände durch Multiplikation mit `Security.PriceStep` von Punkten in Preis umgerechnet. Bei Verletzung wird die Position zum Stop/Ziel-Preis geschlossen und der Trade für das Geldmanagement-Modul aufgezeichnet. Short-Trades folgen den symmetrischen Regeln.

## Unterschiede zur MetaTrader-Version

- StockSharp aggregiert Positionen, daher sind `BuyMagic`/`SellMagic`, die MetaTrader-Globalvariablen-Buchhaltung und die `MarginMode`-Option unnötig und wurden weggelassen.
- Tillson T3 ist explizit implementiert; Jurik JJMA und JurX werden beide auf `JurikMovingAverage` mit der angegebenen Phase abgebildet. VIDYA und ParMA werden mit einem exponentiellen gleitenden Durchschnitt angenähert, da StockSharp keine nativen Äquivalente hat.
- Orders werden mit `BuyMarket`/`SellMarket` ausgeführt und Stops/Ziele durch Überwachung von Kerzen-Hochs/Tiefs erzwungen, nicht durch native MT5-Stop-Orders.
- Die Abweichungs-/Slippage-Eingabe ist bei StockSharp-Ausführungsmodellen nicht erforderlich und wurde entfernt.

## Verwendungshinweise

1. Instrument wählen und `CandleType` auf den vom ursprünglichen Experten verwendeten Zeitrahmen setzen.
2. Glättungsmethoden und Längen konfigurieren, um der MetaTrader-Indikator-Einstellung zu entsprechen.
3. `NormalVolume`, `ReducedVolume` und die Auslöseschwellen an die gewünschte Risikopolitik anpassen.
4. Strategie einem Portfolio anhängen und starten; Handel ist vollautomatisch und kehrt bei jedem Indikator-Kreuzung um.

Für weitere Anpassungen können Sie die Glättungs-Mappings in `ExpXwamiMmRecStrategy.CreateFilter` bearbeiten, um alternative StockSharp-Indikatoren anzuschließen.
