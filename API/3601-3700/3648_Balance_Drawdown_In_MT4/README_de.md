# Balance Drawdown in der MT4-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den ursprünglichen MetaTrader 4 Expert Advisor **BalanceDrawdownInMT4** auf den StockSharp High-Level API. Der EA eröffnet sofort eine einzelne Long-Position und misst kontinuierlich den Kontoverlust im Verhältnis zum seit Beginn der Sitzung erreichten Spitzensaldo.

## Handelslogik

1. Wenn die Strategie startet, ruft sie `StartProtection` auf, um verwaltete Stop-Loss- und Take-Profit-Levels zu aktivieren, die die in Preispunkten ausgedrückten MQL-Eingaben nachahmen.
2. Bei der ersten abgeschlossenen Kerze (Standardzeitrahmen: 1 Minute) prüft die Strategie, ob eine Position offen ist. Wenn kein Risiko vorhanden ist, wird eine Market-Buy-Order mit dem konfigurierten `Volume` übermittelt.
3. Nach jeder fertigen Kerze wird die Drawdown-Metrik aktualisiert:
   - Die Strategie verfolgt den maximal erreichten Saldo als **StartBalance + realisierter PnL**.
   - Das aktuelle Eigenkapital entspricht **StartBalance + realisierter PnL + nicht realisierter PnL**, wobei der nicht realisierte PnL aus dem letzten Schlusskurs der Kerze, dem durchschnittlichen Einstiegspreis und dem `PriceStep`/`StepPrice` des Instruments abgeleitet wird.
   - Drawdown ist der prozentuale Rückgang vom gespeicherten Spitzensaldo zum aktuellen Eigenkapital. Der Wert wird bei jeder Aktualisierung mit einer Informationsmeldung protokolliert.

Der Algorithmus eröffnet niemals zusätzliche Positionen oder Umkehrungen. Sobald die Ausgangsposition festgelegt ist, bleibt sie aktiv, bis sie gestoppt wird, der Take-Profit ausgelöst wird oder der Benutzer manuell eingreift.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `StartBalance` | `1000` | Basissaldo, der bei der Berechnung des Spitzenkapitals und des Drawdown-Prozentsatzes verwendet wird. |
| `Volume` | `0.01` | Nettovolumen (in Instrumenteneinheiten) der ursprünglichen Marktkauforder. |
| `StopLossPoints` | `300` | Abstand vom Einstiegspreis bis zum Schutzstopp, gemessen in Preispunkten. Ein Wert von `0` deaktiviert den Stopp. |
| `TakeProfitPoints` | `400` | Abstand vom Einstiegspreis zum Schutzziel, gemessen in Preispunkten. Ein Wert von `0` deaktiviert das Ziel. |
| `CandleType` | `1m` Zeitrahmen | Zeitrahmen, der regelmäßige Inanspruchnahmeaktualisierungen und die anfängliche Eingangsprüfung steuert. |

## Implementierungshinweise

- Der Drawdown-Zähler verwendet den realisierten PnL (`PnL`) der Strategie in Kombination mit dem aus Preisunterschieden geschätzten nicht realisierten PnL, was der laufenden Bilanzlogik der MT4-Version entspricht.
- Wenn `PriceStep` oder `StepPrice` für das Wertpapier nicht verfügbar ist, gibt die nicht realisierte PnL-Berechnung sicher Null zurück und verhindert so Fehler bei der Division durch Null.
- `Volume` wird validiert, um vor dem ersten Handel einen positiven Wert sicherzustellen; Andernfalls wird eine Warnung protokolliert und die Strategie bleibt unverändert.
- `DrawdownPercent` stellt den neuesten Drawdown-Wert zur Verfügung, sodass andere Module (Dashboards, Risikocontroller) den Wert programmgesteuert abrufen können.

## Nutzungstipps

- Setzen Sie `StartBalance` auf den tatsächlichen Kontostand (oder den Stand zu Beginn der Handelssitzung), um aussagekräftige Drawdown-Statistiken zu erhalten.
- Behalten Sie die standardmäßigen 1-Minuten-Kerzen für zeitnahe Aktualisierungen bei oder wählen Sie einen schnelleren synthetischen Kerzentyp, wenn Sie nahezu Tick-Präzision benötigen.
- Da diese Strategie absichtlich eine einzelne Long-Position hält, kombinieren Sie sie mit manuellen Risikokontrollen oder externer Automatisierung, wenn Sie nach dem Erreichen eines Stopps oder Ziels erneut einsteigen müssen.
- Testen Sie immer auf einem Simulator, um sicherzustellen, dass der Broker `PriceStep` und `StepPrice` bereitstellt, sodass die nicht realisierte PnL-Konvertierung den Erwartungen entspricht.
