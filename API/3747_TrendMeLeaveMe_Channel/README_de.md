# TrendMeLeaveMe Ausstehende Kanalstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese StockSharp-Implementierung erstellt den ursprünglichen MetaTrader-Expertenberater „TrendMeLeaveMe“. Die Idee besteht darin, einem dynamischen Trendkanal manuell zu folgen und ausstehende Stop-Orders zu verwenden, um Ausbrüche zu erkennen, wenn der Preis die Trendlinie berührt. Da StockSharp nicht mit vom Benutzer gezeichneten Diagrammobjekten funktioniert, baut die Strategie die Kanalmitte automatisch mit einem linearen Regressionsindikator neu auf und reproduziert dann dieselbe Offset-Logik, die die MQL-Version auf die oberen und unteren Hilfslinien angewendet hat.

Der Ansatz ist sowohl für lange als auch für kurze Einträge konzipiert. Sobald eine Stop-Order ausgelöst wird, wird die Position sofort durch statische Stop-Loss- und Take-Profit-Orders geschützt, die die in EA konfigurierten Distanzen widerspiegeln. Ausstehende Aufträge werden ständig aktualisiert, sodass die Aktivierungsstufen den neuesten Wert der Regressionslinie verfolgen.

## Wie die Strategie funktioniert

1. Ein Kerzenabonnement treibt einen `LinearRegression`-Indikator an, der als mittlere Trendlinie fungiert.
2. Der Benutzer definiert vier Offsets (oben/unten für Kauf- und Verkaufsszenarien) in Instrumentenpreisschritten. Die Strategie übersetzt sie in Preise oberhalb oder unterhalb der Regressionslinie.
3. Wenn die letzte Kerze zwischen der Trendlinie und dem konfigurierten unteren Offset schließt, wird ein Kaufstopp am oberen Offset positioniert. Wenn der Preis zwischen der Linie und dem oberen Offset schließt, wird symmetrisch ein Verkaufsstopp an der unteren Grenze platziert.
4. Wenn der Markt diese Aktivierungszonen verlässt, wird die entsprechende ausstehende Order storniert, damit die Strategie das Buch nicht überfüllt.
5. Nachdem eine Stop-Order ausgeführt wurde, wird der Handel mit einem statischen Stop-Loss und Take-Profit umschlossen, die die gleichen Punktabstände wie der ursprüngliche Expert Advisor verwenden.

## Signale

- **Kauf-Setup**: Der Kerzenschluss liegt unter oder auf der Regressionslinie, aber immer noch über dem unteren Kauf-Offset. Eine Kauf-Stopp-Order wird am oberen Offset platziert und folgt der Linie, solange die Bedingung gültig bleibt.
- **Verkaufskonfiguration**: Der Kerzenschluss liegt über oder auf der Regressionslinie, aber immer noch unter dem oberen Verkaufsoffset. Eine Verkaufsstopp-Order wird am unteren Offset platziert und folgt der Trendlinie.
- **Keine Einrichtung**: Wenn der Preis außerhalb des Aktivierungskorridors liegt, werden bestehende ausstehende Aufträge entfernt.

## Risikomanagement

- Kaufgeschäfte verwenden `BuyStopLossSteps` und `BuyTakeProfitSteps`, um feste Stop-Loss- und Take-Profit-Werte aus dem Einstiegspreis zu berechnen.
- Verkaufsgeschäfte verwenden `SellStopLossSteps` und `SellTakeProfitSteps` für denselben Zweck.
- Schutzaufträge werden nur dann neu berechnet, wenn sich die Nettoposition ändert, und ahmen nach, wie MetaTrader Stop-Levels direkt an jeden ausstehenden Auftrag anhängt.

## Parameter

- `CandleType` – Kerzenaggregation zur Berechnung der Trendlinie.
- `TrendLength` – Anzahl der Kerzen im linearen Regressionsfenster.
- `BuyStepUpper` / `BuyStepLower` – Offsets (in Preisschritten), die den oberen Auslöser und den unteren Aktivierungsschwellenwert für lange Setups definieren.
- `SellStepUpper` / `SellStepLower` – Offsets (in Preisschritten), die den Aktivierungskorridor für kurze Setups definieren.
- `BuyTakeProfitSteps` / `BuyStopLossSteps` – Entfernungen für Long-Positionsausstiege, ausgedrückt in Preisschritten.
- `SellTakeProfitSteps` / `SellStopLossSteps` – Distanzen für kurze Positionsausstiege.
- `BuyVolume` / `SellVolume` – Volumen, das für ausstehende Orders auf jeder Seite verwendet wird.

## Notizen

- Da es keine manuellen Trendlinien gibt, ersetzt der Regressionsindikator die Diagrammobjekte aus der Strategie MQL. Benutzer können mit der Regressionslänge experimentieren, um ihre manuelle Trendanalyse anzunähern.
- Die Strategie handelt nur, wenn die Börsenverbindung aktiv ist (`IsFormedAndOnlineAndAllowTrading`).
- Ausstehende Aufträge werden automatisch storniert, wenn bereits eine Position in die gleiche Richtung besteht, wodurch das Einzelauftragsverhalten des ursprünglichen EA reproduziert wird.
