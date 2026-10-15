# MultiTrader Currency Strength-Strategie (3253)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist ein High-Level-StockSharp-Port des öffentlichen "MultiTrader"-MQL-Panels (Codebasis #24786). Der ursprüngliche Expertenberater war ein diskretionäres Dashboard, das die relative Stärke der acht wichtigsten Währungen anzeigte, visuelle/akustische Alerts auslöste, wenn eine Währung extrem stark oder schwach wurde, und vorschlug, welches Forex-Paar gehandelt werden soll. Die StockSharp-Version automatisiert denselben analytischen Workflow und führt optional Trades auf dem stärksten-vs.-schwächsten Paar aus.

Die Logik berechnet eine prozentuale Position des Schlusskurses jedes Symbols innerhalb seiner aktuellen Kerzenspanne. Das Mitteln der relevanten Kreuze ergibt einen Stärkewert für AUD, CAD, CHF, EUR, GBP, JPY, NZD und USD. Wenn eine Währung über den konfigurierbaren Kaufschwellenwert steigt und eine andere unter den Verkaufsschwellenwert fällt, empfiehlt die Strategie das aus diesen Währungen gebildete Paar. Wenn das Paar im konfigurierten Universum vorhanden ist, kann die Strategie automatisch eine Market-Order in diese Richtung platzieren.

## Währungsstärkemodell
Die Prozentwertung für ein Symbol wird berechnet als:

```
percent = 100 * (Close - Low) / (High - Low)
```

Die Stärke jeder Währung wird aus sieben Kreuzen abgeleitet, was die MQL-Implementierung widerspiegelt. Eine `100 - percent`-Inversion wird angewendet, wenn die Währung als Kurswährung im Paar erscheint:

| Währung | Komponenten |
| --- | --- |
| AUD | AUDJPY, AUDNZD, AUDUSD, 100-EURAUD, 100-GBPAUD, AUDCHF, AUDCAD |
| CAD | CADJPY, 100-NZDCAD, 100-USDCAD, 100-EURCAD, 100-GBPCAD, 100-AUDCAD, CADCHF |
| CHF | CHFJPY, 100-NZDCHF, 100-USDCHF, 100-EURCHF, 100-GBPCHF, 100-AUDCHF, 100-CADCHF |
| EUR | EURJPY, EURNZD, EURUSD, EURCAD, EURGBP, EURAUD, EURCHF |
| GBP | GBPJPY, GBPNZD, GBPUSD, GBPCAD, 100-EURGBP, GBPAUD, GBPCHF |
| JPY | 100-AUDJPY, 100-CHFJPY, 100-CADJPY, 100-EURJPY, 100-GBPJPY, 100-NZDJPY, 100-USDJPY |
| NZD | NZDJPY, 100-GBPNZD, NZDUSD, NZDCAD, 100-EURNZD, 100-AUDNZD, NZDCHF |
| USD | 100-AUDUSD, USDCHF, USDCAD, 100-EURUSD, 100-GBPUSD, USDJPY, 100-NZDUSD |

Die Strategie speichert die letzte abgeschlossene Kerze pro Paar, behält den neuesten Prozentsatz und aktualisiert die Währungsstärken nach jeder Aktualisierung.

## Handel und Alerts
1. Wenn alle acht Währungen gültige Daten haben, protokolliert die Strategie einen Snapshot (stärkste bis schwächste).
2. Wenn der stärkste Wert **≥ BuyLevel** und der schwächste Wert **≤ SellLevel** ist, wird ein Handelsvorschlag generiert.
3. Die Strategie versucht, das direkte Paar zu finden (starke Währung als Basis, schwache Währung als Kurs). Wenn es nicht existiert, prüft sie die inverse Orientierung und greift schließlich auf Paare mit USD zurück.
4. Das erkannte Paar und die Richtung werden protokolliert. Wenn `EnableAutoTrading` `true` ist und `OrderVolume` positiv ist, gibt die Strategie eine Market-Order in die vorgeschlagene Richtung aus. Entgegengesetzte Positionen werden automatisch durch Erhöhung der Auftragsgröße geflacht.

Signale werden durch Merken des zuletzt vorgeschlagenen Paares und der Seite gedrosselt, was doppelte Alerts verhindert, bis der Markt die Schwellenwertzone verlässt.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `Universe` | Liste von `Security`-Objekten, die die FX-Paare repräsentieren (28 Hauptpaare empfohlen). | Erforderlich |
| `CandleType` | Kerzenspezifikation für Berechnungen (Täglich, Wöchentlich, Monatlich, etc.). | Tageskerzen |
| `BuyLevel` | Schwellenwert, über dem eine Währung als überkauft gilt. | 90 |
| `SellLevel` | Schwellenwert, unter dem eine Währung als überverkauft gilt. | 10 |
| `EnableAutoTrading` | Aktiviert oder deaktiviert automatische Orderplatzierung. | false |
| `OrderVolume` | Volumen für Market-Orders bei aktiviertem Auto-Trading. | 1 |
| `SymbolPrefix` | Optionales Präfix vom Broker/Exchange (z.B. `m.`). | "" |
| `SymbolSuffix` | Optionales Suffix vom Broker/Exchange (z.B. `.FX`). | "" |

## Konfigurationsschritte
1. **Universum-Setup.** Fügen Sie die 28 wichtigsten Forex-Kreuze zum Strategieuniversum hinzu. Codes sollten den kanonischen Parnamen entsprechen (z.B. `EURUSD`). Verwenden Sie `SymbolPrefix`/`SymbolSuffix`, wenn Ihr Broker Dekorationen hinzufügt.
2. **Zeitrahmenauswahl.** Wählen Sie den gewünschten `CandleType`. Tages-, Wochen- und Monatskerzen reproduzieren die ursprünglichen Panel-Modi.
3. **Schwellenwert-Anpassung.** Passen Sie `BuyLevel`/`SellLevel` an, um zu steuern, wie extrem die Stärke sein muss, bevor ein Signal generiert wird.
4. **Auto-Trading (optional).** Setzen Sie `EnableAutoTrading` auf true und definieren Sie `OrderVolume`. Lassen Sie das Flag auf false, um nur Informationsprotokolle zu empfangen.

## Migrationshinweise
- Die gesamte GUI-Schicht des ursprünglichen MQL-Panels wird bewusst weggelassen. Alle Ausgaben sind über das Strategieprotokoll verfügbar.
- Alerts werden als `LogInfo`-Einträge ausgegeben; Push-/E-Mail-/Desktop-Benachrichtigungen wurden nicht portiert.
- Automatische Stop-Loss-/Zielberechnungen der MQL-Version werden nicht unterstützt; Trader sollten das Risiko über StockSharp-Schutzmodule oder externe Risikokontrollen verwalten.
- Der DES-basierte Lizenz-Helper im MQL-Skript wurde entfernt.

## Empfohlene Verwendung
- Setzen Sie die Strategie in einer Connector-Sitzung ein, die Echtzeit- und historische Kerzen für alle relevanten Paare liefert.
- Kombinieren Sie sie mit einem Chart-Widget, um das vorgeschlagene Paar zu visualisieren und die zugrunde liegenden Kerzenserien zu überwachen.
- Verwenden Sie StockSharp-`StartProtection`-Parameter oder separate Risikostrategien für globale Stops/Ziele.

## Testüberlegungen
- Stellen Sie sicher, dass Ihre Datenquelle abgeschlossene Kerzen für den ausgewählten Zeitrahmen liefert; die Strategie ignoriert unfertige Kerzen.
- Wenn einige Paare im Universum fehlen, kann die entsprechende Währung nicht berechnet werden und es wird kein Signal erzeugt.
- Stellen Sie beim Bewerten der historischen Leistung sicher, dass das Universum während des gesamten Backtests statisch bleibt, um Stärkelücken zu vermeiden.
