# Cross Line Trader Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie emuliert den originalen MetaTrader-Expert "Cross Line Trader", indem sie auf Preisinteraktionen mit benutzerdefinierten synthetischen Linien reagiert. Anstatt auf manuelle Chart-Objekte zu hören, empfängt die StockSharp-Version alle Linienbeschreibungen über einen einzigen Parameter, analysiert diese beim Start und überwacht kontinuierlich abgeschlossene Kerzen. Wenn sich ein Kerzenöffnungspreis durch eine aktive Linie bewegt, platziert die Strategie eine Marktorder in der entsprechenden Richtung und deaktiviert diese Linie, damit sie nicht erneut ausgelöst werden kann.

## Handelslogik
1. Die Strategie abonniert den im Parameter **Candle Type** ausgewählten Kerzentyp und verarbeitet nur Kerzen im `Finished`-Zustand, um Intrabar-Rauschen zu vermeiden.
2. Synthetische Linien werden aus dem Parameter **Line Definitions** erstellt. Jede Linie hält ihren eigenen Zustand (aktiv/abgelaufen, Anzahl der verarbeiteten Bars und Geometrie).
3. Bei **Trend**- oder **Horizontal**-Linien vergleicht der Algorithmus die vorherige Kerzenöffnung mit der nächsten relativ zur Preisverlaufsbahn der Linie:
   - Ein Long-Signal tritt auf, wenn die vorherige Öffnung unter der Linie liegt und die aktuelle Öffnung darüber wechselt.
   - Ein Short-Signal tritt auf, wenn die vorherige Öffnung über der Linie liegt und die aktuelle Öffnung darunter wechselt.
4. **Vertical**-Linien funktionieren wie zeitgesteuerte Auslöser. Sobald die konfigurierte Anzahl von Bars abgelaufen ist, eröffnet die Strategie sofort eine Position zum aktuellen Kerzenöffnungspreis.
5. Die Richtung wird gemäß **Direction Mode** aufgelöst:
   - `FromLabel` vergleicht jede Linienbezeichnung mit **Buy Label** und **Sell Label**.
   - `ForceBuy` und `ForceSell` behandeln alle Linien als dieselbe Richtung unabhängig von Bezeichnungen.
6. Jeder erfolgreiche Auslöser sendet eine Marktorder mit dem Volumen aus **Trade Volume**, protokolliert die Aktivierung und markiert die Linie als inaktiv.
7. Optionale Stop-Loss- und Take-Profit-Abstände werden auf jeder neuen Kerze angewendet, indem der letzte Einstiegspreis gegen Kerzenhochs und -tiefs bewertet wird.

## Format der Liniendefinition
Der **Line Definitions**-String verwendet Semikolons zur Trennung von Einträgen. Jeder Eintrag muss folgendes Format haben:

```
Name|Type|Label|BasePrice|SlopePerBar|Length|Ray
```

- **Name** – In Protokollen angezeigter Bezeichner. Beliebige Zeichenkette ohne Semikolons.
- **Type** – `Horizontal`, `Trend` oder `Vertical` (Groß-/Kleinschreibung ignoriert).
- **Label** – Freitext, der verwendet wird, wenn **Direction Mode** `FromLabel` ist.
- **BasePrice** – Anfangspreis der Linie bei der ersten verarbeiteten Kerze. Erforderlich für jede nicht-vertikale Linie (dezimal, invariante Kultur).
- **SlopePerBar** – Preisänderung pro Kerze für eine Trendlinie. Verwenden Sie `0` für horizontale Linien.
- **Length** – Bedeutung hängt vom Linientyp ab:
  - Bei Trend- oder horizontalen Linien ohne Ray definiert es, wie viele Bars der rechte Anker vom Start entfernt ist. Nach diesem Zähler läuft die Linie automatisch ab.
  - Bei Ray-Linien wird der Wert ignoriert, da die Linie unbegrenzt weitergeht.
  - Bei vertikalen Linien gibt es an, wie viele Bars gewartet werden soll, bevor ausgelöst wird. Der minimale akzeptierte Wert ist `1`.
- **Ray** – `true` hält die Linie unbegrenzt nach rechts aktiv, `false` beschränkt sie auf die angegebene Länge.

Beispiel:

```
TrendLine|Trend|Buy|1.1000|0.0005|8|false;HorizontalSell|Horizontal|Sell|1.1050|0|0|true;VerticalImpulse|Vertical|Buy|0|0|1|false
```

Das Beispiel erstellt eine steigende Kauf-Trendlinie, ein horizontales Verkaufsniveau, das nie abläuft, und einen einmaligen vertikalen Auslöser für die nächste Kerze.

## Parameter
- **Candle Type** – Marktdatentyp für Berechnungen. Standard: 1-Minuten-Zeitrahmen.
- **Trade Volume** – Ordergröße für neue Einstiege. Muss positiv sein.
- **Direction Mode** – Bestimmt, wie die Einstiegsseite ausgewählt wird (`FromLabel`, `ForceBuy`, `ForceSell`).
- **Buy Label** / **Sell Label** – Bezeichnungswerte zur Identifizierung von Linien, wenn **Direction Mode** `FromLabel` ist.
- **Line Definitions** – Roher String, der jede synthetische Linie beschreibt (siehe Format oben).
- **Stop Loss Offset** – Abstand in Preiseinheiten für Schutzausstiege bei Long- und Short-Positionen (0 deaktiviert die Prüfung).
- **Take Profit Offset** – Preisabstand für Gewinnziele (0 deaktiviert die Prüfung).

## Risikomanagement
Die Strategie platziert keine separaten Stop- oder Take-Profit-Orders. Stattdessen überwacht sie jede abgeschlossene Kerze:
- Long-Positionen werden geschlossen, wenn das Kerzentief `EntryPrice - StopLossOffset` unterschreitet oder das Hoch `EntryPrice + TakeProfitOffset` überschreitet.
- Short-Positionen werden geschlossen, wenn das Kerzenhoch `EntryPrice + StopLossOffset` überschreitet oder das Tief unter `EntryPrice - TakeProfitOffset` fällt.

Wenn beide Offsets null sind, wird die Position nur durch das entgegengesetzte Signal oder manuelle Intervention geschlossen.

## Implementierungshinweise
- Alle Kommentare im Quellcode sind auf Englisch, um die Konsistenz mit den Projektrichtlinien zu wahren.
- Die Strategie ignoriert ungültige Liniendefinitionen stillschweigend; stellen Sie sicher, dass das Format korrekt ist, um fehlende Auslöser zu vermeiden.
- Das Neustarten der Strategie löscht den internen Zustand, sodass Linienzähler und Aktivierungstimer bei der ersten verarbeiteten Kerze neu beginnen.
- Der Ansatz konzentriert sich auf Kerzenöffnungspreise, genau wie der ursprüngliche EA, und reagiert nicht auf Intrabar-Berührungen.

## Verwendung
1. Das Handelsinstrument und den gewünschten Kerzentyp konfigurieren.
2. **Line Definitions** anpassen, um jede manuelle Linie zu beschreiben, gegen die gehandelt werden soll.
3. **Direction Mode** so einstellen, dass entweder Bezeichnungen verwendet werden oder einseitiger Handel erzwungen wird.
4. Optional Stop-Loss- und Take-Profit-Offsets für automatische Ausstiege festlegen.
5. Die Strategie starten und die Protokolle überwachen: Jede ausgelöste Linie wird zusammen mit ihrer Richtung und dem Aktivierungspreis gemeldet.
