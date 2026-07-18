# PricerEA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **PricerEA-Strategie** bildet das Verhalten des MetaTrader 4-Experten „PricerEA v1.0“ unter Verwendung des StockSharp-High-Level-API nach.
Es platziert bis zu vier ausstehende Aufträge (Kaufstopp, Verkaufsstopp, Kauflimit und Verkaufslimit) zu manuell definierten Preisniveaus. Einmal
Wenn die ausstehende Order ausgeführt wird, fügt die Strategie schützende Stop-Loss- und Take-Profit-Orders hinzu und ermöglicht optional einen Trailing Stop und
Break-Even-Anpassung, um dem ursprünglichen Expert Advisor zu folgen.

## Wie es funktioniert

1. **Ausstehende Aufträge** – die Strategie liest absolute Preisniveaus aus den Eingaben und übermittelt nur die entsprechenden ausstehenden Aufträge
einmal beim Start. Der optionale Ablauf kann in Minuten konfiguriert werden.
2. **Volumenauswahl** – Benutzer können die feste manuelle Losgröße beibehalten oder in den automatischen Modus wechseln, aus dem das Volumen abgeleitet wird
der Portfoliosaldo und das MT4-Risikofaktor-Analogon.
3. **Schutz** – nachdem eine Einstiegsorder ausgeführt wurde, erstellt die Strategie Stop-Loss- und Take-Profit-Orders im konfigurierten Abstand
(ausgedrückt in Preispunkten). Wenn sowohl Trailing als auch Break-Even aktiviert sind, folgt der Stopp den ursprünglichen MQL-Bedingungen: das ist er
Der Kurs bewegt sich erst, nachdem der Preis die Break-Even-Distanz plus den anfänglichen Stopp erreicht hat.
4. **Auftragspflege** – ausstehende Aufträge werden automatisch storniert, wenn ihre Gültigkeitsdauer abläuft oder die Strategie endet.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `BuyStopPrice`, `SellStopPrice`, `BuyLimitPrice`, `SellLimitPrice` | Absolute Preise für die entsprechenden ausstehenden Bestellungen. Ein Wert von `0` deaktiviert die Bestellung. |
| `TakeProfitPoints` | Abstand vom Einstiegspreis zur Take-Profit-Order, gemessen in Preispunkten (`Security.PriceStep`). |
| `StopLossPoints` | Abstand vom Einstiegspreis zur Stop-Loss-Order, ebenfalls gemessen in Preispunkten. |
| `EnableTrailingStop` | Aktiviert die Trailing-Stop-Logik. |
| `TrailingStepPoints` | Minimale Bewegung (in Punkten) erforderlich, bevor der Trailing Stop verschoben wird. |
| `EnableBreakEven` | Aktiviert die Break-Even-Regel, die den Stop nach ausreichendem Gewinn über/unter den Einstiegspunkt anhebt. |
| `BreakEvenTriggerPoints` | Zusätzlicher Gewinn (Punkte) erforderlich, bevor der Stop für die Gewinnschwelle verschoben wird. |
| `PendingExpiryMinutes` | Lebensdauer der ausstehenden Bestellungen in Minuten. `0` hält sie am Leben, bis sie gefüllt oder manuell abgebrochen werden. |
| `VolumeMode` | Wählt zwischen manueller Lautstärke und automatischer Größenanpassung. |
| `RiskFactor` | Von der automatischen Größenanpassung verwendeter Risikomultiplikator (spiegelt die Eingabe MQL wider). |
| `ManualVolume` | Feste Losgröße, die verwendet wird, wenn `VolumeMode` auf `Manual` gesetzt ist. |

## Unterschiede zur MT4-Version

- Die automatische Volumenberechnung verwendet den Portfoliosaldo StockSharp und den Wertpapiervertragsmultiplikator. Verschiedene Makler
kann unterschiedliche Formeln verwenden, daher kann der resultierende Wert geringfügig von MetaTrader abweichen.
- Schutzaufträge werden über StockSharp-Helfer erteilt und respektieren die Lautstärkestufe sowie die Mindest- und Höchstlautstärke des Veranstaltungsortes.
- Der Ablauf wird innerhalb der Strategie implementiert (MetaTrader basiert auf dem serverseitigen Ablauf der Bestellung).

## Nutzungshinweise

- Konfigurieren Sie die Preisniveaus, bevor Sie mit der Strategie beginnen. Bei Werten gleich Null ist die entsprechende Bestellung deaktiviert.
- Um die MT4-Logik „Ziffern“ zu imitieren, arbeiten die punktbasierten Parameter in `Security.PriceStep`-Einheiten.
- Kombinieren Sie die Strategie mit den Portfolio- und Protokollierungstools von StockSharp, um ausstehende Aufträge und Schutzstopps zu überwachen.
