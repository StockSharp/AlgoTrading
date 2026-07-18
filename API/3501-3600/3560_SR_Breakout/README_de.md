# SR-Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung
SR Breakout Strategy überwacht die von Donchian-Kanälen abgeleiteten Unterstützungs- und Widerstandsniveaus in zwei Zeitrahmen (H1 und H4). Wenn eine abgeschlossene Kerze über dem Widerstand oder unter der Unterstützung schließt, schreibt die Strategie eine informative Protokollmeldung. Die Implementierung spiegelt die Alarmierungslogik des ursprünglichen MQL4-Experten wider, ohne dass Aufträge erteilt werden müssen.

## Wie es funktioniert
1. Es werden zwei Kerzenabonnements erstellt: eines für den 1-Stunden-Zeitraum und eines für den 4-Stunden-Zeitraum.
2. Jedes Abonnement ist an seinen eigenen `DonchianChannels`-Indikator mit einer konfigurierbaren Lookback-Länge (Standard `26`) gebunden.
3. Sobald der Indikator gebildet ist, verfolgt die Strategie für jeden Zeitrahmen den vorherigen Kerzenschluss.
4. Bei jeder fertigen Kerze wird der aktuelle Schlusskurs mit den oberen und unteren Bändern von Donchian verglichen:
   - Wenn sich der Schlusskurs von unterhalb nach oberhalb des oberen Bandes bewegt, wird eine Meldung „Über dem Widerstand kreuzen“ protokolliert.
   - Wenn sich der Schlusskurs von oben nach unten über das untere Band bewegt, wird die Meldung „Unterschreitung der Unterstützung“ protokolliert.
5. Die Logik reproduziert das Benachrichtigungsverhalten des MQL4-Skripts, indem sie `LogInfo`-Einträge als Warnungen verwendet.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `LookbackLength` | Anzahl der Kerzen, die zur Berechnung von Donchian Unterstützung/Widerstand verwendet werden. | 26 |
| `Hour1CandleType` | Kerzentyp für das einstündige Abonnement. | `TimeFrame(1h)` |
| `Hour4CandleType` | Kerzentyp für das Vier-Stunden-Abonnement. | `TimeFrame(4h)` |

## Signale
- **H1-Ausbruch** – wird protokolliert, wenn der einstündige Schlusskurs der Kerze den Widerstand oder die Unterstützung überschreitet.
- **H4-Ausbruch** – wird protokolliert, wenn der 4-Stunden-Kerzenschluss den Widerstand oder die Unterstützung überschreitet.

## Notizen
- Die Strategie dient ausschließlich der Alarmierung; es führt keine Trades aus.
- Damit der Indikator Donchian ordnungsgemäß funktioniert, müssen beide Kerzenabonnements Hoch- und Tiefstdaten liefern.
- Passen Sie die Lookback-Länge oder Kerzentypen an, um sie an andere Handelssitzungen oder Instrumente anzupassen.
