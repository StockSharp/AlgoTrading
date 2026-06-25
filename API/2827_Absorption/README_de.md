# Absorptions-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie repliziert den Absorption-Expert Advisor für MetaTrader. Sie sucht nach "engulfing"-Kerzen, die den Bereich des vorherigen Balkens absorbieren und ein Extrem innerhalb eines kurzen Rückblicks bilden. Wenn eine solche Absorptionskerze erscheint, platziert der Algorithmus Stop-Orders auf beiden Seiten des Marktes und verwaltet die resultierende Position mit einer Kombination aus festen Zielen, Breakeven-Logik und einem Trailing Stop.

## Handelslogik

1. **Mustererkennung**
   - Die letzten zwei abgeschlossenen Kerzen werden überprüft.
   - Eine Kerze wird als *Absorptionsbalken* behandelt, wenn ihr Hoch über dem vorherigen Kerzenhoch und ihr Tief unter dem vorherigen Kerzentief liegt.
   - Der Balken wird validiert, indem geprüft wird, ob sein Hoch oder Tief der extremste Wert innerhalb der letzten `MaxSearch` Kerzen ist.
   - Der ältere Kerze (zwei Balken zurück) wird Priorität eingeräumt. Wenn beide Balken die Absorptionsbedingung erfüllen, wird der ältere Balken verwendet; andernfalls kann die neueste Kerze das Setup auslösen.
2. **Orderplatzierung**
   - Eine Kauf-Stop-Order wird am Balkenhoch plus dem konfigurierten `Indent` platziert.
   - Eine Verkauf-Stop-Order wird am Balkentief minus demselben `Indent` platziert.
   - Beide Orders verwenden das gemeinsame Strategie-Volumen.
   - Jede offene Order speichert ihr eigenes Schutz-Stop-Niveau und optionales Take-Profit-Ziel. Orders verfallen automatisch nach `OrderExpirationHours`, wenn sie nicht ausgeführt werden.
3. **Positionsmanagement**
   - Wenn eine Seite ausgeführt wird, wird die gegenüberliegende offene Order storniert.
   - Der anfängliche Stop befindet sich auf der gegenüberliegenden Seite des Absorptionsbalken minus/plus dem Indent.
   - Ein optionaler fester Take-Profit schließt den Trade, sobald die konfigurierte Distanz in Preisschritten erreicht ist.
   - Das Breakeven-Modul verschiebt den Stop-Loss auf `Einstieg + Breakeven` (Long) oder `Einstieg - Breakeven` (Short), nachdem der Preis um `BreakevenProfit` Schritte vorrückt.
   - Der Trailing Stop hält den Stop-Loss auf `TrailingStop`-Distanz vom besten Preis und aktualisiert nur, wenn der Preis mindestens `TrailingStep` Schritte in die profitable Richtung bewegt.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Kerzendatentyp zum Abonnieren (Standard: 1-Stunden-Zeitrahmen). |
| `MaxSearch` | Anzahl der neuesten Kerzen zur Bestätigung von Hoch/Tief-Extremen. |
| `TakeProfitBuy` | Distanz in Preisschritten für die Long-Take-Profit-Order. `0` deaktiviert das Ziel. |
| `TakeProfitSell` | Distanz in Preisschritten für die Short-Take-Profit-Order. `0` deaktiviert das Ziel. |
| `TrailingStop` | Trailing-Stop-Distanz in Preisschritten. `0` deaktiviert das Trailing. |
| `TrailingStep` | Minimale Vorwärtsbewegung erforderlich, bevor der Trailing Stop vorgerückt wird. Muss positiv sein, wenn Trailing aktiviert ist. |
| `Indent` | Versatz in Preisschritten, der über/unter dem Absorptionsbalken hinzugefügt wird, um Stop-Einstiegsniveaus zu definieren. |
| `OrderExpirationHours` | Laufzeit der offenen Orders. Nach diesem Zeitraum werden die Orders storniert, wenn sie nicht ausgelöst wurden. |
| `Breakeven` | Versatz auf den Stop-Loss, wenn die Breakeven-Regel ausgelöst wird. `0` deaktiviert Breakeven. |
| `BreakevenProfit` | Gewinnsschwelle (in Preisschritten), die erreicht werden muss, bevor der Stop-Loss auf Breakeven verschoben wird. |

Alle abstandsbasierten Eingaben werden als Vielfache des Instrument-Preisschritts ausgedrückt. Das Standard-Strategievolumen ist auf `0.1` gesetzt.

## Risikomanagement

Die Strategie verwendet nur Market Orders für Ausstiege. Stop-Loss-, Take-Profit-, Breakeven- und Trailing-Regeln überwachen Kerzenhochs und -tiefs, um Intrabar-Berührungen zu erkennen. Sobald eine Ausstiegsorder gesendet wird, werden keine weiteren Ausstiegsanfragen generiert, bis die aktuelle Position flach ist.
