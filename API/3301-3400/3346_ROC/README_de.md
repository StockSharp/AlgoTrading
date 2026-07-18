# ROC Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die ROC-Strategie ist ein StockSharp-Port des MetaTrader-Expertenberaters, der in `MQL/26938/ROC.mq4` gespeichert ist. Es arbeitet mit einem einzelnen Symbol und bewertet die Preisbewegung mithilfe einer Kette linearer gewichteter gleitender Durchschnitte (LWMA), eines benutzerdefinierten Änderungsratenmodells (ROC), eines höheren Zeitrahmenmomentums und eines monatlichen MACD-Filters. Die ursprünglichen Geldmanagementfunktionen wie Break-Even, Pip-basierte Trailing-Stops, Aktienschutz und auf Geld lautende Gewinnziele bleiben erhalten.

## Eingabelogik
1. Die Strategie abonniert drei Datenströme:
   - Primäre Handelskerzen, die durch die Eigenschaft `CandleType` definiert werden.
   - Ein höherer Zeitrahmen für den 14-Perioden-Momentum-Oszillator (wird automatisch entsprechend dem Handelszeitrahmen ausgewählt).
   - Monatliche Kerzen für den Bestätigungsfilter MACD.
2. Bei jeder abgeschlossenen Handelskerze müssen die folgenden Bedingungen erfüllt sein, um eine Position zu eröffnen:
   - Das benutzerdefinierte ROC-Modell muss einen Aufwärtstrend (`Line4 < Line5`) für Käufe oder einen Abwärtstrend (`Line4 > Line5`) für Verkäufe melden.
   - Der anhand des typischen Preises berechnete schnelle LWMA muss bei Käufen über dem langsamen LWMA und bei Verkäufen darunter liegen.
   - Jeder der letzten drei Momentum-Messwerte aus dem höheren Zeitrahmen muss den konfigurierten Kauf- oder Verkaufsschwellenwert überschreiten (absolute Abweichung von 100).
   - Die monatliche MACD-Hauptlinie muss für Käufe über ihrer Signallinie und für Verkäufe darunter bleiben.
   - Die Positionsgröße respektiert das `MaxTrades`-Limit und skaliert optional das nächste Handelsvolumen nach aufeinanderfolgenden Verlusten, wenn `IncreaseFactor` größer als Null ist.

## Exit-Logik
- Klassische Stop-Loss- und Take-Profit-Orders werden in MetaTrader Punkten projiziert, sobald sich die Positionsgröße ändert.
- Der optionale Break-Even-Block verschiebt den Schutzstopp auf den Einstiegspreis zuzüglich des konfigurierten Offsets, sobald die Triggerdistanz in Punkten erreicht ist.
- Pip-basierte Trailing-Stops erhöhen den Stop-Wert bei jedem Kerzenschluss.
- Money-Management-Prüfungen schließen die Position, wenn ein Währungsziel oder ein Prozentziel erreicht ist, und können schwankende Gewinne verfolgen, indem sie Pullbacks erkennen, die größer als `StopLossMoney` sind, nachdem der Gewinn `TakeProfitMoney` übersteigt.
- Ein Aktienstopp vergleicht den gleitenden Drawdown mit dem höchsten aufgezeichneten Eigenkapital und liquidiert die Position, wenn der zulässige Prozentsatz überschritten wird.
- Wenn Sie `ExitStrategy` auf `true` setzen, wird die Notausgangsroutine ausgeführt und die aktuelle Position zum Markt geschlossen.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `LotSize` | Das bei jedem Signal eröffnete Basishandelsvolumen. |
| `IncreaseFactor` | Berechnet das nächste Volumen nach aufeinanderfolgenden Verlustgeschäften neu. |
| `FastMaPeriod` / `SlowMaPeriod` | Länge der LWMA-Trendfilter. |
| `PeriodMa0`, `PeriodMa1`, `BarsV`, `AverBars`, `KCoefficient` | Definieren Sie das benutzerdefinierte ROC-Trendmodell. |
| `MomentumBuyThreshold`, `MomentumSellThreshold` | Minimale absolute Abweichung von 100, die vom Impulsfilter für höhere Zeitrahmen verwendet wird. |
| `StopLossSteps`, `TakeProfitSteps` | Anfängliche Schutzabstände, ausgedrückt in MetaTrader Punkten. |
| `TrailingStopSteps` | Pip-basierter Trailing-Stop. |
| `UseBreakEven`, `BreakEvenTriggerSteps`, `BreakEvenOffsetSteps` | Konfigurieren Sie das Break-Even-Modul. |
| `UseTpInMoney`, `TpInMoney`, `UseTpInPercent`, `TpInPercent` | Geld- und prozentuale Take-Profit-Ziele. |
| `EnableMoneyTrailing`, `TakeProfitMoney`, `StopLossMoney` | Parameter des Money-Trailing-Moduls. |
| `UseEquityStop`, `TotalEquityRisk` | Einstellungen zum Eigenkapitalschutz. |
| `MaxTrades` | Maximale Anzahl von Scale-Ins pro Richtung. |
| `ExitStrategy` | Erzwingt bei Aktivierung eine sofortige flache Position. |

## Notizen
- Der höhere Zeitrahmen für den Momentum-Indikator wird automatisch aus dem Handelszeitrahmen abgeleitet, um mit der ursprünglichen Switch-Anweisung im MetaTrader-Code übereinzustimmen.
- Alle Indikatorberechnungen verwenden die hohe Ebene `Bind` API, daher sind keine manuellen Datenanfragen erforderlich.
- Bei der Strategie handelt es sich nur um eine Netting-Strategie: Wenn beim Halten von Short-Positionen ein neues Long-Signal auftritt, wird das Short-Engagement zuerst geschlossen, bevor Long-Positionen eingegangen werden. Dies spiegelt das Verhalten des ursprünglichen EA auf Nicht-Hedging-Konten wider.
