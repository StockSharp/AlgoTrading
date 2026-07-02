# SilverTrend V3 JTPO-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
SilverTrend V3 ist eine Trendfolgestrategie, die aus der ursprünglichen MetaTrader 4-Implementierung übersetzt wurde. Es wertet den SilverTrend-Indikator zusammen mit dem statistischen Filter J_TPO aus, um neue Richtungsschwankungen zu identifizieren. Die Strategie handelt jeweils mit einem einzelnen Instrument und erzwingt eine Freitagabend-Flat-Regel, um zu vermeiden, dass das Risiko über das Wochenende gehalten wird.

## Handelslogik
1. **Indikatorverarbeitung**
   - Die Strategie behält einen rollierenden Puffer der letzten Kerzen bei und berechnet die SilverTrend-Richtung für jeden abgeschlossenen Balken neu.
   - SilverTrend verwendet ein 9-Balken-Fenster und einen Risikofaktor von 3, um die adaptiven Kanalgrenzen zu bestimmen. Wenn der Schlusskurs die Obergrenze überschreitet, wechselt das Signal zu bullisch; Das Unterschreiten der Untergrenze dreht das Signal in Richtung Abwärts.
   - Die J_TPO-Berechnung (Länge 14) misst die Schiefe der Preisverteilung. Nur positive J_TPO-Werte bestätigen Long-Einträge, während für Short-Werte negative Werte erforderlich sind.
2. **Eintrittsbedingungen**
   - Ein Long-Trade wird eröffnet, wenn das SilverTrend-Signal von bärisch zu bullisch wechselt und J_TPO über Null liegt.
   - Ein Short-Trade wird eröffnet, wenn das SilverTrend-Signal von bullisch zu bärisch wechselt und J_TPO unter Null liegt.
   - Neue Positionen werden freitags blockiert, sobald die Marktstunde den konfigurierten Cutoff überschreitet.
3. **Exit-Management**
   - Entgegengesetzte SilverTrend-Signale schließen offene Geschäfte sofort.
   - Optionale anfängliche Stop-Loss- und Take-Profit-Levels werden in festen Abständen (ausgedrückt in Punkten) platziert.
   - Ein optionaler Trailing Stop folgt dem Preis, sobald er den konfigurierten Gewinnpuffer überschreitet.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `Volume` | Bestellgröße in Losen. | `1` |
| `TrailingStopPoints` | Trailing-Stop-Distanz in Preispunkten. `0` deaktiviert das Trailing. | `0` |
| `TakeProfitPoints` | Nehmen Sie die Gewinndistanz in Preispunkten. `0` deaktiviert den Take-Profit. | `0` |
| `InitialStopPoints` | Anfängliche Stop-Loss-Distanz in Preispunkten. `0` deaktiviert den Schutzstopp. | `0` |
| `FridayCutoffHour` | Stunde (Börsenzeit), nach der neue Geschäfte am Freitag nicht mehr möglich sind. | `16` |
| `CandleType` | Kerzentyp oder Zeitrahmen, der für die Analyse verwendet wird. | `1h` Kerzen |

## Zusätzliche Hinweise
- Es ist immer nur eine Position offen, was dem Single-Trade-Verhalten des ursprünglichen Expertenberaters entspricht.
- Die Implementierung verwendet StockSharp auf hoher Ebene API, sodass die Strategie Kerzen abonniert und die Logik nur für fertige Balken ausführt.
- Trailing- und Fixstopps werden intern verwaltet und schließen die Position zum Marktpreis, sobald sie ausgelöst werden.
