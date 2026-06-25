# Vortex-Indikator-System-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
- **Quelle**: Konvertiert vom MetaTrader 5 Expert Advisor "Vortex Indicator System" (MQL ID 19137).
- **Konzept**: Verwendet den Vortex-Indikator, um bullische oder bärische Crossover zu erkennen und dann Ausbruchstrigger am Hoch/Tief der Crossover-Kerze zu setzen.
- **Ausführungsstil**: Ausbruchsverfolgung; Trades werden nur initiiert, nachdem der Preis den Crossover durch Überschreiten des Trigger-Levels bestätigt.
- **Marktregime**: Funktioniert auf jedem Instrument und Zeitrahmen, der den Vortex-Indikator und Kerzendaten unterstützt; broker-spezifische Funktionen sind nicht erforderlich.
- **Ordertypen**: Market Orders über `BuyMarket` und `SellMarket`. Die Strategie schließt entgegengesetzte Positionen automatisch, bevor ein neuer Trigger in die Warteschlange gestellt wird.

## Handelslogik
1. Das konfigurierte Kerzentyp abonnieren und den Vortex-Indikator mit der angegebenen Länge berechnen.
2. Einen bullischen Crossover erkennen, wenn `VI+` sich über `VI-` bewegt, nachdem er auf der vorherigen Kerze darunter war:
   - Jede bestehende Short-Position über `ClosePosition()` schließen.
   - Das Hoch der Crossover-Kerze als Long-Trigger-Preis speichern.
   - Jeden ausstehenden Short-Trigger stornieren.
3. Einen bärischen Crossover erkennen, wenn `VI-` sich über `VI+` bewegt, nachdem er auf der vorherigen Kerze darunter war:
   - Jede bestehende Long-Position schließen.
   - Das Tief der Crossover-Kerze als Short-Trigger-Preis speichern.
   - Jeden ausstehenden Long-Trigger stornieren.
4. Während ein Trigger aktiv ist, nachfolgende Kerzen überwachen:
   - Wenn der High-Preis den gespeicherten Long-Trigger durchbricht und die aktuelle Position flat oder short ist, eine Market Buy-Order senden, die groß genug ist, um jedes Short-Exposure umzukehren.
   - Wenn der Low-Preis den gespeicherten Short-Trigger durchbricht und die aktuelle Position flat oder long ist, eine Market Sell-Order senden, die groß genug ist, um jedes Long-Exposure umzukehren.
5. Jeder ausgeführte Trade löscht seinen entsprechenden Trigger. Entgegengesetzte Trigger schließen sich gegenseitig aus.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `Length` | 14 | Periode des Vortex-Indikators. Entspricht dem ursprünglichen MQL-Eingang `VI_Length`. |
| `CandleType` | 60-Minuten-Zeitrahmen | Kerzentyp für Indikatorberechnung und Trigger-Auswertung. Kann auf jeden von der verbundenen Datenquelle unterstützten Zeitrahmen angepasst werden. |
| `Volume` | Aus der Basis-`Strategy`-Eigenschaft | Handelsvolumen für Market Orders. Vor dem Start der Strategie konfigurieren, wenn ein anderer Wert als 1 Kontrakt/Lot erforderlich ist. |

### Wie Parameter das Verhalten beeinflussen
- Eine Erhöhung von `Length` glättet die Vortex-Linien, reduziert die Anzahl der Crossover, verbessert aber deren Zuverlässigkeit.
- Eine Verringerung von `Length` macht das System reaktiver und generiert mehr Trigger und potenzielle Trades.
- Der `CandleType` sollte mit der Datengranularität in der ursprünglichen MQL-Einrichtung (typischerweise der Chart-Zeitrahmen) abgestimmt werden. Kürzere Kerzen liefern schnellere Signale, längere Kerzen konzentrieren sich auf breitere Trends.

## Risikohinweise
- Der ursprüngliche Expert Advisor definiert keine Stop-Loss- oder Take-Profit-Levels. Diese Konvertierung behält dieses Verhalten bei; das Risikomanagement muss extern oder durch Erweiterung der Strategie behandelt werden.
- Die Positionsumkehr erfolgt sofort: Wenn ein entgegengesetztes Signal auftritt, gibt die Strategie `ClosePosition()` aus und wartet auf einen Ausbruch über den Trigger hinaus, bevor sie in die neue Richtung einsteigt.
- Es kann jeweils nur ein Trigger (Long oder Short) aktiv sein. Trigger werden gelöscht, wenn der Preis sie durchbricht oder wenn ein entgegengesetzter Crossover auftritt.

## Gebrauchsanweisung
1. Füge die Strategie zu deinem StockSharp-Projekt hinzu und stelle sicher, dass das Paket `StockSharp.Algo.Indicators` verfügbar ist.
2. Konfiguriere das gewünschte Wertpapier und den Konnektor in der Hostanwendung.
3. Setze den `CandleType`-Parameter auf den Zeitrahmen, den du handeln möchtest. Er sollte einem verfügbaren Kerzenabonnement für das ausgewählte Instrument entsprechen.
4. Optional passe `Length` und `Volume` vor dem Start der Strategie oder durch Optimierung an.
5. Starte die Strategie. Orders werden generiert, sobald der Indikator gebildet ist und Echtzeit-Daten verfügbar sind.

## Implementierungshöhepunkte
- Verwendet die High-Level `SubscribeCandles`-API mit Indikator-Binding (`Bind`) für saubere ereignisgesteuerte Verarbeitung.
- Speichert die vorherigen Vortex-Werte, um Crossover genau wie die MQL-Implementierung zu erkennen (`VI+` und `VI-`-Vergleiche über zwei aufeinanderfolgende Kerzen).
- Einstiegs-Trigger sind als nullable Decimal-Felder implementiert, um den ursprünglichen "Armen und Brechen"-Mechanismus nachzuahmen.
- Englische Inline-Kommentare in der C#-Datei beschreiben jeden Entscheidungsschritt und helfen bei der Pflege des Codes.

## Mögliche Erweiterungen
- Stop-Loss- und Take-Profit-Regeln hinzufügen (z. B. ATR-basierte Ausstiege), wenn eine striktere Risikokontrolle erforderlich ist.
- Eine Abkühlzeit oder maximale Haltezeit einführen, um längere Flat-Phasen zu vermeiden, wenn Trigger nicht ausgeführt werden.
- Mit einem Volatilitätsfilter kombinieren, um nur dann zu handeln, wenn die Preisspannen breit genug sind, um Ausbruchsversuche zu rechtfertigen.
