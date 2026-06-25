# Trendlinien-Kreuzungs-Alert-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie reproduziert das Verhalten des ursprünglichen MetaTrader-Expert-Advisors, der auf Preiskreuzungen manuell gezeichneter horizontaler Linien und Trendlinien achtete. Sie überwacht kontinuierlich abgeschlossene Kerzen, prüft ob der Kerzenkörper ein registriertes Niveau überspannt hat, und generiert beim ersten Auftreten einer Kreuzung Alerts. Standardmäßig werden keine automatischen Orders übermittelt; das Modul konzentriert sich auf die Verfolgung diskretionärer Niveaus und die Information des Operators.

## Konvertierungs-Highlights
- Es werden nur Linien berücksichtigt, die mit dem *Überwachungsfarbe*-Wert getaggt sind, und spiegeln den ursprünglichen EA wider, der Objekte nach Farbe filterte.
- Sobald eine Kreuzung erkannt wird, wird die Linie intern markiert, sodass nachfolgende Kerzen keine doppelten Alerts auslösen. Dies spiegelt das Umfärben des Objekts zur `CrossedColor`-Eingabe in MetaTrader wider.
- Da StockSharp keine Chart-Objekte vom Terminal exponiert, werden Niveaus durch Textparameter definiert. Horizontale Einträge werden aus `Name|Color|Price`-Blöcken geparst, während Trendlinien `Name|Color|StartTime|StartPrice|EndTime|EndPrice` verwenden und als unendliche Linien zwischen den zwei Ankerpunkten ausgewertet werden.
- Alert-, Push-Benachrichtigungs- und E-Mail-Optionen werden auf informative Protokolleinträge abgebildet, sodass der Workflow auch ohne plattformspezifische Benachrichtigungskanäle transparent bleibt.

## Parameter
| Parameter | Typ | Beschreibung |
| --- | --- | --- |
| `MonitoringColor` | `string` | Farbbezeichnung, die Linien entsprechen müssen, um überwacht zu werden. Groß-/Kleinschreibung unempfindlich. |
| `CrossedColor` | `string` | Bezeichnung in Alert-Nachrichten, die anzeigt, dass die Linie gekreuzt wurde. |
| `HorizontalLevelsInput` | `string` | Semikolon-getrennte Liste horizontaler Niveaus. Jeder Eintrag ist `Name|Color|Price`; wenn die Farbe weggelassen wird, wird die Überwachungsfarbe angenommen. |
| `TrendlineDefinitions` | `string` | Semikolon-getrennte Liste von Trendlinien. Jeder Eintrag ist `Name|Color|StartTime|StartPrice|EndTime|EndPrice`. Zeiten müssen im ISO 8601-Format vorliegen und die Zeitzone des Handelskalenders verwenden. |
| `EnableAlerts` | `bool` | Wenn `true` schreibt die Strategie einen Info-Protokolleintrag, der die Kreuzung beschreibt. |
| `EnableNotifications` | `bool` | Fügt einen zweiten Protokolleintrag hinzu, der eine Push-Benachrichtigung emuliert. |
| `EnableEmails` | `bool` | Fügt einen dritten Protokolleintrag hinzu, der einen E-Mail-Alert emuliert. |
| `CandleType` | `DataType` | Kerzenserie zur Marktüberwachung. |

## Definitionsformat
1. Mehrere Einträge mit Semikolon (`;`) trennen.
2. Horizontale Niveaus können Name oder Farbe weglassen:
   - `1.1050` → überwacht als `Horizontal 1` bei Preis `1.1050` mit der Überwachungsfarbe.
   - `Resistance|1.1180` → benutzerdefinierter Name, noch mit der Überwachungsfarbe.
   - `Breakout|Blue|1.1225` → benutzerdefinierte Farbe muss noch mit `MonitoringColor` übereinstimmen, um verfolgt zu werden.
3. Trendlinien erfordern zwei Ankerpunkte mit ISO 8601-Zeitstempeln (`2024-03-15T10:00:00Z`). Fehlende Farbwerte werden auf die Überwachungsfarbe voreingestellt. Linien werden über die Anker hinaus extrapoliert, genau wie MetaTrader-Trendlinien.

## Ausführungsfluss
1. Während `OnStarted` werden die Textdefinitionen in stark typisierte Strukturen geparst und im Speicher gespeichert.
2. Abgeschlossene Kerzen aus dem konfigurierten Abonnement lösen `ProcessCandle` aus.
3. Die Methode prüft ob die Kerze auf einer Seite eines Niveaus geöffnet und auf der anderen Seite geschlossen wurde. Falls ja, wird die Linie als gekreuzt markiert und eine Nachricht generiert.
4. Nachrichten enthalten die Kreuzungsrichtung, den theoretischen Linienpreis und den Schlusskurs, damit diskretionäre Händler manuell reagieren können.

## Benachrichtigungen
StockSharp-Strategien emittieren Protokollnachrichten anstelle von UI-Pop-ups. Jeder aktivierte Benachrichtigungskanal erzeugt einen separaten Protokolleintrag, was der Host-Anwendung ermöglicht, sie an tatsächliche Alertsysteme weiterzuleiten, wenn benötigt.

## Verwendungscheckliste
1. Das Instrument und den Zeitrahmen auswählen, dann `CandleType` entsprechend setzen.
2. `HorizontalLevelsInput` und `TrendlineDefinitions` mit den in Ihrem MetaTrader-Workspace gezeichneten Linien (oder benutzerdefinierten Werten) füllen.
3. Die Benachrichtigungs-Booleans an die gewünschten Alert-Kanäle anpassen.
4. Die Strategie starten. Das Charting-Subsystem kann für manuelles Zeichnen von Linien verwendet werden, falls gewünscht; dieses Modul konzentriert sich auf die Erkennung.

## Beispielkonfiguration
```
MonitoringColor = "Yellow"
CrossedColor = "Green"
HorizontalLevelsInput = "DailyPivot|Yellow|1.1025;WeeklyHigh|Yellow|1.1100"
TrendlineDefinitions = "UpperChannel|Yellow|2024-03-14T08:00:00Z|1.0950|2024-03-14T16:00:00Z|1.1080"
EnableAlerts = true
EnableNotifications = true
EnableEmails = false
CandleType = 15 minute candles
```
Diese Einrichtung überwacht zwei statische Niveaus und eine aufsteigende Trendlinie. Eine Nachricht wie `Price crossed horizontal line 'DailyPivot' upward at 1.10250 ...` wird beim ersten Mal geschrieben, wenn ein Schlusskurs jedes Niveau passiert.

## Risikomanagement und Erweiterungen
- Die Strategie modifiziert keine Positionen. Kombinieren Sie sie mit separater Ausführungslogik wenn automatisches Trading erforderlich ist.
- Um Alerts zurückzusetzen, die Strategie stoppen und neu starten oder die Definitionsstrings anpassen. Das Persistieren des `HashSet`-Zustands wird absichtlich vermieden, um nahe am ursprünglichen EA-Verhalten zu bleiben.
- Zusätzliche Sicherheitsmaßnahmen wie Sitzungsfilter oder Volatilitätsprüfungen können durch Erweiterung der `ProcessCandle`-Methode hinzugefügt werden.
