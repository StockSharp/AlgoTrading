# AeroSpine-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die AeroSpine-Strategie ist eine Konvertierung des MetaTrader-Experten **AEROSPINE.mq4**. Sie handelt ein einzelnes Symbol und versucht, Ausbrüche vom täglichen Eröffnungspreis zu erfassen. Der ursprüngliche Roboter war für Tagescharts konzipiert, während er Ticks überwachte; der Port behält die Idee des täglichen Eröffnungsausbruchs bei, stützt sich jedoch auf fertige Kerzen von StockSharp.

## Handelslogik
- Zu Beginn jedes Handelstages speichert die Strategie den täglichen Eröffnungspreis aus der ersten Kerze des Tages.
- Neue Positionen werden nur nach der konfigurierten Einstiegsstunde bewertet. Fertige Kerzen müssen einen minimalen Volumenfilter erfüllen und der aktuelle Spread muss unter dem konfigurierten Limit liegen.
- Wenn keine Position offen ist und kein Recovery-Trade ausstehend ist:
  - Ein **Long**-Trade wird eröffnet, sobald das Kerzenhoch die tägliche Eröffnung um `EntryOffsetPips` übersteigt.
  - Ein **Short**-Trade wird eröffnet, sobald das Kerzentief die tägliche Eröffnung um `EntryOffsetPips` unterschreitet.
- Nach einem Verlusttrade bereitet die Strategie einen Recovery-Einstieg in die entgegengesetzte Richtung vor. Recovery-Trades verwenden `RecoveryOffsetPips` und erhöhen das Volumen durch Addition des Basisvolumens zur Größe des Verlusttrades, was das Martingale-ähnliche Sizing des MQL-Experten repliziert.
- Offene Positionen werden mit drei Mechanismen verwaltet:
  - Ein fester Take-Profit bei `TakeProfitPips` vom Einstiegspreis.
  - Ein optionaler Break-Even-Auslöser, der den Trade schließt, wenn der Preis zur Break-Even-Distanz zurückkehrt, nachdem er sich zugunsten der Position bewegt hat.
  - Ein Schutzausstieg, wenn der Preis zur täglichen Eröffnung zurückkehrt und diese um `ExitOffsetPips` gegen die Position überschreitet.

## Parameter
| Name | Beschreibung |
| ---- | ----------- |
| **Candle Type** | Zeitrahmen der Arbeitskerzen für die Signalbewertung. |
| **Volume** | Basis-Auftragsgröße für Ersteinstiege und zum Aufbau des Recovery-Volumens. |
| **Entry Hour** | Mindeststunde (Börsenzeit), ab der neue Einstiege möglich sind. |
| **Entry Offset** | Abstand in Pips von der täglichen Eröffnung, der überschritten werden muss, um den ersten Trade des Tages zu eröffnen. |
| **Exit Offset** | Abstand in Pips jenseits der täglichen Eröffnung zum Schließen von Positionen, die zur Eröffnung zurückkehren. |
| **Recovery Offset** | Abstand in Pips von der täglichen Eröffnung für einen Recovery-Trade nach einem Verlust. |
| **Take Profit** | Fester Take-Profit-Abstand in Pips vom Einstiegspreis. |
| **Break Even** | Abstand in Pips zum Aktivieren des Break-Even-Ausstiegs. |
| **Use Break Even** | Aktiviert oder deaktiviert den Break-Even-Verwaltungsblock. |
| **Volume Filter** | Minimales Kerzenvolumen für neue Einstiege, entsprechend der originalen `Volume[0] > 10000`-Prüfung. |
| **Max Spread** | Lehnt neue Einstiege ab, wenn der aktuelle Spread breiter als der erlaubte Wert ist (aus Pips umgerechnet). |
| **Enable Recovery** | Aktiviert die entgegengesetzte Recovery-Logik nach einem Verlusttrade. |

## Hinweise zur Konvertierung
- Das originale EA platzierte Aufträge direkt bei Ticks während es einen Tageschart erzwang. Der Port emuliert dies mit Intraday-Kerzen: Die tägliche Eröffnung wird bei der ersten Kerze jedes Tages aktualisiert und die Ausbruchsprüfungen verwenden Kerzenhochs/-tiefs.
- Alle MetaTrader-Schnittstellenelemente (Labels, Kapitalberechnungen über mehrere Symbole usw.) wurden entfernt. Nur die relevante Handelslogik für das aktuelle Symbol wurde beibehalten.
- Break-Even und Stop-Modifikationen (`OrderModify`) werden über explizite `ClosePosition()`-Aufrufe simuliert, wenn die berechneten Schwellenwerte berührt werden.
- Spread- und Volumenfilter entsprechen direkt den originalen `MODE_SPREAD`- und `Volume[0]`-Prüfungen.
