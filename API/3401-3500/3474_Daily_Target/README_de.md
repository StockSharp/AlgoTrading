# Tägliche Zielstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

`DailyTargetStrategy` repliziert den MetaTrader 4-Expertenberater „Daily Target“. Die Strategie handelt weiterhin mit offenen Positionen bis
Der kombinierte Gewinn und Verlust des aktuellen Kalendertages erreicht ein konfiguriertes Gewinnziel oder überschreitet eine maximale Verlustgrenze. Als
Sobald einer der Schwellenwerte erreicht wird, werden alle aktiven Aufträge storniert und die Position abgeflacht, sodass der Handel bis zum Erreichen des Schwellenwerts pausiert bleibt
Der nächste Tag beginnt.

## Handelslogik

1. **Inbetriebnahme**
   - Die Strategie ruft `ResetDailySnapshot` während `OnStarted` auf, um das aktuelle Datum und die realisierte PnL-Basislinie zu speichern.
   - `SubscribeLevel1()` liefert Gebots-/Briefaktualisierungen, die zur genauen Bewertung des schwankenden Gewinns erforderlich sind.
   - `SubscribeTrades()` erfasst den zuletzt ausgeführten Preis und bietet einen Ersatz, wenn Kurse fehlen.
   - Ein einminütiger `Timer`-Tick sorgt dafür, dass Datumsänderungen auch dann erkannt werden, wenn keine Marktdaten eintreffen.
2. **PnL-Auswertung**
   - `EvaluateDailyThresholds` berechnet den realisierten PnL neu (aktueller `PnL` minus der gespeicherten Basislinie) und fügt den gleitenden PnL hinzu
berechnet aus dem letzten Geld-/Briefkurs oder letzten Handelspreis.
   - Wenn der gesamte tägliche PnL das konfigurierte Ziel überschreitet oder unter die negative Verlustgrenze fällt, ruft die Strategie auf
`TriggerDailyStop`.
3. **Notausgang**
   - `TriggerDailyStop` schreibt einen informativen Protokolleintrag, storniert alle ausstehenden Aufträge und sendet den entsprechenden Marktauftrag an
Reduzieren Sie die verbleibende lange oder kurze Belichtung.
   - `_dailyStopTriggered` verhindert den erneuten Eintritt am selben Tag. Wenn sich das Kalenderdatum ändert, löscht `ResetDailySnapshot` dies
markiert und zeichnet eine neue PnL-Basislinie auf.

## Parameter

| Name | Standard | Beschreibung |
| --- | --- | --- |
| `DailyTarget` | `10` | Gewinnziel in Portfoliowährung. Der Handel wird für den Rest des Tages eingestellt, sobald der gesamte tägliche PnL diesen Wert erreicht oder überschreitet. |
| `DailyMaxLoss` | `0` | Maximal tolerierter Verlust in Portfoliowährung. Auf Null setzen, um den Verlustfilter zu deaktivieren. Der Handel wird für den Tag unterbrochen, sobald der gesamte tägliche PnL unter den negativen Schwellenwert fällt. |

## Notizen

- Die Strategie verwaltet nur das primäre `Security`, das der Strategieinstanz zugewiesen ist, und spiegelt das Einzelsymbolverhalten der wider
MQL Experte.
- Floating PnL verwendet den besten Geldkurs für Long-Positionen und den besten Briefkurs für Short-Positionen. Wenn kein Angebot verfügbar ist, der letzte Handel
Der Preis fungiert als Fallback, um ein Abwürgen der Bewertung zu vermeiden.
- Es wird kein Python-Port bereitgestellt. In diesem Paket ist nur die C#-High-Level-Implementierung enthalten.
