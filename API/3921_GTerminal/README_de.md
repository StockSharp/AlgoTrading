# GTerminal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die GTerminal-Strategie ist eine C#-Portierung des MetaTrader 4 Expert Advisors `GTerminal_V5a`. Das ursprüngliche Skript erlaubte manuelles Arbeiten
Kontrolle von Ein- und Ausgängen durch Zeichnen horizontaler Linien auf dem Diagramm. Dieser Port stellt im Inneren dasselbe zeilengesteuerte Verhalten wieder her
das StockSharp-Framework, indem jede virtuelle Linie als konfigurierbarer Parameter verfügbar gemacht wird. Immer wenn der Schlusskurs des ausgewählten
Wenn eine Kerzenserie eine dieser virtuellen Linien kreuzt, öffnet, schließt oder kehrt die Strategie Positionen auf die gleiche Weise wie die MQL4 um.
Version. Optionale automatische Schutzstufen emulieren die Hilfszeilen „tpinit“ und „slinit“ des Originaltools.

## Strategielogik
### Preisbeispiele
* Die Strategie funktioniert auf fertige Kerzen eines benutzerdefinierten Zeitrahmens (`CandleType`).
* `StartShift` steuert, welche Kerze als Referenzschluss verwendet wird. Ein Wert von `0` verwendet den aktuellen Kerzenschluss, `1` verwendet den
vorherige Kerze usw. Die Verschiebung wirkt sich auch auf die Vergleichskerze aus, sodass das Skript immer zwei aufeinanderfolgende Schlusskurse wie die auswertet
MetaTrader-Implementierung.
* `CrossMethod` spiegelt die Eingabe von MQL4 wider:
  * `0` – strikte Kreuzung: Der vorherige Schlusskurs muss unter (bei langen Auslösern) oder über (bei kurzen Auslösern) dem Niveau und dem liegen
Der aktuelle Abschluss muss auf der gegenüberliegenden Seite des Levels enden.
  * `1` – sofortiger Auslöser: Der aktuelle Schlusskurs muss nur über/unter dem Niveau liegen. Der Port prüft weiterhin die vorherige Nähe
Verhindern Sie mehrere Auslöser auf derselben Leiste, indem Sie das „Einmal berühren“-Verhalten reproduzieren, das in MetaTrader durch Löschen der Zeile erhalten wurde
nachdem es abgefeuert wurde.

### Einreisebestimmungen
* **Kauf-Stopp-Linie** – wenn sich der Schlusskurs von unter `BuyStopLevel` auf über `BuyStopLevel` bewegt, kauft die Strategie. Wenn eine Short-Position offen ist,
Die Auftragsgröße umfasst das zum Glätten der Short-Position erforderliche Volumen zuzüglich der konfigurierten `Volume` für die neue Long-Position.
* **Kauflimitlinie** – wenn der Schlusskurs unter `BuyLimitLevel` fällt, wird eine Long-Position mit derselben Volumenlogik eröffnet.
* **Verkaufsstopplinie** – wenn sich der Schlusskurs von über nach unter `SellStopLevel` bewegt, wird die Strategie verkauft. Bestehende Long-Positionen werden als geschlossen
Teil der Bestellmenge.
* **Verkaufslimitlinie** – wenn der Schlusskurs über `SellLimitLevel` steigt, wird eine Short-Position eröffnet.
* Einträge werden ignoriert, wenn `Volume` den Wert `0` hat oder `PauseTrading` aktiviert ist.

### Ausgangsregeln
* **Richtungsausgänge** – `LongStopLevel` und `LongTakeProfitLevel` schließen die lange Seite, wenn der Schluss die entsprechende kreuzt
Linie. `ShortStopLevel` und `ShortTakeProfitLevel` machen dasselbe für eine Kurzbelichtung.
* **Globale Exits** – `AllLongStopLevel` / `AllLongTakeProfitLevel` liquidieren jede Long-Position, unabhängig davon, wie sie eröffnet wurde.
`AllShortStopLevel` / `AllShortTakeProfitLevel` spiegeln die Logik für Kurzfilme wider.
* **Anfänglicher Schutz** – Wenn Sie `UseInitialProtection` auf `true` setzen, werden `InitialLongStopLevel`, `InitialLongTakeProfitLevel` angewendet.
`InitialShortStopLevel` und `InitialShortTakeProfitLevel` unmittelbar nachdem eine neue Position besetzt ist. Diese Ebenen verhalten sich wie die
„slinit“ / „tpinit“ Hilfszeilen aus dem Originalskript und bleiben aktiv, bis die Position geschlossen oder das Level aktualisiert wird.
* Pro Kerze wird nur eine Exit-Aktion übermittelt. Wenn eine Ausstiegsbedingung erfüllt ist, sendet die Strategie den Abschlussauftrag und überspringt den
verbleibende Prüfungen für diesen Balken, genau wie die MQL4-Version nach dem Auslösen der Zeile gestoppt wurde.

### Steuerung unterbrechen
* `PauseTrading` reproduziert die Funktionalität der MetaTrader „PAUSE“-Zeile. Wenn diese Option aktiviert ist, wird keine Ein- oder Ausstiegslogik ausgewertet.
Der Status kann manuell umgeschaltet werden, ohne die Strategie neu zu laden.

## Parameter
* **Volumen** – Bestellvolumen für Neuzugänge. Die endgültige Auftragsgröße umfasst automatisch alle erforderlichen Gegenpositionen
während einer Stornierung geschlossen.
* **Kreuzungsmethode** – Wählen Sie den Kreuzungsalgorithmus aus (`0` streng, `1` sofort).
* **Start Shift** – Kerzenversatz, der für die Kreuzungsberechnung verwendet wird.
* **Handel pausieren** – deaktiviert alle Handelsaktionen während `true`.
* **Anfänglichen Schutz verwenden** – ermöglicht die automatische Anwendung der anfänglichen Stop-/Take-Profit-Levels nach jeder Füllung.
* **Buy Stop Level / Buy Limit Level** – Preisniveaus, die Long-Einstiege auslösen.
* **Verkaufsstopp-Level / Verkaufslimit-Level** – Preisniveaus, die Short-Einstiege auslösen.
* **Long Stop Level / Long Take Profit** – Ausstiegslinien für die aktive Long-Position.
* **Short Stop Level / Short Take Profit** – Ausstiegslinien für die aktive Short-Position.
* **All Long Stop / All Long Take Profit** – globale Ausstiegslinien, die jede Long-Position schließen.
* **All Short Stop / All Short Take Profit** – globale Ausstiegslinien, die jede Short-Position schließen.
* **Initial Long Stop / Initial Long Take Profit** – Schutzniveaus werden nach jedem Long-Einstieg aktiviert, wenn der anfängliche Schutz aktiviert ist
aktiviert.
* **Initial Short Stop / Initial Short Take Profit** – Schutzniveaus werden nach jedem Short-Einstieg aktiviert, wenn der anfängliche Schutz aktiviert ist
aktiviert.
* **Kerzentyp** – Zeitrahmen, der die für Vergleiche verwendeten Schlusskurse liefert.

## Hinweise zur Implementierung
* Der Port behält den linienbasierten Workflow bei, stellt jedoch jede Linie als Parameter bereit, anstatt sich auf Diagrammobjekte zu verlassen. Benutzer können
Aktualisieren Sie Ebenen im Handumdrehen durch das Parameterraster und ahmen Sie so die Art und Weise nach, wie Linien in einem MetaTrader-Diagramm verschoben wurden.
* Indikatorfenster-Trigger aus dem Originalskript (RSI, CCI, Momentum usw.) sind in dieser Version nicht verfügbar. Alle Auslöser
Verwenden Sie nur Schlusskurse. Der Parametersatz kann bei indikatorgesteuertem Verhalten weiterhin mit anderen StockSharp-Komponenten kombiniert werden
ist erforderlich.
* Die Strategie basiert ausschließlich auf Marktaufträgen (`BuyMarket`, `SellMarket`), genau wie das MQL4-Skript, das dazu Marktaufträge verwendete
Emulieren Sie die ausstehende Zeilenausführung.
* Es gibt keine Python-Implementierung; In diesem Paket wird nur die C#-Version bereitgestellt.
