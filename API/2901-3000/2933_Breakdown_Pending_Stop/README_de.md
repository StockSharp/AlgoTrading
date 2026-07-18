# Ausbruch-Pending-Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie recreates den originalen MetaTrader "breakdown"-Expertenberater. Sie platziert Stop-Orders rund um den Bereich des Vortages und aktualisiert die Orders kontinuierlich jede Sitzung. Eine Trailing-Stop-Engine repliziert die gestufte Trailing-Logik des Quellskripts und hält die Stops eng, sobald eine Position in die profitable Richtung zu bewegen beginnt.

## Funktionsweise
- **Tägliche Vorbereitung** – Wenn eine Tageskerze schließt, speichert die Strategie das Hoch und das Tief. Zu Beginn der folgenden Sitzung werden übrig gebliebene Orders storniert und ein Buy Stop oberhalb des vorherigen Hochs und ein Sell Stop unterhalb des vorherigen Tiefs eingereicht. Der Parameter `Min Distance (ticks)` versetzt die Orders von den Rohlevels, um Rauschen zu vermeiden.
- **Order-Aktualisierung** – Wann immer ausstehende Orders ausgeführt werden oder ein neuer Tag beginnt, werden die verbleibenden Orders storniert und ein frisches Paar mit denselben Levels des Vortages eingereicht. Das Verhalten spiegelt den MQL-Experten wider, der kontinuierlich Stop-Einstiege auf beiden Marktseiten aufrechterhält.
- **Risikokontrollen** – Ausgeführte Positionen initialisieren Stop-Loss- und Take-Profit-Ziele basierend auf Tick-Distanzen. Eine gestufte Trailing-Regel hebt/senkt den Stop nur, nachdem der Preis mindestens `Trailing Stop (ticks) + Trailing Step (ticks)` vom Einstieg entfernt ist, genau wie die ursprüngliche Trailing-Stop-Implementierung.
- **Exits** – Positionen schließen sofort, wenn der Preis den aktiven Stop oder das Ziel berührt. Manuelles Trailing schließt Positionen zum Markt, wenn das Trailing-Level verletzt wird, was der MetaTrader-Logik entspricht, die Stops bei jedem Tick modifizierte.

## Parameter
| Parameter | Beschreibung |
|-----------|--------------|
| `Working Candles` | Zeitrahmen zur Überwachung der Preisbewegung und Verwaltung von Stops (Standard: 15-Minuten-Kerzen). |
| `Stop Loss (ticks)` | Initialer schützender Stop-Abstand, konvertiert in absoluten Preis unter Verwendung der Instrument-Tick-Größe. Auf null setzen zum Deaktivieren. |
| `Take Profit (ticks)` | Initialer Take-Profit-Abstand. Auf null setzen zum Deaktivieren. |
| `Trailing Stop (ticks)` | Kern-Trailing-Stop-Distanz. Auf null setzen, um Trailing zu deaktivieren. |
| `Trailing Step (ticks)` | Zusätzlicher Gewinn erforderlich, bevor der Trailing-Stop bewegt wird. |
| `Min Distance (ticks)` | Versatz, der zum Hoch/Tief des Vortages beim Platzieren ausstehender Orders hinzugefügt wird. |
| `Order Volume` | Menge, die mit beiden Stop-Orders gesendet wird. |

## Nutzungshinweise
- Die Strategie auf Instrumenten konfigurieren, die Tageskerzen veröffentlichen, damit der vorherige Sitzungsbereich ermittelt werden kann.
- Die Logik geht von einer konstanten Tick-Größe aus. Für Instrumente mit variablen Tick-Inkrementen die Standardwerte entsprechend anpassen.
- Die Strategie implementiert keine prozentuale Dimensionierung aus dem ursprünglichen MQL-Skript; das Volumen wird explizit durch den Parameter `Order Volume` definiert.
- Für diese Strategie ist noch keine Python-Version verfügbar.
