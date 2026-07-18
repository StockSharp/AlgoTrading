# Trading-Boxing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Trading-Boxing-Strategie recreates das manuelle Order-Management-Panel des ursprünglichen TradingBoxing-Expertenberaters. Anstelle von Schaltflächen im Chart stellt die StockSharp-Version Parameter bereit, die über die Strategie-UI oder Automatisierung umgeschaltet werden können. Jeder Schalter führt sofort die angeforderte Aktion aus und setzt sich dann zurück, und bietet so eine bequeme Steueroberfläche für Markteinstiege, die Platzierung ausstehender Orders und die Bereinigung bestehender Positionen.

Die Strategie hängt nicht von Indikatorlogik oder Marktdatenereignissen ab. Sie koordiniert lediglich die Auftragsübermittlung und -stornierung für das der Strategieinstanz zugeordnete Wertpapier und Portfolio.

## Parameter
### Volumen-Konfiguration
- `BuyVolume` – Menge, die beim Auslösen der Aktion *Open Buy Market* verwendet wird. Muss positiv sein.
- `SellVolume` – Menge, die beim Auslösen der Aktion *Open Sell Market* verwendet wird. Muss positiv sein.
- `BuyStopVolume` – Menge für neue Buy-Stop-Orders.
- `BuyLimitVolume` – Menge für neue Buy-Limit-Orders.
- `SellStopVolume` – Menge für neue Sell-Stop-Orders.
- `SellLimitVolume` – Menge für neue Sell-Limit-Orders.

### Preis-Konfiguration
- `BuyStopPrice` – Aktivierungspreis für Buy-Stop-Orders.
- `BuyLimitPrice` – Preis für Buy-Limit-Orders.
- `SellStopPrice` – Aktivierungspreis für Sell-Stop-Orders.
- `SellLimitPrice` – Preis für Sell-Limit-Orders.

### Aktionsschalter
Alle Aktionsparameter sind boolesche Schalter. Das Setzen eines Schalters auf `true` führt die entsprechende Aufgabe aus und die Strategie setzt ihn im selben Verarbeitungszyklus zurück auf `false`.

- `CloseBuyPositions` – schließt das aktuelle Long-Engagement (wenn `Position > 0`).
- `CloseSellPositions` – schließt das aktuelle Short-Engagement (wenn `Position < 0`).
- `DeleteBuyStops` – storniert verfolgte Buy-Stop-Orders.
- `DeleteBuyLimits` – storniert verfolgte Buy-Limit-Orders.
- `DeleteSellStops` – storniert verfolgte Sell-Stop-Orders.
- `DeleteSellLimits` – storniert verfolgte Sell-Limit-Orders.
- `OpenBuyMarket` – sendet eine Marktkauforder mit `BuyVolume`.
- `OpenSellMarket` – sendet eine Marktverkaufsorder mit `SellVolume`.
- `PlaceBuyStop` – registriert eine neue Buy-Stop-Order zum `BuyStopPrice` mit `BuyStopVolume` und speichert sie für die spätere Stornierung.
- `PlaceBuyLimit` – registriert eine neue Buy-Limit-Order zum `BuyLimitPrice` mit `BuyLimitVolume` und speichert sie für die spätere Stornierung.
- `PlaceSellStop` – registriert eine neue Sell-Stop-Order zum `SellStopPrice` mit `SellStopVolume` und speichert sie für die spätere Stornierung.
- `PlaceSellLimit` – registriert eine neue Sell-Limit-Order zum `SellLimitPrice` mit `SellLimitVolume` und speichert sie für die spätere Stornierung.

## Verhaltensdetails
- Orders, die durch die ausstehenden Order-Aktionen erstellt wurden, werden intern verfolgt, damit die Löschaktionen sie später stornieren können. Externe Orders, die nicht von dieser Strategie platziert wurden, sind nicht betroffen.
- Die Strategie überprüft, ob sie läuft und ob sowohl `Security` als auch `Portfolio` zugewiesen sind, bevor sie eine Anfrage ausführt. Wenn eine Anforderung fehlt, protokolliert sie eine Warnung und ignoriert den Schalter.
- Die Volumen- und Preisvalidierung repliziert die Sicherheitsprüfungen des ursprünglichen Panels: Jede nicht-positive Menge löst eine Warnung aus und es wird keine Order gesendet.
- Schließaktionen operieren auf der von der Strategie verwalteten Nettoposition. Wenn ein Short gedeckt werden muss, sendet die Strategie eine Marktkauforder gleich `Math.Abs(Position)`; für eine Long-Position sendet sie eine Marktverkaufsorder mit dem aktuellen `Position`-Wert.

## Verwendungshinweise
1. Die Strategie mit einem gültigen Portfolio und Wertpapier starten.
2. Volumen- und Preisparameter an das gehandelte Instrument anpassen.
3. Manuelle Aktionen durch Setzen des erforderlichen booleschen Parameters auf `true` auslösen. Die Eigenschaft kehrt nach Abschluss der Aktion automatisch auf `false` zurück, sodass der nächste Auslöser sofort bereit ist.
4. Die Lösch-Schalter verwenden, um zuvor platzierte ausstehende Orders zu löschen, wenn sich der Handelsplan ändert.

Da die Strategie rein durch Benutzereingaben gesteuert wird, ist keine Abonnierung von Kerzen oder Kursen erforderlich. Sie fungiert als einfacher Ausführungsassistent und spiegelt die Flexibilität der ursprünglichen TradingBoxing-Oberfläche innerhalb der StockSharp-Umgebung wider.
