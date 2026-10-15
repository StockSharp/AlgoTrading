# TimeEA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **TimeEA-Strategie** repliziert den ursprünglichen MetaTrader-Expertenberater "TimeEA" in StockSharp. Sie verwaltet eine einzelne Position ausschließlich basierend auf der Tageszeit: Sie öffnet zu einem konfigurierten Zeitpunkt, hält die Position in einer fixen Richtung und schließt sie entweder zu einer geplanten Abschlusszeit oder sobald optionale Stop-Loss/Take-Profit-Level überschritten werden.

Im Gegensatz zu indikatorbasierten Systemen konzentriert sich diese Implementierung auf diszipliniertes Sitzungsmanagement. Sie gewährleistet nur einen Einstieg pro Handelstag, bereinigt entgegengesetzte Exposition vor dem Öffnen und erzwingt konfigurierbare Mindestabstände für Schutzaufträge, um die Stop-Level-Beschränkungen eines Brokers nachzuahmen.

## Funktionsweise

1. Die Strategie abonniert eine konfigurierbare Kerzenserie (standardmäßig 1 Minute) und bewertet nur abgeschlossene Kerzen.
2. Wenn der Schluss einer Kerze die konfigurierte **Eröffnungszeit** kreuzt, führt die Strategie aus:
   - Schließt jede entgegengesetzte Position, die möglicherweise noch offen ist.
   - Platziert einen Marktauftrag in der gewählten Richtung (Kauf oder Verkauf) mit dem angegebenen Volumen.
   - Zeichnet Stop-Loss- und Take-Profit-Preise in Punkten (Preisschritte) vom Einstieg auf, wobei der Mindestabstands-Multiplikator angewendet wird.
3. Während der Sitzung überwacht die Strategie Kerzen:
   - Wenn eine Kerze das gespeicherte Stop-Loss- oder Take-Profit-Level berührt, wird die Position sofort geschlossen.
   - Wenn die Kerze das **Schlusszeit**-Fenster kreuzt, wird die Position unabhängig von Gewinn oder Verlust abgeflacht.
4. Nach dem Schließen des Trades (durch Stop, Ziel oder Zeitplan) bleibt die Strategie flach bis zum nächsten Handelstag.

Dieser Ablauf reproduziert das "einmal täglich öffnen"-Verhalten der MetaTrader-Version, die sich auf `TimeCurrent()` und `Time[0]`-Vergleiche stützte.

## Parameter

| Name | Beschreibung |
| --- | --- |
| **Open Time** | Tageszeit für das Öffnen des Trades. Akzeptiert `HH:MM:SS`. |
| **Close Time** | Tageszeit für das Abflachen aller Positionen. Kann am gleichen Tag sein oder in den nächsten Tag übergehen. |
| **Position Type** | Richtung der Position (`Buy` oder `Sell`). |
| **Order Volume** | Menge beim Senden des Marktauftrags. |
| **Stop Loss (points)** | Abstand in Preisschritten für den Schutz-Stop. Auf 0 setzen, um zu deaktivieren. |
| **Take Profit (points)** | Abstand in Preisschritten für das Gewinnziel. Auf 0 setzen, um zu deaktivieren. |
| **Minimum Distance Multiplier** | Minimaler Offset für Stop und Ziel (in Preisschritten) zur Emulation des ursprünglichen Stop-Level-Checks gegen den Spread. |
| **Candle Type** | Datenserie zur Erkennung von Zeitgrenzen. Standard sind 1-Minuten-Kerzen. |

## Praktische Hinweise

- **Einzelner Einstieg pro Tag** – Sobald die Eröffnungszeit ausgelöst wird, wird die Strategie nicht wieder einsteigen bis zum nächsten Kalendertag, auch wenn die Position früh gestoppt wurde.
- **Mitternacht-Unterstützung** – Sowohl Eröffnungs- als auch Schlusszeiten können vor oder nach Mitternacht festgelegt werden. Der Helfer respektiert Sitzungen, die über 00:00 Uhr hinausgehen.
- **Volumenhandling** – Marktaufträge respektieren den `Order Volume`-Parameter; an die Kontraktgröße des ausgewählten Instruments anpassen.
- **Stop-Level-Emulation** – Der Mindestabstands-Multiplikator stellt sicher, dass Stops/Ziele mindestens eine definierte Anzahl von Punkten vom Einstieg entfernt sind, was die ursprüngliche "Spread × Multiplikator"-Regel widerspiegelt.
- **Datenanforderungen** – Die Strategie ist auf konsistente Kerzen für das Timing angewiesen. Börsenlokale Zeitrahmen verwenden, um Zeitzonendrift zu vermeiden.
- **Risikomanagement** – Stops und Ziele werden intern gepflegt; es werden keine serverseitigen OCO-Aufträge erstellt. Wenn eine Kerze die Schwellen kreuzt, gibt die Strategie einen Marktauftrag zum Ausstieg aus.

## Anwendungsfälle

- Automatisierung sitzungsbasierter Einträge (z.B. Eröffnen von Positionen zur London- oder New-York-Eröffnung).
- Ausführung von Richtungsvoreingenommenheits-Strategien, bei denen die Richtung im Voraus bekannt ist, aber die Ausführung einem genauen Zeitplan folgen muss.
- Emulation von MetaTrader-Stil-Zeitauslösern innerhalb der StockSharp High-Level-API ohne manuelle Timer.

## Einschränkungen

- Slippage wird implizit durch Marktaufträge behandelt; es gibt keinen separaten Abweichungsparameter wie in MetaTrader.
- Der Mindestabstands-Multiplikator liest keine dynamischen Spreads; er erzwingt ein statisches Polster in Preisschritten.
- Die Strategie geht davon aus, dass pro Instanz nur ein Instrument/Wertpapier gehandelt wird.

## Erste Schritte

1. Strategieparameter im Designer oder über Code konfigurieren (Eröffnungs-/Schlusszeiten, Richtung, Volumen, Risikoabstände).
2. Strategie dem gewünschten Wertpapier und der Datenquelle zuordnen.
3. Sicherstellen, dass die Kerzenserie dieselbe Zeitzone wie der beabsichtigte Zeitplan verwendet.
4. Strategie ausführen und das Handelsprotokoll überwachen; visuelle Overlays können über `DrawCandles` und `DrawOwnTrades` bei Bedarf aktiviert werden.

Die Logik ist vollständig in `CS/TimeEaStrategy.cs` enthalten, mit ausführlichen Inline-Kommentaren, die jede Phase des Workflows erklären.
