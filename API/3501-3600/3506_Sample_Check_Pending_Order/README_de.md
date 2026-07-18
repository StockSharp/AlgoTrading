# Beispiel für eine Strategie zur Prüfung ausstehender Bestellungen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Sample Check Pending Order-Strategie stellt kontinuierlich sicher, dass genau eine Buy-Stop- und eine Sell-Stop-Order im Auftragsbuch verbleiben. Der ursprüngliche MetaTrader 5-Experte von Tungman überprüft, ob der Broker die angeforderte Losgröße akzeptiert, bestätigt, dass ausreichend freie Marge für beide Richtungen vorhanden ist, und übermittelt dann neue ausstehende Aufträge direkt zusätzlich zum aktuellen Geld-/Briefkurs mit einem Ablaufdatum von einem Tag. Diese Konvertierung reproduziert den gleichen Workflow unter Verwendung der übergeordneten Auftragsverwaltung API von StockSharp und der Angebote der Ebene 1.

## Handelslogik

1. **Marktdatenverarbeitung**
   - Die Strategie abonniert Aktualisierungen der Stufe 1 und speichert die neuesten besten Geld- und Briefkurse zwischen.
   - Die Handelslogik wird ausgesetzt, bis beide Seiten des Buchs bekannt sind und `IsFormedAndOnlineAndAllowTrading()` bestätigt, dass die Umgebung bereit ist (Strategie läuft, das Portfolio ist verbunden usw.).
2. **Volumenvalidierung**
   - Jeder eingehende Tick löst eine Validierung des konfigurierten `OrderVolume` gegen `Security.MinVolume`, `Security.MaxVolume` und `Security.VolumeStep` aus.
   - Die Prüfung spiegelt den MT5-Helfer wider: Das Volumen muss im zulässigen Bereich liegen und ein genaues Vielfaches des Schritts sein. Verstöße führen zu einem informativen Protokolleintrag und blockieren alle neuen Bestellungen.
3. **Margenvorprüfung**
   - Before submitting anything, the strategy estimates the margin required to place a long or short position of the configured size. Zur Berechnung der Anforderung werden das letzte Geld/Brief, der Instrumentenmultiplikator und der vom Benutzer bereitgestellte `AccountLeverage` verwendet.
   - Wenn der aktuelle oder anfängliche Portfoliowert für eine der beiden Richtungen nicht ausreicht, bricht der Algorithmus für diesen Tick ab und ahmt die `CheckMoneyForTrade`-Schutzmaßnahmen weitgehend nach.
4. **Ausstehende Auftragserteilung**
   - Wenn keine aktive Buy-Stop-Order vorhanden ist, wird eine neue zum aktuellen Briefkurs registriert (auf den nächsten Tick gerundet). Die gleiche Regelung gilt für den Verkaufsstopp beim aktuellen Gebot. Beide Bestellungen verwenden dasselbe Volumenvalidierungsergebnis.
   - Der Ablauf wird manuell erzwungen: Jede Bestellung speichert ihr Zeitlimit (`ExpirationMinutes`, standardmäßig ein Tag). Future ticks cancel the order if the deadline has passed and immediately clear the slot for a new pending order.
5. **Risikomanagement**
   - `StartProtection` weist einen absoluten Stop-Loss und Take-Profit basierend auf `StopLossPoints` und `TakeProfitPoints` auf. Sobald eine Bestellung ausgelöst wird, übermittelt StockSharp automatisch die Schutzausgänge in den konfigurierten Entfernungen und stellt die in der MT5-Version verwendeten SL/TP-Parameter wieder her.

Das Endergebnis ist eine minimalistische Breakout-Engine, die den Markt immer zwischen zwei Stop-Orders „einschließt“. Immer wenn eine Order ausgeführt wird, bleibt die andere Seite aktiv, während sich die Strategie darauf vorbereitet, den fehlenden Teil bei der nächsten Kursaktualisierung erneut auszugeben.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `OrderVolume` | Die mit jeder Stop-Order gesendete Losgröße. Die Limits und Volumenschritte des Brokers müssen eingehalten werden. |
| `StopLossPoints` | Distanz in Punkten, umgerechnet in Preiseinheiten für den Schutzstopp, sobald ein Trade eröffnet wird. |
| `TakeProfitPoints` | Abstand in Punkten, der für das nach einer Füllung erstellte Gewinnziel verwendet wird. |
| `ExpirationMinutes` | Lebensdauer jeder ausstehenden Bestellung. Nach Ablauf der Frist wird die Bestellung storniert und beim nächsten Tick neu erstellt. |
| `AccountLeverage` | Estimated account leverage used to approximate margin requirements before each submission. |

Alle Entfernungen werden mit `Security.PriceStep` in tatsächliche Preisversätze umgewandelt. Wenn das Instrument keinen gültigen Preisschritt oder Multiplikator bereitstellt, greift die Strategie auf einen Wert von `1` zurück, um die Berechnungen definiert zu halten. Logging messages document any abnormal configuration so operators can adjust parameters quickly.

## Implementierungshinweise

- **Auftragslebenszyklus** – Die Strategie verfolgt die neuesten `Order`-Objekte, die von `BuyStop` und `SellStop` zurückgegeben wurden. Hilfsmethoden verwerfen Referenzen, sobald die Bestellung zu `Done`, `Canceled` oder `Failed` wechselt, um sicherzustellen, dass veraltete Bestellungen nicht mit aktiven verwechselt werden.
- **Ablaufbehandlung** – StockSharp-Börsen unterstützen nicht allgemein den serverseitigen Ablauf für Stop-Orders. Anstatt sich auf Broker-spezifische Felder zu verlassen, überwacht die Strategie die Zeitstempel lokal und ruft `CancelOrder` auf, wenn eine ausstehende Bestellung ihre Frist abläuft.
- **Margin-Annäherung** – Die Margin-Verfügbarkeit wird anhand des Portfolio-Eigenkapitals und der konfigurierten Hebelwirkung geschätzt. Dadurch bleibt das Verhalten nahe an `OrderCalcMargin`, ohne dass börsenspezifische Implementierungen erforderlich sind.
- **Verwendung von API auf hoher Ebene** – Alle Vorgänge basieren auf den Hilfsprogrammen `SubscribeLevel1`, `BuyStop`, `SellStop` und `StartProtection` auf hoher Ebene, was den Konvertierungsrichtlinien entspricht und den Code prägnant hält.

Diese Dokumentation enthält bewusst umfangreiche Details, damit Händler jede Nuance der Konvertierung verstehen und die Parameter sicher an ihre Brokerumgebung anpassen können.
