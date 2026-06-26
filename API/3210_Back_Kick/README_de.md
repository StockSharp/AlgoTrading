# Back Kick-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Back Kick**-Strategie ist ein gehedgtes Ausbruchssystem, das vom MetaTrader 5-Expertenberater `Back kick.mq5` konvertiert wurde. Sie hält kontinuierlich ein zweiseitiges Exposure aufrecht, indem sie beim Schließen jeder Bar sowohl eine Long- als auch eine Short-Marktposition eröffnet. Jedes Bein ist mit symmetrischen Stop-Loss- und Take-Profit-Distanzen in Pips geschützt. Der StockSharp-Port hält die gepaartePositionen unabhängig, indem er deren Zustand manuell verfolgt, anstatt sich auf die aggregierte Nettoposition zu stützen.

## Trading-Logik

1. Den konfigurierten Zeitrahmen-Kerzen abonnieren. Wenn eine Kerze schließt und keine aktiven gehedgten Beine vorhanden sind, ein neues Eintrittspaar anfordern.
2. Sofort eine Market-Buy- und eine Market-Sell-Order mit demselben Volumen einreichen. Jedes Bein behält seine eigenen Stop-Loss- und Take-Profit-Abstände bei, die aus Pip-Distanzen konvertiert wurden.
3. Die besten Geld/Brief-Preise aus Level-1-Daten überwachen. Wenn ein Bein seinen Schutzpreis erreicht, wird es mit einer Market-Order geschlossen, während das gegenüberliegende Bein aktiv bleibt, bis sein eigener Ausstieg ausgelöst wird.
4. Nachdem beide Beine flat sind, wartet die Strategie auf die nächste abgeschlossene Kerze, bevor die Hedge neu erstellt wird.

Dieses Verhalten spiegelt den ursprünglichen Experten wider, der ständig in beide Richtungen wieder einsteigt, um abrupte Preis-"Kicks" zu erfassen.

## Parameter

| Name | Beschreibung | Standard | Hinweise |
| ---- | ----------- | ------- | ----- |
| `OrderVolume` | Volumen für jedes Hedge-Bein. | `0.1` | Auf den `VolumeStep` des Instruments normalisiert, muss `MinVolume`/`MaxVolume` einhalten. |
| `StopLossPips` | Stop-Loss-Distanz in Pips. | `50` | Auf `0` setzen, um den Schutz-Stop für beide Beine zu deaktivieren. |
| `TakeProfitPips` | Take-Profit-Distanz in Pips. | `140` | Auf `0` setzen, um den Schutz-Take-Profit zu deaktivieren. |
| `CandleType` | Zeitrahmen, der neue gehedgte Paare auslöst. | `15m` | Akzeptiert jeden `TimeFrame`, der vom ausgewählten Wertpapier unterstützt wird. |
| `LogDiagnostics` | Aktiviert ausführliche Protokollierung über Einstiege und Ausstiege. | `false` | Nützlich zum Debuggen von Fill-Sequenzen. |

## Implementierungshinweise

- **Pip-Konvertierung** – Der ursprüngliche EA passt die Pip-Größe für 3/5-Stellen-Symbole an. Der StockSharp-Port repliziert dies, indem er den Preisschritt bei Bedarf mit `10` multipliziert.
- **Manuelles Hedging-Modell** – StockSharp verwendet Netto-Positionen, daher hält die Strategie einen Zustand pro Bein (`PositionState`) und sendet explizite Market-Orders für Ausstiege. Dies ermöglicht ein Verhalten ähnlich dem MT5-Hedged-Account-Modus.
- **Risikomanagement** – Stop-Loss- und Take-Profit-Niveaus sind optional. Wenn eines deaktiviert ist, wird dieses Bein nur geschlossen, wenn das gegenüberliegende Schutzniveau erreicht wird oder durch externe Verwaltung.
- **Schutzdienst** – `StartProtection()` wird weiterhin aufgerufen, damit das Framework unerwartete Verbindungsabbrüche überwachen kann, obwohl benutzerdefinierte Ausstiegslogik implementiert ist.

## Verwendung

1. Strategie an ein Wertpapier mit zuverlässigen Level-1-Daten (Geld/Brief) und den gewünschten Zeitrahmen-Kerzen anhängen.
2. Pip-Abstände und Handelsvolumen entsprechend dem Risikoprofil konfigurieren.
3. Strategie starten; sie wartet auf den nächsten Kerzenschluss, bevor das gehedgte Paar eingereicht wird.
4. Protokolle oder Chart überwachen, um zu beobachten, wie jedes Bein unabhängig aussteigt.

## Unterschiede zur MT5-Version

- Geldmanagement basierend auf Risikoprozentsatz wird nicht übertragen; verwenden Sie `OrderVolume` zur Steuerung der Handelsgröße.
- Da StockSharp Portfolio-Positionen aggregiert, emuliert die Strategie Hedging durch interne Buchhaltung. Dies gewährleistet ein dem ursprünglichen Experten nahes Verhalten, während es mit Brokern kompatibel bleibt, die Positionen netzen.
- Broker-spezifische Freeze/Stop-Level-Prüfungen werden weggelassen. Stattdessen wirft die Volumen-Normalisierungsroutine beschreibende Ausnahmen, wenn Exchange-Limits verletzt werden.

## Dateien

- `CS/BackKickStrategy.cs` – Strategieimplementierung mit der High-Level-StockSharp-API.
- `README.md` – Englische Dokumentation (diese Datei).
- `README_ru.md` – Russische Dokumentation.
- `README_zh.md` – Chinesische Dokumentation.
