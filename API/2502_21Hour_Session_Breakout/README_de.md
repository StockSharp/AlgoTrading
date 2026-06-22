# 21-Stunden-Sitzungsausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert den MetaTrader "21hour" Expert Advisor innerhalb von StockSharp. Sie operiert während zwei konfigurierbarer Handelsfenster und verwendet ausstehende Stop-Orders, um Ausbrüche an der Ober- und Unterseite der Range zu erfassen. Am Ende jedes Fensters liquidiert die Strategie jede offene Exposition und entfernt die aktiven Orders, um sicherzustellen, dass jeder Handelstag sauber beginnt.

## Kernidee

- Die Handelsrichtung wird ausschließlich durch die Kursbewegung rund um die angegebenen Sitzungsstartzeiten bestimmt.
- Zu Beginn jeder Sitzung umklammert die Strategie den Markt mit einem Buy-Stop über dem aktuellen Ask und einem Sell-Stop unter dem aktuellen Bid.
- Wenn eine Stop-Order ausgeführt wird, wird die entgegengesetzte Seite sofort storniert und eine Take-Profit-Order mit festem Abstand platziert.
- Zur konfigurierten Sitzungsendzeit wird jede Position geschlossen und alle Orders werden storniert, auch wenn der Take-Profit noch nicht erreicht wurde.

## Datenfluss

- **Kerzen:** 1-Minuten-Kerzen (konfigurierbar) werden nur zur Bereitstellung von Zeitstempeln und zur Auslösung der stündlichen Terminprüfungen verwendet.
- **Orderbuch:** Level-1-Quotes liefern die aktuellen besten Bid/Ask-Werte, die die Aktivierungspreise der Stop-Orders definieren.

## Handelsregeln

### Einsstiegsplanung
- Zu `FirstSessionStartHour` (Standard 08:00 Serverzeit) und zu `SecondSessionStartHour` (Standard 22:00) führt die Strategie aus:
  - Platziert einen Buy-Stop bei `Ask + StepPoints * PriceStep`.
  - Platziert einen Sell-Stop bei `Bid - StepPoints * PriceStep`.
- Es ist nur eine Position erlaubt. Wenn beim Start der anderen Sitzung bereits eine Position offen ist, werden alle ausstehenden Einstiegsorders vor der Platzierung neuer entfernt.

### Order-Management
- Wenn eine der Stop-Orders ausgeführt wird, wird der entgegengesetzte Stop sofort storniert.
- Eine Take-Profit-Limit-Order wird bei `EntryPrice ± TakeProfitPoints * PriceStep` je nach Handelsrichtung registriert.
- Order-Größen sind durch den `Volume`-Parameter festgelegt (Standard 1 Lot).

### Ausstiegslogik
- Take-Profit-Orders schließen gewinnende Trades automatisch.
- Zu `FirstSessionStopHour` (Standard 21:00) und `SecondSessionStopHour` (Standard 23:00) schließt die Strategie alle offenen Positionen zum Marktpreis und storniert alle verbleibenden ausstehenden Orders.
- Wenn die Position manuell geschlossen wird, entfernt die Strategie auch die ausstehende Take-Profit-Order.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `Volume` | `1` | Order-Volumen für Stop-Einstiege und Take-Profit-Ausstiege. |
| `FirstSessionStartHour` | `8` | Stunde (0-23), wenn die erste Handelssitzung beginnt. |
| `FirstSessionStopHour` | `21` | Stunde, wenn die erste Sitzung endet und Positionen geschlossen werden. |
| `SecondSessionStartHour` | `22` | Stunde, wenn die Abendsitzung beginnt. Muss nach der ersten Sitzung liegen. |
| `SecondSessionStopHour` | `23` | Stunde, wenn die zweite Sitzung endet. Muss nach dem Stop der ersten Sitzung liegen. |
| `StepPoints` | `5` | Abstand vom besten Quote zur Stop-Order, gemessen in Preisschritten. |
| `TakeProfitPoints` | `40` | Abstand zwischen Einstiegspreis und Take-Profit-Limit, gemessen in Preisschritten. |
| `CandleType` | `1 Minute` | Kerzentyp für die Intraday-Terminprüfungen. |

Alle Parameter werden validiert, um überlappende Sitzungen oder unmögliche Stundenkombinationen zu vermeiden.

## Tags und Eigenschaften

- **Stil:** Sitzungsausbruch / zeitbasiertes Trendfolge.
- **Richtung:** Long und Short.
- **Zeitrahmen:** Intraday, zeitplangesteuert (1-Minuten-Kerzen nur für Timing).
- **Risikokontrollen:** Fester Take-Profit plus erzwungenes Flat am Sitzungsende (kein Stop-Loss).
- **Markttypen:** Für FX, Indizes oder jedes Instrument mit kontinuierlichen Handelszeiten und zuverlässigen Quotes konzipiert.
- **Komplexität:** Niedrig – keine Indikatoren, rein zeit- und preisbasiert.

## Implementierungshinweise

- Die Strategie erfordert einen gültigen `Security.PriceStep`; Orders werden übersprungen, wenn Preisschritt oder Quotes nicht verfügbar sind.
- Take-Profit-Volumina verwenden das ausgeführte Handelsvolumen, wenn verfügbar, mit Fallback auf die aktuelle Position oder das konfigurierte Volumen.
- Der Code enthält englische Inline-Kommentare zur Klarheit und spiegelt die ursprüngliche MQL-Logik wider, während StockSharp High-Level-APIs genutzt werden (`SubscribeCandles`, `SubscribeOrderBook`, Hilfsparameter und Order-Helpers).
