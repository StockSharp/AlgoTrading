# Terminator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Terminator-Strategie reproduziert die gitterbasierte Martingal-Logik des MetaTrader 4-Expertenberaters „Terminator v2.0“ unter Verwendung des StockSharp-High-Levels API. Die Strategie beginnt in Richtung der MACD-Steigung und bildet dann einen Durchschnittskorb, wenn sich der Preis um eine konfigurierbare Anzahl von Pips gegen die Position bewegt. Der Korb wird mit optionalem Stop-Loss, Take-Profit, Trailing-Stop und einer Secure-Profit-Schutzregel verwaltet, die den letzten Trade schließen kann, wenn der variable Gewinn ein Ziel erreicht.

## Handelslogik

1. **Signalgenerierung** – Bei jeder fertigen Kerze wertet die Strategie das MACD-Histogramm aus. Wenn der MACD-Wert im Vergleich zum vorherigen Wert steigt, wird von einer bullischen Tendenz ausgegangen, während ein abnehmender MACD auf eine bärische Tendenz hindeutet. Ein `ReverseSignals`-Flag kann die Interpretation umkehren.
2. **Ersteingabe** – Wenn keine offenen Trades vorhanden sind und der Zeitplanfilter (`StartYear`, `StartMonth`, `EndYear`, `EndMonth`) den Handel zulässt, übermittelt die Strategie eine Marktorder in der erkannten Richtung, es sei denn, `ManualTrading` ist aktiviert.
3. **Martingale-Durchschnittsbildung** – Wenn ein offener Korb vorhanden ist, wartet die Strategie darauf, dass sich der Preis um `EntryDistancePips` nachteilig bewegt. Jeder zusätzliche Eintrag verdoppelt das vorherige Volumen (oder multipliziert es mit 1,5, wenn `MaxTrades` größer als 12 ist), bis zum Limit von `MaxTrades`. Durch die Aktivierung von `UseMoneyManagement` kann die Positionsgröße auch vom Kontostand abgeleitet werden.
4. **Risikomanagement** –
   - **Take-Profit**: `TakeProfitPips` definiert den Abstand, der zur Positionierung des gemeinsamen Take-Profit-Levels verwendet wird.
   - **Anfangsstopp**: `InitialStopPips` legt optional den anfänglichen Schutzstopp für den gesamten Korb fest.
   - **Trailing Stop**: `TrailingStopPips` wird aktiviert, nachdem der Korb mindestens die Trailing-Distanz plus einen Abstandsschritt erreicht hat, und verschiebt dann den Stop in die Handelsrichtung.
   - **Kontoschutz**: Wenn `UseAccountProtection` aktiviert ist und die Anzahl der offenen Trades `MaxTrades - OrdersToProtect` erreicht, wird der variable Gewinn mit `SecureProfit` verglichen (oder dem aktuellen Portfoliowert, wenn `ProtectUsingBalance` wahr ist). Wenn der Schwellenwert überschritten wird, wird der letzte Trade geschlossen, um Gewinne zu sichern, und es sind keine neuen Einträge zulässig, bis der Korb zurückgesetzt wird.
5. **Warenkorb-Reset** – Wenn die Nettoposition auf Null zurückkehrt, werden alle internen Zähler gelöscht, was einen neuen Handelszyklus ermöglicht.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `TakeProfitPips` | Abstand in Pips für das Korb-Take-Profit-Level. |
| `InitialStopPips` | Anfängliche Stoppdistanz in Pips. Zum Deaktivieren auf Null setzen. |
| `TrailingStopPips` | Trailing-Stop-Distanz in Pips. Zum Deaktivieren auf Null setzen. |
| `MaxTrades` | Maximale Anzahl gleichzeitig erlaubter Martingaleinträge. |
| `EntryDistancePips` | Mindestens erforderliche negative Bewegung, bevor der nächste Trade hinzugefügt wird. |
| `SecureProfit` | Vom Schutzmodul verwendete variable Gewinnschwelle. |
| `UseAccountProtection` | Aktiviert den Secure-Profit-Schutzblock. |
| `ProtectUsingBalance` | Bei „true“ entspricht der Schutzschwellenwert dem aktuellen Portfoliowert und nicht `SecureProfit`. |
| `OrdersToProtect` | Anzahl der letzten Trades, die vom Schutzblock überwacht werden (spiegelt die ursprüngliche Eingabe „Orders to Protect“ wider). |
| `ReverseSignals` | Kehrt bullische und bärische MACD-Signale um. |
| `ManualTrading` | Deaktiviert automatische Eingaben, während die Warenkorbverwaltung aktiv bleibt. |
| `LotSize` | Feste Losgröße, wenn die Geldverwaltung deaktiviert ist. |
| `UseMoneyManagement` | Ermöglicht eine ausbalancierte Größenanpassung, abgeleitet von `RiskPercent`. |
| `RiskPercent` | Risikoprozentsatz (pro 100 %), der angewendet wird, wenn das Geldmanagement aktiv ist. |
| `IsStandardAccount` | Schaltet zwischen Standard- und Mini-Los-Skalierung um. |
| `EurUsdPipValue`, `GbpUsdPipValue`, `UsdChfPipValue`, `UsdJpyPipValue`, `DefaultPipValue` | Pip-Wertannahmen, die zur Umrechnung von Pips in die Währung für die Schutzregel verwendet werden. |
| `StartYear`, `StartMonth`, `EndYear`, `EndMonth` | Beschränken Sie das Zeitfenster, in dem neue Körbe geöffnet werden können. |
| `CandleType` | Zeitrahmen, der zum Aufbau des MACD-Signals verwendet wird. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | Periodeneinstellungen des Indikators MACD. |

## Nutzungshinweise

- Die Strategie abonniert den durch `CandleType` definierten Kerzentyp und reagiert nur auf fertige Kerzen.
- Um das ursprüngliche MT4-Verhalten widerzuspiegeln, stellen Sie sicher, dass die Parameter des Symbol-Pip-Werts mit den Spezifikationen Ihres Brokers übereinstimmen.
- Wenn `ManualTrading` aktiviert ist, können Sie Bestellungen weiterhin manuell verwalten; Der Algorithmus setzt weiterhin Trailing-Stops ein und erzwingt den Kontoschutz für den offenen Warenkorb.
- Die Implementierung konzentriert sich auf die MACD-basierte Eingabemethode des ursprünglichen Expert Advisors, da die anderen Modi auf benutzerdefinierten Indikatoren beruhten, die in StockSharp nicht verfügbar sind.

## Konvertierungsdetails

- Geldmanagement, Pip-Abstand, Martingal-Skalierung und sichere Gewinnlogik folgen der ursprünglichen MQ4-Codestruktur.
- Die MT4-Optionen `AccountProtection` und `AllSymbolsProtect` werden in den Parametern `UseAccountProtection` und `ProtectUsingBalance` kombiniert.
- `ReverseCondition`- und `Manual`-Flags aus der Quellzuordnung zu `ReverseSignals` bzw. `ManualTrading`.
- Stop-Loss- und Trailing-Regeln gelten für den Gesamtkorb und nicht pro Order, ähnlich dem Verhalten des Quell-Expertenberaters.

## Wie man läuft

1. Öffnen Sie die Lösung in Visual Studio.
2. Fügen Sie die Strategie einer `StrategyRunner`- oder `StrategyConnector`-Instanz hinzu.
3. Konfigurieren Sie die Parameter in der Benutzeroberfläche oder per Code.
4. Starten Sie die Strategie; Es abonniert automatisch die angegebene Kerzenserie und beginnt mit der Auswertung der Signale.
