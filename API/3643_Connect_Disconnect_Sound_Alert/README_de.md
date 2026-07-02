# Strategie für akustische Warnungen bei Verbindung und Trennung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Connect Disconnect Sound Alert Strategy** überwacht kontinuierlich den Verbindungsstatus des Strategie-Connectors und protokolliert jeden Übergang zwischen Online- und Offline-Status. Der ursprüngliche MQL5-Experte spielte Audiodateien ab, wenn das MetaTrader-Terminal angeschlossen oder getrennt wurde. Diese C#-Konvertierung behält die Kernlogik – das Erkennen von Verbindungsänderungen – bei und macht Hooks verfügbar, die es der StockSharp-Laufzeit ermöglichen, Ereignisse und Dauer aufzuzeichnen. Die Strategie kann als leichter Watchdog verwendet werden, der den Betreiber über Verbindungsprobleme informiert, ohne dass er Befehle erteilen muss.

## Hauptmerkmale
- Fragt den Connector-Status regelmäßig in einem konfigurierbaren Intervall ab.
- Erkennt sowohl Verbindungs- als auch Trennungsereignisse und schreibt detaillierte Protokolleinträge.
- Zeichnet auf, wie lange das Terminal online oder offline blieb (optional).
- Überspringt Benachrichtigungstöne bei der allerersten Prüfung, um das Verhalten von MQL widerzuspiegeln.

## Parameter
| Name | Standard | Beschreibung |
| ---- | ------- | ----------- |
| `CheckIntervalSeconds` | `1` | Anzahl der Sekunden zwischen den Connector-Statusprüfungen. Muss größer als Null sein. |
| `LogDurations` | `true` | Wenn die Strategie aktiviert ist, protokolliert sie die Zeitspanne, in der die Verbindung nach jedem Übergang online oder offline blieb. |

Alle Parameter werden über `StrategyParam<T>` verfügbar gemacht, sodass sie über die Benutzeroberfläche oder während der Optimierung geändert werden können.

## Wie es funktioniert
1. Wenn die Strategie startet, speichert sie den aktuellen Connector-Status und protokolliert optional den Anfangsstatus.
2. Ein `System.Threading.Timer` ruft regelmäßig einen internen Handler auf, der das aktuelle Verbindungsflag mit dem vorherigen Wert vergleicht.
3. Wenn sich der Zustand ändert, protokolliert die Strategie den Übergang. Die allererste Benachrichtigung ist als „initial“ gekennzeichnet und stellt keinen tatsächlichen akustischen Alarm dar (entspricht der ursprünglichen Expert Advisor-Logik).
4. Optionale Dauerprotokolle zeigen, wie lange der vorherige Zustand gedauert hat, und helfen dem Betreiber, die Verbindungsstabilität zu beurteilen.
5. Der Timer wird automatisch entsorgt, wenn die Strategie stoppt oder zurückgesetzt wird.

## Nutzungshinweise
- Hängen Sie die Strategie an jedes Connector-fähige StockSharp-Terminal an. Es interagiert nicht mit Marktdaten und erteilt keine Aufträge.
- Behalten Sie das Standardabfrageintervall bei, um eine Überwachung nahezu in Echtzeit zu ermöglichen. Erhöhen Sie den Wert, wenn Sie nur grobe Aktualisierungen benötigen.
- Die Strategie verwendet das Protokollierungssubsystem StockSharp (`LogInfo`). Konfigurieren Sie Protokoll-Listener oder Dashboards, um die Benachrichtigungen anzuzeigen.
- Um tatsächliche akustische Warnungen hinzuzufügen, verbinden Sie einen Benachrichtigungsdienst in Ihrer Hostanwendung und spielen Sie Audio ab, wenn Protokollmeldungen eingehen.

## Sicherheitsüberlegungen
- Die Strategie validiert das Abfrageintervall und löst eine Ausnahme aus, wenn es nicht positiv ist.
- Timer-Rückrufe verwenden die Strategie `CurrentTime`, um konsistente Zeitstempel sicherzustellen, selbst wenn die Wiedergabe historischer Daten verwendet wird.
- Alle Ressourcen werden beim Stoppen/Zurücksetzen freigegeben, um Hintergrundtimer nach der Deaktivierung der Strategie zu vermeiden.
