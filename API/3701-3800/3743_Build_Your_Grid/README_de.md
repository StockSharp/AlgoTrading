# Erstellen Sie Ihre Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Build Your Grid Strategy** ist eine direkte Umsetzung des MetaTrader Expertenberaters „BuildYourGridEA“. Es hält zwei unabhängig
Dent Ladders von Marktpositionen auf der Long- und Short-Seite und fügt neue Ebenen hinzu, wenn der Preis um eine konfigurierbare Anzahl von Pip steigt
s und erhöht optional das gehandelte Volumen geometrisch oder exponentiell. Der Warenkorb kann bei Erreichen eines kombinierten Gewinnziels geschlossen werden
et wird erreicht, wenn ein in Pips gemessener maximaler Verlust überschritten wird, oder durch die Erteilung von Absicherungsaufträgen bei Unterbrechung des Floating Drawdowns
Er ist ein Prozentsatz des Kontostands.

## Wie es funktioniert

1. **Erste Eingaben.** Abhängig von der *Auftragserteilung* eröffnet die Strategie die ersten Kauf-, Verkaufs- oder beide Marktaufträge, sobald die Spread-Bedingungen dies zulassen.
2. **Netzausbau.** Zusätzliche Aufträge werden entweder mit oder gegen den Trend ausgelöst. Der Abstand zur nächsten Ebene wird in Pips gemessen, wahlweise multipliziert mit der Anzahl bereits offener Orders oder einer Zweierpotenz.
3. **Volumenverlauf.** Die Auftragsgröße folgt der ausgewählten Lot-Verlaufsregel (statisch, geometrisch oder exponentiell) und kann durch den *Max. Multiplikator* relativ zum ersten Eintrag begrenzt werden.
4. **Gewinnmitnahmen.** Der gesamte Korb wird geschlossen, sobald der gesamte variable PnL das Ziel überschreitet, das entweder in Pips oder in der Kontowährung ausgedrückt wird.
5. **Verlustschutz.** Wenn der kumulative Verlust den konfigurierten Pip-Schwellenwert überschreitet, schließt die Strategie je nach *Verlustbehandlung*-Modus entweder das älteste Ticket auf jeder Seite oder den gesamten Korb.
6. **Absicherung.** Wenn der variable Drawdown den *Hedge-Schwellenwert (%)* erreicht, wird eine Ausgleichsorder in der Größe der Volumendifferenz und des *Hedge-Multiplikators* eingereicht, um das Engagement einzufrieren.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `Order Placement` | Welche Richtungen zum Öffnen neuer Ebenen zulässig sind (beide, nur lang, nur kurz). |
| `Grid Direction` | Ob zusätzliche Aufträge dem Trend folgen oder die Bewegung abschwächen. |
| `Grid Step (pips)` | Basisabstand in Pips zur nächsten Ebene, bevor Multiplikatoren angewendet werden. |
| `Step Progression` | Statischer Abstand, geometrisches Wachstum (× count) oder exponentielles Wachstum (× 2^(n-1)). |
| `Close Target` | Art des Gewinnziels (Pips oder Kontowährung). |
| `Target (pips)` / `Target (currency)` | Schwellenwert, der überschritten werden muss, um den Korb mit Gewinn zu schließen. |
| `Loss Handling` | Aktion, wenn das Pip-Drawdown-Limit erreicht wird (nichts tun, die ersten Tickets schließen oder alle schließen). |
| `Loss (pips)` | Maximal tolerierter kombinierter Verlust, bevor der Schutz greift. |
| `Use Hedge` | Ermöglicht Hedge-Orders zum Ausgleich des Nettorisikos bei starken Drawdowns. |
| `Hedge Threshold (%)` | Prozentsatz des Kontostands, der als Auslöser für die Absicherung verwendet wird. |
| `Hedge Multiplier` | Bei der Erteilung des Sicherungsauftrags wird ein Multiplikator auf die Volumendifferenz angewendet. |
| `Auto Volume` / `Risk Factor` | Balancegesteuerte Positionsgrößenbestimmung. Volumen = Saldo × Risikofaktor / 100000. |
| `Manual Volume` | Die Losgröße wurde korrigiert, wenn die automatische Größenbestimmung deaktiviert ist. |
| `Lot Progression` | Statische, geometrische oder exponentielle Skalierung für aufeinanderfolgende Ordnungen. |
| `Max Multiplier` | Begrenzt die Losgröße auf `firstLot × MaxMultiplier`. |
| `Max Orders` | Maximale Anzahl gleichzeitig offener Positionen (0 = unbegrenzt). |
| `Max Spread` | Blockiert neue Trades, während der Spread in Pips über dem Schwellenwert liegt (0 = ignorieren). |
| `Use Completed Bar` / `Candle Type` | Signale nur einmal pro abgeschlossener Kerze des ausgewählten Typs auswerten. |

## Nutzungshinweise

- Die Strategie basiert auf den besten Bid/Ask-Aktualisierungen. Konfigurieren Sie Ihren Datenfeed, um Notierungen der Stufe 1 mit genauen Spreads bereitzustellen.
- Absicherungsaufträge hängen vom Portfoliowert ab. Stellen Sie bei der Ausführung im StockSharp-Designer oder -Tester sicher, dass das verbundene Portfolio ein aussagekräftiges Gleichgewicht meldet.
- Grid-Strategien akkumulieren schnell Risiken. Beginnen Sie mit konservativen Volumina und testen Sie die Konfiguration in einer Simulation, bevor Sie sie auf den Live-Handel anwenden.
- Wenn `Use Completed Bar` aktiviert ist, wird die Handelslogik nur einmal pro fertiger Kerze ausgewertet, was die Option „Abgeschlossene Leiste verwenden“ des ursprünglichen Beraters nachahmt.
