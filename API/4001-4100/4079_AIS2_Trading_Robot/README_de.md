# AIS2 Trading Robot 20005 (Port StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

AIS2 Trading Robot 20005 ist ein Intraday-Breakout-Expertenberater, der ursprünglich für MetaTrader 4 geschrieben wurde. Der Port erstellt seine Multi-Timeframe-Logik auf der Grundlage der High-Level-Strategie API von StockSharp neu. Die Strategie wartet auf Momentumausbrüche über/unter der Mitte der vorherigen Kerze mit höherem Zeitrahmen, wendet dynamische Take-Profit- und Stop-Loss-Distanzen an, die aus der Spanne dieser Kerze abgeleitet werden, und verwaltet Positionen mit einem sekundären, schnelleren Zeitrahmen, der einen Trailing Stop antreibt.

Der Fokus der Umstellung liegt auf Transparenz und manueller Kontrolle: Positionen werden mit Marktaufträgen eröffnet, Schutzniveaus werden innerhalb der Strategie selbst durchgesetzt und eine konfigurierbare Handelspause verhindert schnelle Wiedereinstiege. Die aktienbasierte Positionsgröße spiegelt die ursprüngliche „Reserve“-Logik wider und ermöglicht es Benutzern, jedem Handel einen Bruchteil des Portfoliowerts zuzuweisen, während ein Kapitalpuffer unangetastet bleibt.

## Kernlogik

1. **Analyse des primären Zeitrahmens** – Für jede abgeschlossene Kerze des Hauptzeitrahmens (Standard 15 Minuten) berechnet die Strategie Folgendes:
   - Kerzenmittelpunkt `(High + Low) / 2`.
   - Bereichsbasierte Take-Profit- und Stop-Loss-Distanzen (`range * TakeFactor` und `range * StopFactor`).
   - Aktuelle Spread-Annäherung, Stopp-/Einfrierpuffer und ein minimaler Nachlaufschritt.
2. **Breakout-Bedingungen** – Long-Einstiege erfordern sowohl einen Schlusskurs über dem Mittelpunkt als auch den aktuellen Briefkurs, der das vorherige Hoch plus Spread durchbricht. Shorts spiegeln den Zustand für Tiefs wider. Aufträge werden blockiert, wenn die berechneten Stopp-/Zielentfernungen die Einschränkungen auf Brokerebene nicht erfüllen.
3. **Risikomanagement** – Die Positionsgröße wird vom Portfolioeigenkapital abgeleitet: `OrderReserve` definiert den handelbaren Anteil, während `AccountReserve` einen Teil unberührt lässt. Wenn das verfügbare Kapital oder die Brokerlimits den Handel nicht zulassen, wird die Einrichtung übersprungen.
4. **Handelsmanagement** – Der schnellere Zeitrahmen (Standard 1 Minute) aktualisiert kontinuierlich die Nachlaufdistanz. Wenn der Preis steigt, verschiebt sich der Stop zugunsten des Handels, sobald die sekundäre Spanne dies rechtfertigt. Das Erreichen eines Ziels oder Stopps führt zu einem sofortigen Ausstieg aus dem Markt.
5. **Betriebliche Leitplanken** – Ein Cooldown-Timer (`TradingPauseSeconds`) repliziert die ursprüngliche Handelspause von MQL. Die Strategie abonniert auch das Auftragsbuch, um Live-Bid/Ask-Werte zu erfassen; Wenn es nicht verfügbar ist, wird auf das Schließen der Kerze zurückgegriffen.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `PrimaryCandleType` | Höherer Zeitrahmen zur Generierung von Einstiegssignalen. | 15-Minuten-Kerzen |
| `SecondaryCandleType` | Kürzerer Zeitrahmen für Trailing-Stop-Berechnungen. | 1-Minuten-Kerzen |
| `TakeFactor` | Auf den primären Kerzenbereich angewendeter Multiplikator, um die Take-Profit-Distanz aufzubauen. | 1.7 |
| `StopFactor` | Der auf den primären Kerzenbereich angewendete Multiplikator dient zum Aufbau der Stop-Loss-Distanz. | 1.7 |
| `TrailFactor` | Auf den sekundären Kerzenbereich angewendeter Multiplikator für nachlaufende Aktualisierungen. | 0,5 |
| `AccountReserve` | Bruchteil des Eigenkapitals, der in der Reserve gehalten wird (nicht für den Handel verwendet). | 0,20 |
| `OrderReserve` | Anteil des pro Trade zugewiesenen Gesamtkapitals vor Puffern. | 0,04 |
| `BaseVolume` | Fallback-Handelsvolumen, wenn die Risikogröße nicht berechnet werden kann. | 1 Los |
| `StopBufferTicks` | Den Compliance-Prüfungen auf Stop-Ebene des Brokers wurden zusätzliche Häkchen hinzugefügt. | 0 |
| `FreezeBufferTicks` | Zusätzliche Häkchen verhindern häufige Stoppaktualisierungen in der Nähe des Einfrierniveaus. | 0 |
| `TrailStepMultiplier` | Beim Validieren nachfolgender Schritte wird ein Multiplikator auf die Ausbreitung angewendet. | 1 |
| `TradingPauseSeconds` | Abklingzeit zwischen aufeinanderfolgenden Trades. | 5 Sekunden |

Alle numerischen Parameter machen `SetCanOptimize()` verfügbar (sofern sinnvoll), damit sie an StockSharp-Optimierungsszenarien teilnehmen können.

## Nutzungshinweise

- Hängen Sie die Strategie an ein Wertpapier an und stellen Sie sicher, dass Level1-/Orderbuchdaten für eine genaue Spread-Erkennung verfügbar sind. Ohne Live-Kurse wird die Logik immer noch mithilfe von Kerzenschlüssen ausgeführt, die Stopp-Validierungen werden jedoch konservativ.
- Legen Sie `PrimaryCandleType`/`SecondaryCandleType` auf Zeitrahmen fest, die in Ihrem Datenfeed vorhanden sind. Der Port verwendet `SubscribeCandles` und bindet Handler über die hohe Ebene API von StockSharp.
- Der Trailing Stop ist virtuell (intern verwaltet); Es werden keine Stop-Orders an den Broker gesendet. Wenn Sie serverseitige Stopps benötigen, erweitern Sie den Code, um Schutzanordnungen nach Eingaben zu registrieren.
- `StartProtection()` wird beim Start aufgerufen, damit die Engine bei Bedarf unerwartete Positionen auflöst.

## Unterschiede zum Original EA

- Die MetaTrader-Version manipulierte terminalweite globale Variablen; Dieser Port speichert Parameter innerhalb der Strategie und stellt sie über `StrategyParam`-Wrapper bereit.
- Auftragsänderungen wurden durch direkte Marktaustritte ersetzt, da StockSharp die Stopp-/Ziellogik innerhalb des Algorithmus selbst verwaltet.
- Risikoberechnungen basieren auf dem von StockSharp bereitgestellten Portfolio-Eigenkapital und nicht auf Kontostandabfragen von MT4.

## Dateien

- `CS/Ais2TradingRobot20005Strategy.cs` – Strategieumsetzung mit StockSharp auf hoher Ebene API.
- `README.md` – Englische Beschreibung (diese Datei).
- `README_zh.md` – Chinesische Übersetzung.
- `README_ru.md` – Russische Übersetzung.
