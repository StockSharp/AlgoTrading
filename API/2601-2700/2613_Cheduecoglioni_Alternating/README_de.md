# Cheduecoglioni Wechselnde Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist ein StockSharp-Port des MQL5 Expert Advisors "cheduecoglioni". Sie hält den Trader immer im Markt, indem sie zwischen Short- und Long-Positionen wechselt. Jeder Einstieg wird mit festen Take-Profit- und Stop-Loss-Niveaus geschützt, die in Pips definiert und gemäß der Instrumentenpräzision in Preisabstände umgerechnet werden.

## Handelsregeln
- Die Strategie hört auf die konfigurierte Kerzenserie (standardmäßig 1 Minute) und reagiert nur, wenn eine Kerze vollständig geschlossen ist. Dieses Ereignis ersetzt die tick-basierte Schleife des ursprünglichen Expert Advisors.
- Wenn keine offene Position vorhanden ist und keine Marktorder auf Ausführung wartet, sendet die Strategie eine Marktorder in der im `_nextSide`-Zustand gespeicherten Richtung. Der allererste Trade nach dem Start ist ein Verkauf, entsprechend der MQL5-Implementierung.
- Sobald eine Position aktiv wird, wartet der Algorithmus darauf, dass sie durch die Schutzorders oder manuelle Intervention geschlossen wird. Sobald die Position auf null zurückkehrt, wechselt die nächste Richtung, sodass der folgende Trade in der entgegengesetzten Richtung erfolgt.
- Stop-Loss- und Take-Profit-Abstände werden automatisch von `StartProtection` angewendet, sodass jeder Trade die konfigurierten Risiko-Rendite-Abstände trägt.

## Parameter
- `Trade Volume` – Volumen für jeden Markteinstieg. Dies spiegelt den `InpLots`-Input wider.
- `Take Profit (pips)` – Abstand in Pips für die Take-Profit-Order. Die Strategie konvertiert ihn in einen absoluten Preisabstand unter Verwendung der erkannten Pip-Größe.
- `Stop Loss (pips)` – Abstand in Pips für den Schutz-Stop-Loss, mit derselben Pip-Größen-Logik umgerechnet.
- `Candle Type` – Zeitrahmen der Kerzen, die den Entscheidungszyklus antreiben. Jeder unterstützte `DataType` kann angegeben werden.

## Implementierungsdetails
- Die Pip-Größe wird aus `Security.PriceStep` abgeleitet. Für 3- oder 5-stellige FX-Symbole wird der Wert mit 10 multipliziert, um vom Bruchteil-Pip zum Standard-Pip zu wechseln, was die MQL-Anpassung repliziert.
- Ein Wartezeichen verhindert doppelte Marktorders, während eine vorherige Order auf Ausführung wartet. Wenn der Broker die Order ablehnt, löscht `OnOrderFailed` das Zeichen, sodass die nächste Kerze einen neuen Versuch starten kann.
- `OnPositionChanged` verfolgt die Seite der aktiven Position und schaltet `_nextSide` nach jedem Flachzustand um. Dies spiegelt die MQL-Logik wider, die die entgegengesetzte Seite nach jedem Ausstieg öffnete.
- Schutzorders werden von `StartProtection` mit Marktausstiegen verwaltet, was der sofortigen Stop-Loss- und Take-Profit-Zuweisung entspricht, die der Expert Advisor bei der Orderplatzierung vornahm.

## Hinweise
- Die Python-Version wurde absichtlich noch nicht erstellt.
- Die Strategie ändert keine Unit-Tests.
