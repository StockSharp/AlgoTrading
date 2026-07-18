# Exp Rj SlidingRangeRj Digit System Tm Plus Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Diese StockSharp-Strategie ist ein Port des MetaTrader-Expertenberaters `Exp_Rj_SlidingRangeRj_Digit_System_Tm_Plus`. Sie recreiert die ursprüngliche Handelslogik basierend auf dem benutzerdefinierten Kanalindikator **Rj_SlidingRangeRj_Digit** und bewahrt die konfigurierbaren Trade-Management-Optionen. Die Strategie überwacht abgeschlossene Kerzen auf einem konfigurierbaren Zeitrahmen, erkennt Ausbrüche jenseits des Kanals und reagiert auf diese Ereignisse mit verzögerten Einstiegen, optionalen zeitgesteuerten Ausstiegen und preisbasiertem Stop/Ziel-Management.

## Indikatorlogik

Der Rj_SlidingRangeRj_Digit-Indikator baut einen adaptiven Preiskanal durch einen mehrstufigen Mittelungsprozess auf:

1. Für das obere Band wird das Highest-High innerhalb von `UpCalcPeriodRange` Balken für jedes der letzten `UpCalcPeriodRange` gleitenden Fenster berechnet, verschoben um `UpCalcPeriodShift` Balken. Der Durchschnitt dieser Maxima wird auf die durch `UpDigit` angegebene Präzision gerundet.
2. Das untere Band wiederholt die gleiche Logik für Tiefs mit `DnCalcPeriodRange`, `DnCalcPeriodShift` und `DnDigit`.
3. Eine Kerze wird als Ausbruch markiert, wenn ihr Schlusskurs über dem oberen Band (Farben `2` / `3`) oder unter dem unteren Band (Farben `0` / `1`) liegt. Kerzen innerhalb des Kanals erzeugen eine neutrale Farbe (`4`).

Die Strategie streamt abgeschlossene Kerzen, baut die Bänder bei jedem Update neu auf und speichert die jüngsten Farbcodes, um das `CopyBuffer`/`SignalBar`-Verhalten der MQL-Implementierung nachzuahmen.

## Handelsregeln

* **Eintrittsverzögerung:** Signale werden auf dem durch `SignalBar` definierten Balken ausgewertet (Standard: ein Balken zurück). Die Strategie wartet, bis eine Ausbruchsfarbe erscheint und der vorherige Balken nicht dieselbe Ausbruchsfarbe hatte. Dies reproduziert die ursprüngliche Ein-Balken-Verzögerung vor der Trade-Ausführung.
* **Long-Einstiege:** Aktiviert durch `EnableBuyEntries`. Ein bullischer Ausbruch (`Farbe 2` oder `3`) löst einen Marktkauf aus, wenn keine Long-Position offen ist (Short-Exposure wird automatisch verrechnet).
* **Short-Einstiege:** Aktiviert durch `EnableSellEntries`. Ein bärischer Ausbruch (`Farbe 0` oder `1`) löst einen Marktverkauf aus, wenn keine Short-Position offen ist.
* **Ausstiegssignale:**
  * Longs schließen bei bärischen Ausbruchsfarben, wenn `EnableBuyExits` true ist.
  * Shorts schließen bei bullischen Ausbruchsfarben, wenn `EnableSellExits` true ist.
  * Optionaler zeitbasierter Ausstieg (`UseTimeExit`) schließt jede offene Position, sobald sie länger als `ExitMinutes` gehalten wurde.
  * Optionale Stop-Loss- und Take-Profit-Level in Punkten (`StopLossPoints`, `TakeProfitPoints`) werden in Preisoffsets unter Verwendung des Instruments `PriceStep` umgerechnet.

Alle Aktionen verwenden `BuyMarket` / `SellMarket`, sodass die Strategie Positionen bei Bedarf automatisch umkehrt.

## Parameter

| Parameter | Beschreibung | Standardwert |
|-----------|--------------|--------------|
| `CandleType` | Kerzentyp (Zeitrahmen) für die Signalerkennung. | 8-Stunden-Kerzen |
| `EnableBuyEntries` / `EnableSellEntries` | Long/Short-Ausbruchseinstiege erlauben. | `true` |
| `EnableBuyExits` / `EnableSellExits` | Indikatorbasierte Ausstiege für Longs/Shorts erlauben. | `true` |
| `UseTimeExit` | Trades nach einer festen Haltezeit schließen. | `true` |
| `ExitMinutes` | Haltezeitlimit in Minuten. | `1920` |
| `UpCalcPeriodRange`, `UpCalcPeriodShift`, `UpDigit` | Parameter des oberen Kanalbands. | `5`, `0`, `2` |
| `DnCalcPeriodRange`, `DnCalcPeriodShift`, `DnDigit` | Parameter des unteren Kanalbands. | `5`, `0`, `2` |
| `SignalBar` | Balkenversatz für die Auswertung von Ausbruchssignalen. | `1` |
| `StopLossPoints`, `TakeProfitPoints` | Stop-Loss / Take-Profit in Preispunkten (konvertiert mit `PriceStep`). | `1000`, `2000` |

Setzen Sie die `Volume`-Eigenschaft der Strategie, um die Positionsgröße zu steuern. Die Stop-Loss- und Take-Profit-Parameter sind optional; setzen Sie sie auf `0`, um einen der Schutzniveaus zu deaktivieren.

## Hinweise

* Die Strategie erwartet ausreichend Geschichte, um den gleitenden Kanal zu bilden (ungefähr `max(shift + 2 × range)` Kerzen). Sie verwaltet automatisch die internen Buffer und ignoriert Signale, bis genügend Daten verfügbar sind.
* Preisrundung wird mit Dezimalstellen durchgeführt, was das MQL-Indikator-Rundungsverhalten widerspiegelt.
* Die Python-Implementierung ist absichtlich ausgelassen gemäß den Projektanweisungen; nur die C#-Version wird bereitgestellt.
