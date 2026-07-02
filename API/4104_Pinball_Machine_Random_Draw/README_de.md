# Flipperautomat-Zufallsziehungsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine direkte StockSharp-Umsetzung des MetaTrader 4-Expertenberaters `Pinball_machine.mq4`. Der ursprüngliche Roboter zeichnete
Bei jedem eingehenden Tick wurden zufällige ganze Zahlen eingegeben und eine Market-Order eröffnet, wenn zwei dieser Ziehungen übereinstimmten. Die StockSharp-Version
behält das gleiche Verhalten im Lotteriestil bei: Bei jeder fertigen Kerze des ausgewählten Zeitrahmens führt der Algorithmus zwei Paare aus
Zufallsziehungen und Einstieg in eine Long- oder Short-Marktposition, wenn das entsprechende Paar gleiche Werte enthält. Stop-Loss und Take-Profit
Die Abstände werden bei jeder Auswertung ebenfalls zufällig ausgewählt, wodurch das Gefühl der ursprünglichen „Flipper“-Routine reproduziert wird, bei der Trades ein- und aussteigen
unvorhersehbar ausgeht.

## Handelslogik
- Abonnieren Sie Kerzen, die durch den Parameter `CandleType` definiert sind, und warten Sie, bis sich die Balken vollständig gebildet haben.
- Generieren Sie für jede fertige Kerze vier gleichmäßig verteilte Ganzzahlen in `[0, RandomMaxValue]`. Das erste Paar gehört zum
potenzieller Long-Einstieg, das zweite Paar gehört zum potenziellen Short-Einstieg.
- Zeichnen Sie zwei zusätzliche Ganzzahlen zwischen `MinStopLossPoints`/`MaxStopLossPoints` und `MinTakeProfitPoints`/`MaxTakeProfitPoints`
Bestimmen Sie die Schutzabstände (ausgedrückt in Preisschritten), die beiden Seiten der Bewertung gemeinsam sind.
- Wenn die erste und die zweite zufällige Ganzzahl übereinstimmen, senden Sie eine Marktkauforder mit dem Volumen `TradeVolume`. Wenn der dritte und vierte
Wenn die Werte übereinstimmen, erteilen Sie einen Marktverkaufsauftrag mit dem gleichen Volumen. Beide Bedingungen können innerhalb derselben Kerze ausgelöst werden, genau wie in
die MQL-Version, in der Kauf- und Verkaufsaufträge unabhängige Ereignisse waren.
- Fügen Sie sofort eine Stop-Loss- und eine Take-Profit-Order hinzu (wenn die gezogene Distanz größer als Null ist). Die Abstände werden interpretiert
als Vielfaches des `PriceStep` des Instruments, was den in MetaTrader verwendeten Multiplikator `Point` widerspiegelt.

## Auftragsverwaltung und Risikokontrolle
- `StartProtection()` wird aufgerufen, wenn die Strategie startet, damit StockSharp Schutzanordnungen im Namen der Strategie verwaltet.
- Jeder Eintrag misst die resultierende Position (`Position ± TradeVolume`) und übergibt sie an `SetStopLoss` und `SetTakeProfit`, die
ermöglicht es der Plattform, Schutzaufträge zu konsolidieren, selbst wenn mehrere Geschäfte gleichzeitig ausgeführt werden.
- Wenn entweder der minimale oder der maximale Abstandsparameter auf Null oder eine negative Zahl eingestellt sind, gilt der entsprechende Schutz
für diesen Zyklus übersprungen.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `TradeVolume` | Auftragsgröße in Losen/Verträgen, die für jeden zufälligen Eintrag eingereicht werden. |
| `CandleType` | Zeitrahmen der Kerzen, die die Zufallsziehungen auslösen. Kürzere Zeiträume emulieren das ursprüngliche Tick-basierte EA genauer. |
| `RandomMaxValue` | Inklusive Obergrenze für die ganzzahligen Ziehungen. Ein größerer Wert verringert die Wahrscheinlichkeit übereinstimmender Zahlen und verringert daher die Handelshäufigkeit. |
| `MinStopLossPoints` | Untergrenze (in Preisschritten) für die zufällig generierte Stop-Loss-Distanz. |
| `MaxStopLossPoints` | Obergrenze (in Preisschritten) für die Stop-Loss-Distanz. |
| `MinTakeProfitPoints` | Untergrenze (in Preisschritten) für die zufällig generierte Take-Profit-Distanz. |
| `MaxTakeProfitPoints` | Obergrenze (in Preisschritten) für die Take-Profit-Distanz. |
| `RandomSeed` | Startwert des Pseudozufallszahlengenerators. Null hält das Verhalten zeitbasiert, jeder andere Wert macht die Sequenz reproduzierbar. |

## Hinweise zur Implementierung
- Das MetaTrader-Skript war tickgesteuert; Der Port StockSharp verwendet Kerzenvervollständigungen, da der Port API auf hoher Ebene mit Zeitreihenereignissen arbeitet. Durch das Setzen eines sehr kurzen `CandleType` (z. B. eine Sekunde oder Tick-Kerzen) wird die Schnelligkeit des Originals wiederhergestellt.
- Stop-Loss- und Take-Profit-Werte werden einmal pro Auswertung generiert und sowohl für den Long- als auch den Short-Zweig wiederverwendet, genau wie in der Quelle EA.
- Stellen Sie sicher, dass das gehandelte Instrument einen gültigen `PriceStep` aufweist, andernfalls müssen die in Punkten ausgedrückten Schutzabstände möglicherweise manuell angepasst werden.
