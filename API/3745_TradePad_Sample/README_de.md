# TradePad-Beispielstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **TradePad-Beispielstrategie** ist eine Portierung des MetaTrader „TradePad“-Beispiels. Der ursprüngliche Fachberater hat ein Raster erstellt
Schaltflächen, die den kurzfristigen Trend für mehrere Symbole anzeigten, indem sie jede Zelle mit dem aktuellen Stochastic-Oszillator einfärbten
Lesen. Diese StockSharp-Implementierung behält den analytischen Kern der Stichprobe bei und konzentriert sich auf die Überwachung einer Liste von Instrumenten
ohne die Benutzeroberfläche auf dem Diagramm zu replizieren. Die Strategie abonniert Kerzendaten für jedes konfigurierte Symbol und berechnet a
Stochastic-Oszillator und klassifiziert jedes Instrument in die Zustände *Aufwärtstrend*, *Abwärtstrend* oder *Flat*. Jedes Mal, wenn die Klasse wechselt,
Die Strategie schreibt eine Protokollmeldung, die der vom ursprünglichen TradePad durchgeführten Farbänderung ähnelt.

Die Strategie erteilt keine Aufträge. Ziel ist es, diskretionären Händlern dabei zu helfen, den Überblick über mehrere Märkte gleichzeitig und vor Ort zu behalten
Momentumänderungen, die manuelle Maßnahmen erfordern (zum Beispiel das Wechseln von Charts oder das Vorbereiten von Trades).

## Wie es funktioniert

1. **Symbolerkennung** – der Parameter `SymbolList` akzeptiert eine durch Kommas getrennte Liste von Tickern. Wenn keine Liste angegeben wird, wird die
Die Strategie greift auf die im Runner zugewiesene Hauptstrategie `Security` zurück.
2. **Kerzenabonnement** – jedes Symbol verwendet denselben Zeitrahmen, der über `CandleType` konfiguriert wurde.
3. **Indikatorverarbeitung** – eine dedizierte `StochasticOscillator`-Instanz ist an den Kerzenstream gebunden. Wenn die Kerze ist
Wenn der Indikator fertig ist, erzeugt er den `%K`-Wert, der für die Trendklassifizierung verwendet wird.
4. **Trendklassifizierung** – ein Messwert über `UpperLevel` wird *Aufwärtstrend* zugeordnet, ein Messwert unter `LowerLevel` wird *Abwärtstrend* zugeordnet,
alles dazwischen ist *Flach*. Der letzte Oszillatorwert wird in `LatestKValues` gespeichert.
5. **Aktualisierungsintervall** – Die Strategie ahmt das Timer-Verhalten des ursprünglichen TradePad nach. Eine Änderung wird höchstens einmal pro Protokoll protokolliert
`TimerPeriodSeconds` für jedes Symbol, auch wenn innerhalb des Intervalls mehrere Kerzen eintreffen.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `SymbolList` | Durch Kommas getrennte Liste der zu überwachenden Instrumente. Eine leere Zeichenfolge bedeutet „Hauptsicherheit verwenden“. |
| `TimerPeriodSeconds` | Mindestanzahl von Sekunden zwischen Statusaktualisierungen pro Symbol. Verhindert Protokoll-Spam, wenn die Kerzen sehr kurz sind. |
| `StochasticLength` | Lookback-Zeitraum, der zur Berechnung der Rohzeile `%K` verwendet wird. |
| `StochasticKPeriod` | Auf die Linie `%K` angewendeter Glättungszeitraum. |
| `StochasticDPeriod` | Auf die Zeile `%D` angewendeter Glättungszeitraum (wird der Vollständigkeit halber beibehalten, obwohl die Strategie nur `%K` lautet). |
| `UpperLevel` | Schwellenwert, oberhalb dessen davon ausgegangen wird, dass sich das Symbol in einem Aufwärtstrend befindet. |
| `LowerLevel` | Schwellenwert, unterhalb dessen davon ausgegangen wird, dass sich das Symbol in einem Abwärtstrend befindet. |
| `CandleType` | Zeitrahmen der für die Indikatorberechnung verwendeten Kerzen. |

## Nutzungshinweise

- Stellen Sie sicher, dass die angegebenen Ticker im Connector verfügbar sind. Fehlende Symbole werden im Protokoll gemeldet und übersprungen.
- Die Eigenschaft `TrendStates` stellt die neueste Klassifizierung für externe Dashboards oder Designer-Blöcke bereit.
- Verwenden Sie die Strategie in Designer oder Runner, um Ihre eigenen visuellen Elemente (Dashboards, Diagramme) anzuhängen, die auf die `AddInfoLog` reagieren.
Nachrichten oder die öffentlichen Wörterbücher.
- Da keine Orders gesendet werden, kann die Strategie sicher auf Live-Datenanbietern ausschließlich zu Überwachungszwecken angewendet werden.

## Ursprüngliches MQL-Verhalten im Vergleich zur StockSharp-Version

| MQL5 Funktion | StockSharp Implementierung |
|--------------|--------------------------|
| Grafisches Raster von Schaltflächen | Wird als Protokolleinträge und öffentliche Wörterbücher verfügbar gemacht, damit eine benutzerdefinierte Benutzeroberfläche in Designer erstellt werden kann. |
| Manuelle KAUF-/VERKAUF-Tasten | Nicht implementiert; Die Strategie bleibt bewusst passiv. |
| Logik zum Ziehen von Diagrammen | Nicht anwendbar in StockSharp und weggelassen. |
| Trendfarben-Updates | Ersetzt durch Trendzustandsänderungen, die alle `TimerPeriodSeconds` pro Symbol ausgelöst werden. |

## Erweiterung der Strategie

- Verbinden Sie das `TrendStates`-Wörterbuch mit Designer-Widgets, um das farbige Pad mithilfe von XAML-Steuerelementen neu zu erstellen.
- Fügen Sie Warnungen oder Benachrichtigungen hinzu, wenn ein Symbol von *Flat* zu *Uptrend* oder *Downtrend* wechselt.
- Kombinieren Sie die Klassifizierung mit der Bestelllogik, wenn Sie Eingaben automatisieren möchten, nachdem Sie eine starke Dynamik festgestellt haben.
