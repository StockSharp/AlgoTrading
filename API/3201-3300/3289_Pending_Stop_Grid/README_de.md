# Pending-Stop-Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Pending-Stop-Grid-Strategie** ist eine direkte Umwandlung des MetaTrader-4 Expert Advisors `new.mq4`. Die Strategie hält zwei symmetrische Leitern aus Pending Orders:

- Eine Sequenz von Buy-Stop-Orders oberhalb des aktuellen Ask-Preises.
- Eine Sequenz von Sell-Stop-Orders unterhalb des aktuellen Bid-Preises.

Jede zusätzliche Stufe erhöht sowohl die Orderdistanz als auch das gehandelte Volumen proportional zu ihrer Position innerhalb der Leiter. Stop-Loss- und Take-Profit-Ziele werden jeder Order einzeln zugewiesen.

## Handelslogik
1. Die Strategie abonniert Level-1-Daten und verfolgt kontinuierlich die neuesten besten Bid- und Ask-Preise.
2. Sobald Marktdaten und Handelsberechtigungen verfügbar sind, berechnet sie die Pip-Größe anhand des Wertpapier-Preisschritts (wobei fünf- und dreistellige Symbole automatisch auf Standard-Pip-Werte normalisiert werden).
3. Vor dem Platzieren von Orders validiert die Strategie, dass das konfigurierte Basisvolumen die Mindest- und Höchstvolumenbeschränkungen des Instruments einhält.
4. Für jeden Index `i` von 1 bis `NumberOfTrades`:
   - Das Ordervolumen wird als `BaseVolume * i` berechnet und auf den nächsten erlaubten Schritt gerundet.
   - Ein Buy Stop wird bei `Ask + DistancePips * i * pipSize` mit optionalen Stop-Loss- und Take-Profit-Offsets platziert.
   - Ein Sell Stop wird bei `Bid - DistancePips * i * pipSize` mit gespiegelten Stop-Loss- und Take-Profit-Offsets platziert.
5. Wenn eine Order ausgeführt, storniert oder abgelehnt wird, wird der entsprechende Platz in der Leiter geleert und sofort mit einer neuen Pending Order aufgefüllt, sobald Marktdaten dies erlauben.
6. Das eingebaute `StartProtection()` wird beim Start aufgerufen, um die Plattform-Risikosteuerungen zu aktivieren.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `BaseVolume` | Volumen der ersten Pending Order. Jede nachfolgende Order multipliziert diese Basis mit ihrem Index. | `0.1` |
| `NumberOfTrades` | Anzahl der gleichzeitig gehaltenen Buy-Stop- und Sell-Stop-Orders. | `10` |
| `DistancePips` | Distanz (in Pips) zwischen Marktpreis und jeder Pending-Order-Stufe. | `10` |
| `StopLossPips` | Stop-Loss-Distanz, die jeder Order zugewiesen wird. Auf null setzen, um Stop-Loss-Platzierung zu deaktivieren. | `10` |
| `TakeProfitPips` | Take-Profit-Distanz, die jeder Order zugewiesen wird. Auf null setzen, um Take-Profit-Platzierung zu deaktivieren. | `10` |

Alle Parameter werden als optimierbare Strategieparameter bereitgestellt und validiert, um negative oder Nullwerte zu vermeiden (wo zutreffend).

## Zusätzliche Hinweise
- Volumina werden auf den nächsten zulässigen Schritt gerundet und innerhalb der börsenseitig definierten Mindest- und Höchstgrenzen begrenzt.
- Preise werden mit `Security.ShrinkPrice` normalisiert, um die Tickgröße des Instruments zu respektieren.
- Die Strategie hält keinen historischen Zustand: Sie baut die gesamte Leiter neu auf, wenn das Wertpapier zurückgesetzt wird oder sich Handelsberechtigungen ändern.
- Die Logik vermeidet manuelle Indikatorbuffer zugunsten der High-Level-API-Bindings von StockSharp und folgt damit den projektweiten Konvertierungsrichtlinien.
