# Heiken Ashi Idea Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Strategie reproduziert das Verhalten des ursprünglichen **HeikenAshiIdea.mq4**-Expertenberaters mithilfe der StockSharp High-Level-API. Sie wartet auf ausgerichtete bullische oder bärische Signale in zwei Zeitrahmen von Heikin Ashi-Kerzen und platziert dann ausstehende Limit-Orders in einem konfigurierbaren Abstand vom Markt. Das Ziel ist es, starke Fortsetzungsbewegungen zu erfassen, wenn die zuletzt gebildete Heikin Ashi-Kerze keinen Docht entgegen der Trendrichtung aufweist.

## Handelsstrategie

1. **Heikin Ashi-Rekonstruktion** – die Strategie rekonstruiert intern Heikin Ashi-Kerzen für den primären Handelszeitrahmen und für einen höheren Bestätigungszeitrahmen. Für jeden Zeitrahmen werden die letzten zwei Heikin Ashi-Kerzen gespeichert, damit Körperrichtung und Vorhandensein von Dochten analysiert werden können.
2. **Ausbruchsbedingung** – ein Long-Setup erscheint, wenn beide Zeitrahmen zeigen:
   - die zuletzt gebildete Heikin Ashi-Kerze ist bullisch und ihre Eröffnung entspricht dem Tief (kein unterer Schatten), und
   - die vorherige Heikin Ashi-Kerze ist ebenfalls bullisch, hat aber einen unteren Schatten.
   Ein Short-Setup erfordert die symmetrischen bärischen Bedingungen (kein oberer Schatten bei der letzten Kerze und ein oberer Schatten bei der vorherigen).
3. **ATR-Volatilitätsfilter** – die Average True Range mit konfigurierbarer Länge muss steigen (`ATR[t] > ATR[t-1]`), wenn der Filter aktiviert ist. Dies reproduziert die ursprüngliche `ActiveMarket`-Volatilitätsprüfung.
4. **Handelsfenster** – Signale außerhalb der benutzerdefinierten Handelssitzung werden ignoriert (Standard: 09:00–19:00).
5. **Orderplatzierung** – wenn ein Signal gültig ist, platziert die Strategie eine einzelne ausstehende Limit-Order:
   - Long-Signal → Kauf-Limit-Order bei `ClosePrice - DistancePoints * PriceStep`.
   - Short-Signal → Verkauf-Limit-Order bei `ClosePrice + DistancePoints * PriceStep`.
   Bestehende entgegengesetzte ausstehende Orders werden vor dem Einreihen einer neuen Order storniert. Die Strategie verfolgt pro Richtung nur eine ausstehende Order und löscht Referenzen automatisch, wenn die Order inaktiv wird.
6. **Positionsverwaltung** – optionale Take-Profit- und Stop-Loss-Abstände werden über `StartProtection` in StockSharp-Schutzmechanismen übersetzt. Wenn eine neue Kerze des „Close-All"-Zeitrahmens öffnet, storniert die Strategie alle ausstehenden Orders und schließt jede offene Position, wenn das Flag aktiviert ist. Dies imitiert das `UseCloseAll`-Verhalten des ursprünglichen EA.

## Risikomanagement

- Schutzlevel werden in Preisschritten (Punkten) ausgedrückt, um nahe an der MetaTrader-Implementierung zu bleiben. Sie sind optional; `0` deaktiviert den entsprechenden Schutz.
- Ausstehende Orders werden nur platziert, wenn der berechnete Abstand positiv ist und das Handelsvolumen über null liegt.
- Die Strategie mittelt Positionen nie automatisch; sie schließt zuerst die entgegengesetzte ausstehende Order, bevor eine neue geplant wird.
- Eine Toleranz von der Hälfte des Instrumentpreisschritts wird verwendet, wenn überprüft wird, ob Heikin Ashi-Kerzen Dochte haben oder nicht. Dies verhindert Gleitkomma-Rundungsfehler, während es den ursprünglichen strengen Vergleichen treu bleibt.

## Parameter

| Name | Beschreibung | Standardwert |
| --- | --- | --- |
| `DistancePoints` | Abstand in Preisschritten für die ausstehenden Limit-Orders. | `8` |
| `StopLossPoints` | Stop-Loss-Abstand in Preisschritten (0 deaktiviert den Stop). | `0` |
| `TakeProfitPoints` | Take-Profit-Abstand in Preisschritten (0 deaktiviert das Ziel). | `20` |
| `UseCloseAllOnNewBar` | Position schließen und Orders stornieren, wenn eine neue Kerze des Close-All-Zeitrahmens öffnet. | `true` |
| `CandleType` | Primärer Kerzentyp für Handelssignale. | `30m`-Zeitrahmen |
| `HigherCandleType` | Bestätigungs-Kerzentyp für den Multi-Zeitrahmen-Filter. | `1d`-Zeitrahmen |
| `CloseAllCandleType` | Kerzentyp, der die Close-All-Routine auslöst. | `7d`-Zeitrahmen |
| `StartHour` | Erste Stunde der Handelssitzung (inklusiv). | `9` |
| `EndHour` | Letzte Stunde der Handelssitzung (inklusiv). | `19` |
| `UseAtrFilter` | ATR-steigenden-Volatilitätsfilter aktivieren. | `true` |
| `AtrPeriod` | ATR-Periode für den Volatilitätsfilter. | `14` |

## Zusätzliche Hinweise

- Die Strategie verwendet die eingebaute `Volume`-Eigenschaft von `Strategy` als Basis-Ordergröße. Passen Sie diese vor dem Start der Strategie an.
- Da die StockSharp-Implementierung Kerzenschlusskurse für die Platzierung ausstehender Orders verwendet, kann die Live-Ausführung leicht vom ursprünglichen MT4-Code abweichen, der Bid/Ask-Kurse nutzte, aber die Kernidee bleibt erhalten.
- Um die Logik auf verschiedene Märkte auszuweiten, passen Sie einfach die Kerzentypen, das Handelsfenster und die Abstandsparameter an, während Sie die Multi-Zeitrahmen-Bestätigung beibehalten.
