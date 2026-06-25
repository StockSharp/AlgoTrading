# TradeEATemplateNews-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die TradeEATemplateNews-Strategie ist eine C#-Konvertierung des MetaTrader 4 Expert Advisors "Trade EA Template for News". Das ursprüngliche System pausierte den Handel rund um geplante wirtschaftliche Ereignisse, die von externen Websites heruntergeladen wurden. Dieser StockSharp-Port behält die Kernideen bei und passt sie an die High-Level-API an:

- Verwendet abgeschlossene Kerzen des konfigurierten Zeitrahmens (H1 standardmäßig).
- Handelt nur, wenn das Konto flat ist, genau wie die MQL-Vorlage, die null offene Orders erforderte.
- Wendet eine manuelle wirtschaftliche Nachrichtensperre an, die Einstiege vor und nach Ereignissen je nach ihrer Wichtigkeit blockiert.
- Erstellt automatisch schützende Stop-Loss- und Take-Profit-Brackets 100 Punkte vom Ausführungspreis entfernt (konvertiert über den Wertpapier-Schritt).

## Handelslogik
1. Jede abgeschlossene Kerze löst eine Neuberechnung des Nachrichtenplans aus. Die Strategie speichert den Eröffnungspreis der vorherigen Kerze, damit die nächste Bar ihren Schluss mit dem vorherigen Eröffnungspreis vergleichen kann.
2. Wenn die aktuelle Zeit in ein konfiguriertes Sperrzeitfenster fällt, storniert die Strategie ausstehende Orders und öffnet keine neuen Trades.
3. Wenn keine Position offen ist und der Handel erlaubt ist:
   - Eine Long-Position wird eröffnet, wenn die letzte Kerze über dem Eröffnungspreis der vorherigen Kerze schließt.
   - Eine Short-Position wird eröffnet, wenn die letzte Kerze unter dem Eröffnungspreis der vorherigen Kerze schließt.
4. Stop-Loss- und Take-Profit-Levels werden in Punkten ausgedrückt (`TakeProfitPoints` und `StopLossPoints`) und über den `Step`-Wert des Wertpapiers in absolute Preisverschiebungen umgewandelt.

## Manueller Nachrichtenplan
Der ursprüngliche Experte lud Daten von investing.com oder DailyFX herunter. Für Portabilität erwartet die StockSharp-Version einen manuell kuratierten Kalender, der über den Parameter `NewsEventsDefinition` bereitgestellt wird. Das Format akzeptiert eine Liste von Einträgen, getrennt durch Semikolons oder Zeilenumbrüche. Jeder Eintrag muss mindestens drei kommagetrennte Felder enthalten:

```
JJJJ-MM-TT HH:MM,WÄHRUNGEN,WICHTIGKEIT[,TITEL]
```

- `JJJJ-MM-TT HH:MM` — Ereignisstart in UTC. Der optionale Parameter `TimeZoneOffsetHours` verschiebt alle geparsten Zeiten um den angeforderten Betrag (setze zum Beispiel `3` für UTC+3).
- `WÄHRUNGEN` — Währungscodes oder Instrumentenidentifikatoren wie `USD`, `EUR`, `EUR/USD`. Mehrere Codes können mit `/`, `,`, `;`, `|` oder Leerzeichen getrennt werden.
- `WICHTIGKEIT` — Wichtigkeits-Schlüsselwort. Erkannte Werte: `Low`, `Medium`, `Mid`, `Midle`, `Moderate`, `High`, `NFP`, Strings, die `Nonfarm` oder `Non-farm` enthalten.
- `TITEL` — optionale Freitextbeschreibung, die in Protokollnachrichten gedruckt wird.

Beispiel:

```
2024-03-01 13:30,USD,High,Nonfarm Payrolls;2024-03-01 15:00,USD,Low,Factory Orders
```

### Sperrzeitfenster
- `UseLowNews`, `UseMediumNews`, `UseHighNews` und `UseNfpNews` schalten um, welche Ereignisse berücksichtigt werden.
- `LowMinutesBefore/After`, `MediumMinutesBefore/After`, `HighMinutesBefore/After` und `NfpMinutesBefore/After` bestimmen, wie viele Minuten rund um das Ereignis der Handel deaktiviert werden soll.
- `OnlySymbolNews` beschränkt die Sperre auf Einträge, deren Währungscodes mit dem aktuellen Wertpapier übereinstimmen (z. B. führt `EURUSD` zum Paar `{EUR, USD}`). Deaktiviere es, um den Handel bei jedem Ereignis zu pausieren.
- Die Strategie hält zu jedem Zeitpunkt nur das Ereignis mit der höchsten Wichtigkeit aktiv. Informationsprotokollnachrichten kündigen den Grund für den aktuellen Status und die nächste geplante Veröffentlichung an.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|--------------|----------|
| `CandleType` | Kerzen-Datentyp zum Abonnieren. Standardmäßig 1 Stunde. | `1h` |
| `UseLowNews` | Ereignisse mit geringer Wichtigkeit aktivieren. | `true` |
| `LowMinutesBefore` / `LowMinutesAfter` | Minuten vor/nach Nachrichten mit geringer Auswirkung zum Blockieren von Einstiegen. | `15 / 15` |
| `UseMediumNews` | Ereignisse mit mittlerer Wichtigkeit aktivieren. | `true` |
| `MediumMinutesBefore` / `MediumMinutesAfter` | Minuten vor/nach Nachrichten mit mittlerer Auswirkung. | `30 / 30` |
| `UseHighNews` | Ereignisse mit hoher Wichtigkeit aktivieren. | `true` |
| `HighMinutesBefore` / `HighMinutesAfter` | Minuten vor/nach Nachrichten mit hoher Auswirkung. | `60 / 60` |
| `UseNfpNews` | Das Non-farm Payrolls-Flag aktivieren. | `true` |
| `NfpMinutesBefore` / `NfpMinutesAfter` | Minuten vor/nach NFP-Ereignissen. | `180 / 180` |
| `OnlySymbolNews` | Den Kalender nach den Währungscodes des aktuellen Wertpapiers filtern. | `true` |
| `NewsEventsDefinition` | Manuelle wirtschaftliche Kalender-Beschreibungszeichenkette. | leer |
| `TimeZoneOffsetHours` | Versatz, der auf jedes geparste Ereignis angewendet wird (UTC standardmäßig). | `0` |
| `TakeProfitPoints` | Abstand in Punkten für die schützende Take-Profit-Order. | `100` |
| `StopLossPoints` | Abstand in Punkten für die schützende Stop-Loss-Order. | `100` |

`Volume` wird von `Strategy` geerbt und sollte gemäß der gewünschten Positionsgröße gesetzt werden.

## Unterschiede zur MQL-Version
- Kein automatischer HTTP-Download — der Benutzer liefert die Nachrichtenliste manuell, was externe Abhängigkeiten vermeidet und die Konvertierung deterministisch hält.
- Chart-Labels und vertikale Linien werden durch Protokollnachrichten ersetzt, die das aktive oder bevorstehende Ereignis beschreiben.
- Der MQL-Experte öffnete Orders mit fester Losgröße `0.01`; in StockSharp kommt die Positionsgröße aus der `Volume`-Eigenschaft.
- Die gesamte Logik ist mit der High-Level-Kerzenabonnement-API implementiert, während das nachrichtenbewusste Verhalten der Vorlage beibehalten wird.

## Bereitstellungshinweise
1. Fülle `NewsEventsDefinition` vor dem Start der Strategie aus oder aktualisiere es, stoppe und starte neu, um den Plan neu zu laden.
2. Passe `TimeZoneOffsetHours` und die Minuten-vor/nach-Parameter an deine Handelssitzung an.
3. Setze `Volume`, Portfolio und Wertpapier in der Benutzeroberfläche oder im Code, dann starte die Strategie.
4. Beobachte das Strategieprotokoll auf Nachrichten wie "Trading paused due to high news" oder "Next scheduled news", um die Sperrlogik zu bestätigen.

Die Python-Übersetzung wird absichtlich weggelassen, wie angefordert.
