# IsConnected-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung
* **Quelle**: Konvertiert aus dem MetaTrader 5-Skript `IsConnected.mq5` (Ordner `MQL/35056`).
* **Zweck**: Überwacht kontinuierlich den Connector-Status und meldet Online-/Offline-Übergänge mit Zeitstempeln und Betriebs-/Ausfalldauern.
* **Typ**: Die Versorgungsstrategie konzentriert sich eher auf die Überwachung der Infrastruktur als auf die Auftragsausführung.

## Verhalten
1. Wenn die Strategie startet, protokolliert sie sofort, dass das Überwachungsmodul initialisiert wurde und erfasst den aktuellen Connector-Status.
2. Ein Hintergrund-Timer überprüft das Flag `Connector.IsConnected` alle `CheckIntervalSeconds` (Standard: 1 Sekunde).
3. Wenn sich der Zustand ändert, ist die Strategie:
   * Speichert den Zeitpunkt des Übergangs mithilfe der Strategie `CurrentTime`.
   * Protokolliert den neuen Status (`Online` oder `Offline`).
   * Meldet, wie lange der vorherige Zustand gedauert hat (Online-Zeit vor der Trennung oder Offline-Zeit vor der erneuten Verbindung).
4. Wenn die Strategie stoppt, bricht sie den Timer ab und protokolliert den letzten bekannten Status, sodass der Bediener weiß, ob die Verbindung beim Herunterfahren aktiv oder unterbrochen war.

## Parameter
| Name | Typ | Standard | Beschreibung |
|------|------|---------|-------------|
| `CheckIntervalSeconds` | `int` | `1` | Intervall (in Sekunden) zwischen aufeinanderfolgenden Verbindungsprüfungen. Muss größer als Null sein. |

## Protokollierungsdetails
* Alle Nachrichten werden mit `LogInfo` auf Englisch geschrieben, um der MetaTrader-Implementierung zu entsprechen, die auf `Print`-Anweisungen beruhte.
* Zeitintervalle werden mithilfe der invarianten Kultur formatiert und umfassen sowohl Startzeitstempel als auch die im vorherigen Zustand verbrachte Zeit.

## Unterschiede zum Originalskript
* Die beschäftigte Warteschleife von MQL5 wird durch einen verwalteten Timer ersetzt, der den Strategie-Thread nicht blockiert.
* Anstatt doppelte Statuszeilen zu drucken, meldet die StockSharp-Version strukturierte Statusänderungen zusammen mit Betriebszeit-/Ausfallzeitmetriken.
* Die Konvertierung sorgt für eine ordnungsgemäße Entsorgung, indem der Timer sowohl in `OnStopped` als auch in `OnReseted` gestoppt wird.
