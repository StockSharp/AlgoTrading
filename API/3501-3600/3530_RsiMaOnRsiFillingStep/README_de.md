# RSI MA auf RSI Füllschrittstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Der **RSI MA für die RSI Filling Step Strategy** ist ein StockSharp-Port des MetaTrader-Expertenberaters `RSI_MAonRSI_Filling Step EA.mq5`. Das ursprüngliche System misst den Impuls mit einem Relative Strength Index (RSI) und glättet diesen Oszillator mit einem gleitenden Durchschnitt. Trades werden eingeleitet, wenn RSI seinen gleitenden Durchschnitt kreuzt, während beide Werte auf der gleichen Seite der mittleren 50-Marke bleiben. Bei der Konvertierung bleiben die konfigurierbaren Handelsrichtungsfilter, der optionale Sitzungstimer und die Möglichkeit, die Signale umzukehren, erhalten, während die High-Level-Indikatorbindungen von StockSharp genutzt werden.

## Handelslogik
1. Abonnieren Sie die ausgewählte Kerzenserie und berechnen Sie zwei Indikatoren für jeden fertigen Balken: `RelativeStrengthIndex` mit der Länge `RsiPeriod` und `MovingAverage` (`MaType`, `MaPeriod`), angewendet auf den Stream RSI.
2. Warten Sie, bis die Kerze vollständig ist, bevor Sie handeln. Dabei wird die „neue Balken“-Absicherung von EA nachgebildet, sodass jeder Balken höchstens eine Handelsentscheidung hervorruft.
3. Ein **bullisches** Setup tritt auf, wenn der vorherige RSI-Wert unter seinem gleitenden Durchschnitt lag und der letzte Wert über dem Durchschnitt schließt, während beide Messwerte unter `MiddleLevel` bleiben (Standard 50). Ein **bärisches** Setup ist der gespiegelte Fall oberhalb des mittleren Niveaus.
4. Wenn `ReverseSignals` aktiviert ist, generiert die bullische Bedingung einen Short-Trade und die bärische Bedingung einen Long-Trade, was die umgekehrte Flagge von EA nachahmt.
5. Der Parameter `Mode` beschränkt den Handel auf Long-Only, Short-Only oder beide Richtungen. Zusätzliche Schutzvorrichtungen schließen optional den gegenüberliegenden Zugang und blockieren neue Einträge, wenn eine Position bereits offen ist.
6. Ein tägliches Zeitfenster, das mit der MetaTrader-Implementierung identisch ist, kann Signale außerhalb des konfigurierten Intervalls `SessionStart` → `SessionEnd` deaktivieren, einschließlich Sitzungen, die über Mitternacht hinausgehen.

## Parameter
- **CandleType** – von der Strategie verarbeitete Datenreihe. Der Standardwert sind einstündige Zeitrahmenkerzen.
- **RsiPeriod** – RSI Durchschnittslänge (Standard 14).
- **MaPeriod** – Länge des gleitenden Durchschnitts, der auf RSI angewendet wird (Standard 21).
- **MaType** – gleitender Durchschnittstyp, der für die RSI-Glättung verwendet wird (Standard `Simple`).
- **MiddleLevel** – zentrale RSI-Ebene, die von beiden Indikatoren zur Validierung von Trades verwendet wird (Standard 50).
- **ReverseSignals** – kehrt die Interpretation der bullischen/bärischen Kreuzung um (Standard `false`).
- **Modus** – Handelsrichtungsfilter (`BuyOnly`, `SellOnly`, `Both`).
- **CloseOppositePositions** – ob die Gegenposition abgeflacht werden soll, bevor ein neuer Trade eingegeben wird (Standard `false`).
- **OnlyOnePosition** – verhindert neue Aufträge, während eine Position bereits offen ist (Standard `false`).
- **UseTimeWindow** – aktiviert den täglichen Handelssitzungsfilter (Standard `false`).
- **SessionStart / SessionEnd** – Start- und Endzeiten der zulässigen Handelssitzung. Funktioniert bei Sitzungen über Nacht, wenn der Abschluss nach Mitternacht erfolgt.

## Implementierungshinweise
- Indikatorwerte werden über `Bind` bereitgestellt, wodurch die Notwendigkeit einer manuellen Pufferverwaltung entfällt, die beim ursprünglichen EA mit `CopyBuffer` erforderlich war.
- Vorherige RSI- und gleitende Durchschnittswerte werden zwischengespeichert, um das `RSI[m_bar_current+1]`-Zugriffsmuster von MQL widerzuspiegeln. Das Feld `_lastSignalBarTime` garantiert nur einen Trade pro Balken, genau wie die Zeitstempel `m_last_deal_buy_in` / `m_last_deal_sell_in` von EA.
- Das Handelsmanagement verwendet `BuyMarket()` und `SellMarket()`, um die unmittelbare Marktausführung von EA widerzuspiegeln. Das optionale Schließen des Gegenrisikos wird mit `ClosePosition()` abgewickelt, bevor die neue Bestellung aufgegeben wird.
- Der Zeitfilter repliziert die `TimeControlHourMinute`-Funktion von EA, einschließlich der Nachtfensterlogik, bei der die Startzeit größer als die Endzeit ist.
- Charting-Helfer zeichnen Preiskerzen mit Handelsmarkierungen und einem speziellen RSI-Panel, damit die Crossovers während Backtests visuell überprüft werden können.

## Unterschiede zum Expert Advisor
- Money-Management-Optionen (`ENUM_LOT_OR_RISK`, Trailing Stops, Freeze-Level-Checks) werden nicht reproduziert. StockSharp-Benutzer können ihre eigene Schutzlogik oder Risikomodule anhängen.
- Handelsbestätigungen, magische Zahlenprüfungen und manuelle Auftragswarteschlangen von EA sind nicht erforderlich, da StockSharp Auftragslebenszyklen anders verwaltet. Die Strategie geht von der sofortigen Verfügbarkeit von Marktaufträgen aus.
- Stop-Loss- und Take-Profit-Orders werden nicht automatisch angehängt. Verwenden Sie `StartProtection` oder externe Module, wenn dieses Verhalten erforderlich ist.

## Nutzungstipps
1. Halten Sie `MiddleLevel` nahe bei 50, um dem ursprünglichen Mittelwertumkehrverhalten treu zu bleiben. Eine Abweichung von diesem Wert treibt das System in Richtung Breakout-Handel.
2. Aktivieren Sie `OnlyOnePosition`, wenn Sie strikte Übergänge von flach zu Position bevorzugen. Deaktivieren Sie es, um Pyramiding mit benutzerdefinierter Volume-Logik zu ermöglichen.
3. Kombinieren Sie den Zeitfilter mit den Börsenhandelszeiten, wenn Sie auf Futures oder Aktien setzen, um nächtlichen Lärm zu vermeiden.
4. Optimieren Sie `MaPeriod`, `RsiPeriod` und `MiddleLevel` gemeinsam, wenn Sie die Strategie an neue Instrumente anpassen.

Mit diesen Notizen können Sie die RSI MA on RSI Filling Step-Strategie sicher in der StockSharp-Umgebung ausführen, anpassen und erweitern.
