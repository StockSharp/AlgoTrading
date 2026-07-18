# Manuelle Trading-Lightweight-Utility-Panel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Manual Trading Lightweight Utility Panel-Strategie** repliziert das Verhalten des MT4-Panels „Manual Trading Lightweight Utility“ unter Verwendung der High-Level-Strategie StockSharp API. Es stellt dieselben interaktiven Steuerelemente wie Strategieparameter bereit, sodass der Bediener zwischen Markt-, Limit- und Stop-Orders wechseln, die automatische Preisberechnung anpassen, das Volumenmanagement konfigurieren und Risikokontrollen hinzufügen kann, ohne auf benutzerdefinierte Diagrammobjekte angewiesen zu sein.

Die Strategie ist für den diskretionären Handel konzipiert. Bestellungen werden manuell ausgelöst, indem die Parameter `Send Buy Order` oder `Send Sell Order` in der Benutzeroberfläche geändert werden. Jeder Befehl wird sofort bestätigt, während die Strategie alle Berechnungen – wie automatische Preisvorschläge und Risikostufen – mit Echtzeit-Marktdaten synchronisiert.

## Hauptmerkmale
- **Manueller Orderversand** für Kauf- und Verkäuferseite mit Unterstützung für Markt-, Limit- und Stop-Orders.
- **Automatischer Preisvorschlag**, der die MT4-Panel-Logik widerspiegelt und den vorgeschlagenen Limit- oder Stop-Preis aus dem neuesten Bid/Ask-Stream aktualisiert.
- **Optionaler manueller Preismodus**, der es dem Bediener ermöglicht, den gewünschten Triggerpegel unter Berücksichtigung der Schrittgrößen des Instruments einzugeben.
- **Volumenverwaltung** mit einer globalen Losgröße und individuellen Kauf-/Verkaufsvolumina, wenn der Loskontrollschalter aktiviert ist.
- **Integriertes Stop-Loss- und Take-Profit-Management**, implementiert in der Strategieebene, um auftragsbezogenen Schutz auf MT4 zu emulieren.
- **Detailliertes Feedback** durch Parameter, die immer die neuesten berechneten Einstiegswerte für beide Seiten widerspiegeln.

## Konvertierungshinweise
- Die MT4-Diagrammobjekte (Schaltflächen, Beschriftungen und Bearbeitungsfelder) werden durch Strategieparameter ersetzt, die in logischen Abschnitten gruppiert sind, um einen einfachen Zugriff in Hydra/Terminal zu ermöglichen.
- Schutzstopps und -ziele werden intern durch Beobachtung des Live-Marktpreises gehandhabt, da StockSharp sie nicht wie MT4 in ausstehende Aufträge einbettet.
- In Punkten ausgedrückte Preisversätze verwenden die Metadaten des Instruments (`PriceStep` und `VolumeStep`) wieder, sodass Limits und Stopps stets die Wechselkursbeschränkungen berücksichtigen.

## Nutzung
1. Hängen Sie die Strategie an ein Wertpapier und Portfolio in Hydra oder Terminal an.
2. Konfigurieren Sie die Standardlosgröße, Risikoparameter und Preisversätze.
3. Aktivieren Sie optional `Lot Control`, um unabhängige Volumina für die Kauf- und Verkaufsschaltflächen beizubehalten.
4. Wählen Sie den Auftragstyp (Markt, ausstehendes Limit oder ausstehender Stop) und ob der Auslösepreis dem Markt folgen oder manuell bleiben soll.
5. Wenn Sie fertig sind, schalten Sie `Send Buy Order` oder `Send Sell Order` auf `true` um. Die Strategie übermittelt die entsprechende Bestellung und setzt das Flag nach der Verarbeitung auf `false` zurück.
6. Der Schutzmanager schließt offene Positionen zu den konfigurierten Stop-Loss- oder Take-Profit-Levels, die aus dem ausgeführten Einstiegspreis berechnet werden.

## Dateien
- `CS/ManualTradingLightweightUtilityPanelStrategy.cs` – C#-Implementierung der Strategie.
- `README.md` – Englische Dokumentation (diese Datei).
- `README_zh.md` – Vereinfachte chinesische Dokumentation.
- `README_ru.md` – Russische Dokumentation.
