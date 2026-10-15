# Horizontal Line Levels-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Horizontal Line Levels**-Strategie emuliert den MetaTrader 5 Expert Advisor des gleichen Namens. Sie erstellt kontinuierlich zwei Preisniveaus rund um das aktuelle Kurs und benachrichtigt den Benutzer, sobald der Markt diese überschreitet. Die Implementierung stützt sich auf Level1 (Bid/Ask)-Marktdaten und ahmt den ursprünglichen OnTick/OnTimer-Workflow nach, ohne Orders zu senden.

## Grundidee

1. Level1-Daten abonnieren und die neuesten besten Bid- und Ask-Preise zwischenspeichern.
2. Die MetaTrader-Punktdistanz in die StockSharp-Preisskala konvertieren.
3. Das beste Ask nach oben und das beste Bid nach unten um die konfigurierte Distanz verschieben, wodurch zwei virtuelle horizontale Linien entstehen.
4. Periodisch (über einen internen Timer) prüfen, ob Bid oder Ask diese Referenzniveaus überschreiten und Alerts im Strategie-Journal protokollieren.

## Parameter

| Name | Standard | Beschreibung |
| --- | --- | --- |
| `TimerPeriodMinutes` | `1` | Minuten zwischen zwei aufeinanderfolgenden Timer-Prüfungen. Muss positiv bleiben. |
| `OffsetPoints` | `50` | Abstand in MetaTrader-Punkten, der über dem Ask und unter dem Bid beim Erstellen der Linien angewendet wird. |

## Verhaltensdetails

- **Datenabonnement**: `GetWorkingSecurities` registriert einen Level1-Stream, damit die Strategie Bid/Ask-Updates auch ohne Kerzen empfängt.
- **Initialisierung**: Wenn sowohl das beste Bid als auch das beste Ask zum ersten Mal verfügbar sind, speichert `RecalculateLevels` die aktuellen oberen und unteren horizontalen Niveaus.
- **Timer**: Jeder Timer-Tick erstellt fehlende Niveaus neu (falls die Initialisierung vor der Kursverfügbarkeit erfolgte) und gibt Log-Meldungen aus, sobald der Markt eine der Grenzen verletzt.
- **MetaTrader-Punkt-Übersetzung**: Der Helfer `EnsurePointSize` konvertiert MetaTrader-"Punkte" in absolute Preiserhöhungen mit `Security.PriceStep`. Dieselbe Technik wird in anderen konvertierten Strategien zur Aufrechterhaltung der numerischen Kompatibilität verwendet.
- **Kein Trading**: Die Strategie sendet nie Orders; sie produziert nur Alerts über `AddInfoLog`. Dies entspricht dem ursprünglichen Expert, der Pop-up-Alerts anzeigte, wenn der Preis eine der Linien berührte.
- **Stopp/Reset**: Das Stoppen der Strategie bricht den Timer ab und löscht alle zwischengespeicherten Werte, sodass der nächste Lauf von einem sauberen Zustand beginnt.

## Typische Verwendung

1. Die Strategie an das gewünschte Instrument anhängen und `TimerPeriodMinutes` und `OffsetPoints` in der Designer-UI einstellen.
2. Die Strategie starten. Sobald ein vollständiger Kurs-Snapshot eintrifft, bestätigt ein Log-Eintrag wie `Horizontal levels updated. Upper: 1.12345, Lower: 1.12245.` die berechneten Schwellenwerte.
3. Das Log-Fenster beobachten. Wenn das Ask über das obere Niveau steigt (oder das Bid unter das untere Niveau fällt), gibt die Strategie die entsprechende Alert-Meldung aus.
4. Wenn der Offset geändert oder die Strategie neu gestartet wird, werden die Niveaus mit den neuen Parametern neu berechnet.

## Klassifizierung

- **Kategorie**: Dienstprogramme / Alerts
- **Richtung**: Keine
- **Ausführungsstil**: Ereignisgesteuertes Monitoring
- **Datenanforderungen**: Level1 Bid/Ask
- **Komplexität**: Grundlegend
- **Empfohlener Zeitrahmen**: Beliebig (rein kursbasiert)
- **Risikomanagement**: Nicht anwendbar (keine Positionen eröffnet)

Diese Konvertierung behält das Alert-zentrierte Verhalten des MetaTrader-Originals bei und nutzt dabei StockSharp-Abstraktionen wie Strategie-Timer und Level1-Abonnements.
