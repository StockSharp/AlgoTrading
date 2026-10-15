# Strategie zur Symbolsynchronisierung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Symbol-Synchronisierungsstrategie** repliziert das MetaTrader-Dienstprogramm `SymbolSyncEA` innerhalb der StockSharp-Umgebung. Die Strategie synchronisiert das Hauptstrategiesymbol und alle registrierten verknüpften Strategien. Immer wenn sich das primäre Symbol ändert, überträgt die Strategie die neue Sicherheit automatisch auf jede verknüpfte Strategie und stellt so sicher, dass der gesamte Arbeitsbereich ohne manuelle Eingriffe demselben Instrument folgt.

## Kernideen
- Erfassen Sie die anfängliche Strategiesicherheit beim Start und verwenden Sie sie als Fallback-Option wieder.
- Führen Sie eine konfigurierbare Liste verknüpfter Strategien, die immer die Hauptsicherheit widerspiegeln sollten.
- Symboländerungen zulassen, die entweder durch eine direkte `Security`-Zuweisung oder durch Angabe einer neuen Sicherheitskennung ausgelöst werden.
- Stellen Sie manuelle Synchronisierungs- und Rücksetzvorgänge bereit, um dem ursprünglichen Verhalten des Expert Advisors zu entsprechen.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `ChartLimit` | Maximale Anzahl verknüpfter Strategien, die synchronisiert werden können. Verhindert versehentliche Massenaktualisierungen. | `10` |
| `SyncSecurityId` | Bezeichner der Sicherheit, die an verknüpfte Strategien weitergegeben wird. Ein leerer Wert fällt auf die Strategiesicherheit zurück. | `""` |

## Öffentliche Methoden
- `RegisterLinkedStrategy(Strategy strategy)` – fügt eine Strategieinstanz zur Synchronisierungsliste hinzu. Gibt bei erfolgreicher Registrierung `true` zurück.
- `UnregisterLinkedStrategy(Strategy strategy)` – entfernt eine Strategie aus der Liste.
- `ChangeSyncSecurity(Security security)` – wechselt zur bereitgestellten Sicherheitsinstanz und gibt sie an jede verknüpfte Strategie weiter.
- `ChangeSyncSecurity(string securityId)` – löst die Kennung durch den aktuellen `SecurityProvider` auf und ruft `ChangeSyncSecurity(Security)` auf.
- `ResetToInitialSecurity()` – stellt das beim Start erfasste Symbol wieder her.
- `SyncSymbols()` – erzwingt eine manuelle Neusynchronisierung, ohne die gespeicherte Kennung zu ändern.

## Nutzungsworkflow
1. Instanziieren Sie `SymbolSyncStrategy` und legen Sie den primären `Security` fest oder weisen Sie `SyncSecurityId` zu, bevor Sie mit der Strategie beginnen.
2. Rufen Sie `RegisterLinkedStrategy` für jede untergeordnete Strategie auf, die das aktive Symbol widerspiegeln muss (z. B. unterschiedliche Zeitrahmen oder Dashboards).
3. Wenn sich das Hauptsymbol ändern sollte, rufen Sie `ChangeSyncSecurity(Security)` oder `ChangeSyncSecurity(string)` auf.
4. Rufen Sie optional `SyncSymbols()` auf, um die Weitergabe zu erzwingen, wenn eine externe Komponente eine verknüpfte Strategie geändert hat.

## Unterschiede zur MQL-Version
- Funktioniert mit StockSharp `Strategy` Instanzen anstelle von MetaTrader Diagrammfenstern.
- Verwendet die `SecurityProvider`-Abstraktion, um Bezeichner aufzulösen.
- Fügt defensive Protokollierung und ein konfigurierbares Limit für synchronisierte Strategien hinzu.
- Bietet explizite Reset- und manuelle Synchronisierungsmethoden für erweiterte Automatisierungsszenarien.

## Notizen
- Die Strategie erteilt keine Marktaufträge; Es fungiert als Infrastrukturhelfer.
- Alle Codekommentare werden auf Englisch gehalten, um den Projektanforderungen zu entsprechen.
