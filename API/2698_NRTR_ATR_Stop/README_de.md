# NRTR ATR Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die **NRTR ATR Stop-Strategie** ist eine direkte Konvertierung des MetaTrader Expert Advisors `Exp_NRTR_ATR_STOP_Tm`. Das System kombiniert einen Non-Repainting Trend Reversal (NRTR) Stop mit einem Average True Range (ATR) Filter, um den dominanten Trend zu bestimmen und Schutzlevels nachzuziehen. Handelsentscheidungen werden beim Schließen des gewählten Zeitrahmens generiert und können um eine konfigurierbare Anzahl vollständig geformter Balken verzögert werden, um die ursprüngliche Signalverschiebung nachzuahmen.

Die Strategie ist auf der High-Level-API von StockSharp implementiert. Alle Handelslogik wird durch Kerzensubskriptionen, Indikatorbindungen und verwaltete Auftragshelfer gesteuert und gewährleistet Kompatibilität mit den Produkten Designer, Shell, Runner und API.

## Handelslogik

1. **Indikatorberechnung**
   - ATR wird im gewählten Zeitrahmen mit der angegebenen Periode berechnet.
   - Der ATR-Wert wird mit einem Koeffizienten multipliziert, um die oberen und unteren NRTR-Levels zu erstellen.
   - Die Trendrichtung ändert sich, wenn die vorherige Kerze das gegenüberliegende NRTR-Level durchbricht; diese Ereignisse erzeugen auch Pfeilsignale, die Einstiege auslösen können.
2. **Signalverzögerung**
   - Der Parameter `SignalBarDelay` reproduziert den `SignalBar`-Eingang von MetaTrader. Er verzögert die Ausführung um die gewählte Anzahl abgeschlossener Kerzen, sodass die Strategie historische Signale genau wie der Quell-Experte auswerten kann.
3. **Einstiege**
   - Eine **Long**-Position wird eröffnet, wenn eine bullische NRTR-Umkehr auftritt und Long-Einstiege aktiviert sind.
   - Eine **Short**-Position wird eröffnet, wenn eine bärische NRTR-Umkehr auftritt und Short-Einstiege aktiviert sind.
4. **Ausstiege**
   - Richtungsumkehrungen schließen jede entgegengesetzte Position, wenn das Schließen für diese Seite erlaubt ist.
   - Ein optionaler Sitzungsfilter kann alle Positionen außerhalb des erlaubten Handelsfensters zwangsweise schließen.
   - Zusätzliches Risikomanagement wird durch Stop-Loss- und Take-Profit-Abstände in Preisschritten gehandhabt. Das NRTR-Level zieht auch eine aktive Position nach, indem es den Schutz-Stop in Trendrichtung verschärft.

## Risikomanagement

- **Volumen**: Trades werden mit dem konfigurierbaren Parameter `OrderVolume` eröffnet. Das Volumen kann genau wie in der MetaTrader-Version optimiert werden.
- **Stop-Loss / Take-Profit**: Abstände werden in Vielfachen des Wertpapierpreisschritts angegeben und entsprechen den ursprünglichen punktbasierten Einstellungen. Wenn sowohl ein manueller Stop als auch ein NRTR-Level verfügbar sind, wird der Schutzpreis konservativ (am nächsten zum Markt) gewählt, um eine Ausweitung des Risikos zu vermeiden.
- **Sitzungssteuerung**: Wenn `UseTradingWindow` aktiviert ist, eröffnet die Strategie nur Positionen innerhalb des definierten Intervalls `[StartHour:StartMinute, EndHour:EndMinute]` und schließt jede offene Position, sobald der Markt dieses Fenster verlässt.

## Parameter

| Name | Standard | Beschreibung |
| --- | --- | --- |
| `OrderVolume` | 1 | Volumen beim Senden von Marktaufträgen. |
| `StopLossPoints` | 1000 | Stop-Abstand in Preisschritten. Auf `0` setzen zum Deaktivieren. |
| `TakeProfitPoints` | 2000 | Take-Profit-Abstand in Preisschritten. Auf `0` setzen zum Deaktivieren. |
| `BuyPosOpen` / `SellPosOpen` | `true` | Eröffnung von Long- oder Short-Positionen bei NRTR-Umkehrungen erlauben. |
| `BuyPosClose` / `SellPosClose` | `true` | Schließen von Long- oder Short-Positionen bei entgegengesetztem Signal erlauben. |
| `UseTradingWindow` | `true` | Zeitfilter aktivieren, der den ursprünglichen Expert Advisor imitiert. |
| `StartHour` / `StartMinute` | 0 / 0 | Beginn der erlaubten Handelssitzung. |
| `EndHour` / `EndMinute` | 23 / 59 | Ende der erlaubten Handelssitzung. Unterstützt Übernacht-Bereiche. |
| `CandleType` | 1-Stunden-Zeitrahmen | Kerzentyp für ATR- und NRTR-Berechnungen. |
| `AtrPeriod` | 20 | Anzahl der Balken zur ATR-Berechnung. |
| `AtrMultiplier` | 2 | Koeffizient, der beim Erstellen von NRTR-Levels auf den ATR angewendet wird. |
| `SignalBarDelay` | 1 | Anzahl abgeschlossener Balken zur Verzögerung der Signalausführung. |

## Hinweise

- Die Strategie verwendet ausschließlich kerzenbasierte Verarbeitung; eine Tick-für-Tick-Replikation des ursprünglichen EAs wird absichtlich vermieden, um mit der High-Level-StockSharp-Architektur konsistent zu bleiben.
- Kommentare im Code sind gemäß den Projektrichtlinien auf Englisch verfasst.
- Eine Python-Version wird entsprechend der Benutzeranforderung absichtlich weggelassen.
