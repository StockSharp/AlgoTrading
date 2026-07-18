# AutoTrading-Scheduler-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die AutoTrading Scheduler-Strategie repliziert den Expert Advisor von EarnForex MetaTrader, der den „AutoTrading“-Schalter von MetaTrader umschaltet. Der StockSharp-Port hält das Konto außerhalb benutzerdefinierter Zeitfenster flach und nimmt den Handel wieder auf, wenn die Uhr innerhalb eines zulässigen Intervalls zurückgeht. Die gesamte Konfiguration erfolgt über lesbare Zeichenfolgen, eine für jeden Wochentag.

Das Modul ist bewusst signalunabhängig: Es eröffnet keine neuen Trades von selbst. Stattdessen überwacht es den Handelsstatus der Host-Strategie. Wenn der Planer den automatischen Handel deaktiviert, storniert er alle aktiven Aufträge, reduziert optional die aktuelle Position und protokolliert das Ereignis über `AddInfoLog`, damit die Hostanwendung reagieren kann.

## Ursprüngliche Logik

* Lädt einen dauerhaften Zeitplan mit mehreren Zeitspannen pro Wochentag.
* Unterstützt lokale oder Broker/Server-Zeitbasen.
* Überprüft den Zeitplan jede Sekunde über einen internen Timer.
* Wenn die Uhr außerhalb jeder Spanne des aktuellen Wochentags steht, deaktiviert sie den automatischen Handel und kann optional alle offenen Geschäfte und ausstehenden Aufträge schließen.
* Aktiviert den automatischen Handel wieder, sobald die Uhr wieder eine zulässige Zeitspanne erreicht.

## Implementierungshinweise

* Die StockSharp-Version speichert den analysierten Zeitplan im Speicher und berechnet ihn jedes Mal neu, wenn der Benutzer einen der Textparameter bearbeitet.
* Zeitspannen akzeptieren mehrere Formate: `9-12`, `09:30-16:00`, `21.15-23.45`. Minuten sind optional und werden standardmäßig auf `00` gesetzt, wenn sie weggelassen werden. Trennen Sie mehrere Bereiche durch Kommas.
* Ein Bereich, dessen Ende `00:00` entspricht, bleibt bis Mitternacht aktiv (z. B. bedeutet `22-0` 22:00:00 bis 23:59:59). Durch die Verwendung von `0-0` bleibt der Handel den ganzen Tag lang aktiviert.
* Zeitspannen, deren Ende vor dem Beginn liegt, werden automatisch auf den nächsten Tag umgebrochen, was die Hilfslogik des ursprünglichen Expertenberaters widerspiegelt.
* Der Timer läuft alle fünf Sekunden, um Reaktionsfähigkeit und Ressourcennutzung auszugleichen.

## Parameter

| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `SchedulerEnabled` | `bool` | `false` | Hauptschalter, der den Stundenplan aktiviert. Wenn die Strategie deaktiviert ist, beeinträchtigt sie niemals den Handel. |
| `ReferenceClock` | `TimeReference` | `Local` | Wählt zwischen der lokalen Maschinenuhr und der vom Connector bereitgestellten Exchange-/Serverzeit. |
| `ClosePositionsBeforeDisable` | `bool` | `true` | Wenn der Planer den automatischen Handel deaktiviert, storniert er zunächst jede aktive Order und reduziert die aktuelle Position. |
| `MondaySchedule` | `string` | `""` | Durch Kommas getrennte Liste der Handelsintervalle für Montag. |
| `TuesdaySchedule` | `string` | `""` | Durch Kommas getrennte Liste der Handelsintervalle für Dienstag. |
| `WednesdaySchedule` | `string` | `""` | Durch Kommas getrennte Liste der Handelsintervalle für Mittwoch. |
| `ThursdaySchedule` | `string` | `""` | Durch Kommas getrennte Liste der Handelsintervalle für Donnerstag. |
| `FridaySchedule` | `string` | `""` | Durch Kommas getrennte Liste der Handelsintervalle für Freitag. |
| `SaturdaySchedule` | `string` | `""` | Durch Kommas getrennte Liste der Handelsintervalle für Samstag. |
| `SundaySchedule` | `string` | `""` | Durch Kommas getrennte Liste der Handelsintervalle für Sonntag. |

Alle Zeitplanparameter akzeptieren dieselbe Syntax. Beispiel: `"09-12, 13:30-17:45, 22-0"`.

## Nutzung

1. Hängen Sie die Strategie an das gewünschte Wertpapier oder Portfolio an.
2. Geben Sie einen oder mehrere Zeitbereiche für die Tage ein, an denen Sie handeln möchten. Lassen Sie einen Tag leer, um den Handel für den gesamten Tag zu verhindern.
3. Aktivieren Sie den Planer, indem Sie `SchedulerEnabled = true` festlegen.
4. Entscheiden Sie, ob Positionen mit `ClosePositionsBeforeDisable` automatisch reduziert werden sollen.
5. Überwachen Sie die Protokollausgabe: Jeder Schalter schreibt eine Nachricht mit dem Grund (Fenster geöffnet oder geschlossen).

Wenn die aktuelle Zeit innerhalb eines zulässigen Bereichs liegt, legt die Strategie `IsAutoTradingEnabled = true` fest. Außerhalb jedes Bereichs wechselt die Eigenschaft zu `false`, das Modul storniert Arbeitsaufträge, reduziert die Position (sofern konfiguriert) und protokolliert die Aktion.

## Bekannte Einschränkungen

* Die Strategie überwacht nur das einzelne damit verbundene Wertpapier. Portfolios mit mehreren Symbolen erfordern mehrere Scheduler-Instanzen oder einen benutzerdefinierten Koordinator.
* Das Timer-Intervall kann im Quellcode (`TimeSpan.FromSeconds(5)`) angepasst werden, wenn eine andere Granularität erforderlich ist.
* Die Strategie speichert den Zeitplan nicht auf der Festplatte. Verwenden Sie die Parameterspeichermechanismen der Hostanwendung, wenn Persistenz erforderlich ist.
