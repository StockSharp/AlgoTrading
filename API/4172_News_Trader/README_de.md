# Strategie für Nachrichtenhändler
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert das Verhalten des ursprünglichen **NewsTrader.mq4**-Skripts, indem beide Seiten des Marktes kurz vor einer geplanten makroökonomischen Veröffentlichung bewaffnet werden. Zehn Minuten vor dem konfigurierten Nachrichtenzeitstempel sendet der Bot ein Paar Breakout-Stop-Orders und fügt sofort Schutzausgänge hinzu, wenn eine Seite ausgelöst wird.

## Kernlogik

- Verwendet ein 1-Minuten-Kerzenabonnement (konfigurierbar) ausschließlich als Timing-Quelle.
- Berechnet den Aktivierungszeitpunkt als `news time - LeadMinutes` und wartet bis zur ersten fertigen Kerze, deren Öffnungszeit an diesem Punkt oder darüber hinaus liegt.
- Platziert einen Verkaufsstopp unter dem aktuellen Preis und einen Kaufstopp darüber, versetzt um `BiasPips`, umgewandelt durch `Security.PriceStep` (spiegelt die `bias * Point`-Logik in MQL4 wider).
- Sobald eine ausstehende Order ausgeführt wurde, wird die entgegengesetzte ausstehende Order storniert; Spezielle Stop-Loss- und Take-Profit-Orders werden anhand der konfigurierten Pip-Abstände platziert.
- Stop-Loss- oder Take-Profit-Ausfüllungen heben die verbleibende Schutzorder auf und verflachen die Strategie.
- Ruft `StartProtection()` beim Start auf, damit die Strategie mit höherstufigen StockSharp-Sicherheitsmaßnahmen zusammenarbeitet.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `TradeVolume` | Mit jeder ausstehenden Bestellung eingereichte Verträge. | `1` |
| `StopLossPips` | Stop-Loss-Distanz in Pips (0 deaktiviert die Stop-Order). | `10` |
| `TakeProfitPips` | Take-Profit-Distanz in Pips (0 deaktiviert die Zielorder). | `10` |
| `BiasPips` | Abstand vom Referenzpreis zu den Breakout-Stop-Orders. | `20` |
| `LeadMinutes` | Minuten vor dem Nachrichtenzeitstempel, wenn die Breakout-Befehle aktiviert werden. | `10` |
| `NewsYear`, `NewsMonth`, `NewsDay`, `NewsHour`, `NewsMinute` | Bestandteile der geplanten Nachrichtenzeit (Bahnsteiguhr). | `2010`, `3`, `8`, `1`, `30` |
| `CandleType` | Kerzendatentyp, der zur Verfolgung des Zeitverlaufs verwendet wird. | `1 Minute` |

## Implementierungshinweise

- Die Strategie setzt `Volume` während `OnStarted` auf `TradeVolume` und stellt so sicher, dass Hilfsmethoden wie `BuyStop` und `SellStop` die erwartete Größe verwenden.
- `Security.PriceStep` muss definiert sein; Andernfalls löst die Logik eine Ausnahme aus, da Pip-basierte Entfernungen nicht in Preise umgewandelt werden können.
- Candle-Close-Preise werden bei der Berechnung der Stop-Levels als Proxy für den letzten Geld-/Briefkurs verwendet – entsprechend der ursprünglichen MQL4-Logik, die sich auf den aktuellsten Kurs zum Auslösezeitpunkt stützte.
- Ausstehende Bestellungen werden nur einmal aufgegeben; Der Algorithmus schaltet sich nicht neu ein, nachdem das konfigurierte Nachrichtenereignis vorüber ist.
- Schutzbefehle werden übersprungen, wenn ihr jeweiliger Pip-Abstand Null ist, wodurch das Verhalten für manuelle Eingriffe konfigurierbar bleibt.

## Dateien

- `CS/NewsTraderStrategy.cs` – C#-Implementierung der Strategie.

Die Python-Version wurde wie gewünscht absichtlich weggelassen.
