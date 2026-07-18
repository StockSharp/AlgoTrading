# ZigZag EvgeTrofi Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die ZigZag EvgeTrofi Strategie portiert den klassischen MetaTrader Expert Advisor in die StockSharp High-Level-API. Sie beobachtet den jüngsten Swing, der durch einen ZigZag-ähnlichen Prozess erkannt wurde, und reagiert schnell, solange der Pivot noch frisch ist.

## Konzept

* Der ursprüngliche Advisor analysiert den ersten Nicht-Null-Punkt des ZigZag-Puffers und entscheidet, ob der letzte bestätigte Swing ein Hoch oder ein Tief war.
* Ein Swing-Hoch generiert standardmäßig einen Long-Einstieg. Durch Aktivierung von **SignalReverse** wird die Logik invertiert.
* Positionen werden nur eröffnet, solange der neue Pivot als frisch gilt. Der **Urgency**-Parameter begrenzt die Anzahl der Balken nach einem Pivot, wenn Trades initiiert werden können.
* Bestehende Positionen in der entgegengesetzten Richtung werden sofort vor der Platzierung neuer Orders geflacht. Die Strategie kann auf aufeinanderfolgenden Balken in dieselbe Richtung skalieren, während das Dringlichkeitsfenster offen ist.

Dieser Port behält das konträre Verhalten bei: Neue Hochs lösen Long-Trades aus, während frische Tiefs Shorts auslösen, was dem ursprünglichen Setup entspricht.

## Funktionsweise

1. Zwei rollende Indikatoren (`Highest` und `Lowest`) approximieren die MetaTrader ZigZag-Tiefenlogik.
2. Wann immer der Preis ein neues Extrem über/unter diesen Bändern druckt und die Bewegung **Deviation** (in Preisschritten) übersteigt, wird ein Pivot aufgezeichnet.
3. Der Algorithmus verfolgt, wie viele Balken seit dem Pivot vergangen sind. Sobald der Zähler **Urgency** überschreitet, läuft das Signal ab.
4. Auf jeder geschlossenen Kerze während des aktiven Fensters gibt die Strategie unter Verwendung von `VolumePerTrade` ein. Entgegengesetzte Exposure wird zuerst geschlossen, sodass Flip-Trades sauber vonstattengehen.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `Depth` | 17 | Fenster in Balken, um nach Swing-Hochs/-Tiefs zurückzuschauen. Entspricht dem ZigZag-Tiefen-Input. |
| `Deviation` | 7 | Mindest-Preisverschiebung in Punkten (multipliziert mit dem Symbol-Preisschritt), die zum Akzeptieren eines neuen Pivots erforderlich ist. |
| `Backstep` | 5 | Balken, die ablaufen müssen, bevor der Indikator zur entgegengesetzten Pivot-Richtung wechseln kann. |
| `Urgency` | 2 | Maximale Anzahl von Balken nach dem Pivot, wenn Einstiege erlaubt sind. |
| `SignalReverse` | `false` | Kehrt die Zuordnung von Hochs/Tiefs zu Long-/Short-Signalen um. |
| `CandleType` | 5-Minuten-Kerzen | Zeitrahmen für die Analyse. An den gewünschten Chart anpassen. |
| `VolumePerTrade` | 0.10 | Ordergröße bei jedem Einstieg. Entspricht dem ursprünglichen Lot-Input. |

## Handelshinweise

* Die Logik enthält **keine** Stops oder Ziele. Risikosteuerung muss bei Bedarf über Overlays oder Portfolio-Einstellungen hinzugefügt werden.
* Da das System auf jedem Balken innerhalb des Dringlichkeitsfensters zu einer Position hinzufügen kann, kann die Positionsgröße bei starken Trends schnell wachsen.
* Verwenden Sie höhere Tiefen bei volatilen Symbolen, um übermäßige Pivots zu vermeiden. Niedrigere Tiefen machen die Strategie reaktiver, aber rauschiger.
* Wenn **SignalReverse** true ist, wird das Verhalten zum Breakout-Folgen: Swing-Hochs lösen Shorts aus und Swing-Tiefs lösen Longs aus.

## Dateien

* `CS/ZigZagEvgeTrofiStrategy.cs` – C#-Implementierung der Strategie.
* Die Python-Version wird absichtlich nicht bereitgestellt.
