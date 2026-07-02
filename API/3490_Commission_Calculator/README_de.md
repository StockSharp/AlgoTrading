# Strategie für den Provisionsrechner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Provisionsrechner-Strategie** ist eine Dienstprogrammstrategie, die das ursprüngliche MetaTrader-Skript widerspiegelt. Es sendet eine einzelne diskretionäre Order unter Verwendung des ausgewählten Ausführungsmodus (Markt, Limit oder Stop) und misst die Maklerprovision, die auf jede resultierende Ausführung angewendet wird. Die Strategie speichert die kumulierte Provision und druckt am Ende einen Abschlussbericht mit dem Startsaldo, den Gesamtgebühren und dem gebührenbereinigten Saldo aus.

Im Gegensatz zu herkömmlichen signalgesteuerten Strategien sind keine Marktdaten oder Indikatoren erforderlich. Die Strategie konzentriert sich auf die automatisierte Gebührenabrechnung für manuelle oder halbmanuelle Ausführungen.

## Handelslogik
1. Wenn die Strategie startet, erfasst sie den anfänglichen Portfoliosaldo und konfiguriert das Standardhandelsvolumen.
2. Optionale schützende Stop-Loss- und Take-Profit-Level werden bis `StartProtection` aktiviert, wenn sowohl der Einstiegspreis als auch die Zielpreise gültig sind. Die Entfernungen werden in absoluten Preiseinheiten berechnet und ahmen die MQL-Implementierung nach.
3. Der konfigurierte Bestellmodus wird genau einmal ausgeführt. Wenn Parameter inkonsistent sind (z. B. fehlender Einstiegspreis für Limit-Orders), protokolliert die Strategie das Problem und überspringt das Senden der Order.
4. Jeder über `OnNewMyTrade` eingegangene eigene Trade wird verarbeitet, um die Provisionsgebühr anhand des konfigurierten Prozentsatzes zu berechnen.
5. Die Strategie fasst alle Provisionen zusammen, merkt sich die letzte Gebühr und protokolliert bei Stopp eine detaillierte Zusammenfassung.

Die Implementierung geht davon aus, dass die Maklergebühr proportional zu `price × volume × commissionRate / 100` ist. Passen Sie die Rate an den zu modellierenden Veranstaltungsort an.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `Quantity` | `0.001` | Von Hilfsmethoden gesendetes Handelsvolumen (`BuyMarket`, `SellLimit` usw.). |
| `EntryPrice` | `31365` | Preis, der für Limit- oder Stop-Orders und zur Berechnung von Schutzabständen verwendet wird. |
| `StopLossPrice` | `31200` | Preis, der die Stop-Loss-Distanz definiert. Ein nicht positiver Abstand deaktiviert den Stop-Loss-Schutz. |
| `TakeProfitPrice` | `32100` | Preis, der die Take-Profit-Distanz definiert. Ein nicht positiver Abstand deaktiviert den Take-Profit-Schutz. |
| `CommissionRate` | `0.04` | Provisionssatz, ausgedrückt als Prozentsatz des gehandelten Nominalwerts. |
| `Mode` | `None` | Auftragstyp, der ausgeführt werden soll, wenn die Strategie startet. Optionen: `None`, `MarketBuy`, `MarketSell`, `BuyLimit`, `SellLimit`, `BuyStop`, `SellStop`. |

## Hinweise und Best Practices
- Beginnen Sie die Strategie mit einem Portfolio, das die manuelle Auftragserteilung unterstützt; Es sind keine Datenabonnements erforderlich.
- Stellen Sie sicher, dass das Maklerprovisionsmodell mit dem Parameter `CommissionRate` übereinstimmt, um eine Unter- oder Überschätzung der Gebühren zu vermeiden.
- Setzen Sie für ausstehende Orders `EntryPrice` auf einen gültigen Wert, bevor Sie die Strategie starten. andernfalls wird die Bestellung nicht übermittelt.
- Wenn Schutzstufen aktiviert sind, weist die Strategie den Konnektor an, bei Auslösung Marktaustritte zu nutzen, um das ursprüngliche MQL-Verhalten möglichst genau nachzuahmen.

## Ergebnisberichterstattung
Wenn `OnStopped` aufgerufen wird, protokolliert die Strategie Folgendes:
- Snapshot des anfänglichen Kontostands (aufgenommen, als die Strategie gestartet wurde).
- Aggregierte Maklergebühren für alle abgewickelten Geschäfte.
- Der Endsaldo wird durch Abzug der aufgelaufenen Gebühren angepasst.

Dadurch eignet sich die Strategie gut für schnelle Was-wäre-wenn-Analysen und zur Validierung von Maklerprovisionsplänen bei Backtests.
