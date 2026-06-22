# TTM Trend Wiedereinstiegs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie recreiert die Logik des MetaTrader-Expertenberaters *Exp_ttm-trend_ReOpen*. Sie überträgt den TTM-Trend-Indikator in das StockSharp-Framework, verwendet Heikin-Ashi-Glättung zur Kerzenkolorierung und handelt, wenn die Farbe von bearisch zu bullisch oder umgekehrt wechselt. Jeder Farbwechsel repräsentiert einen Regimewechsel bei Volatilitätskompression/-expansion, weshalb der Bot sofort jede entgegengesetzte Exposition schließt und eine Position in der neuen Richtung öffnet.

## Indikatorlogik
Der ursprüngliche Indikator färbt jede Bar gemäß sowohl dem Heikin-Ashi-Körper als auch der klassischen OHLC-Kerze:

- **Hellgrün (4)** – Heikin-Ashi-Schluss über seiner Öffnung und die Standard-Kerze schließt höher als sie öffnet.
- **Türkis (3)** – Heikin-Ashi ist bullisch, aber die rohe Kerze schließt tiefer.
- **Tiefes Rosa (0)** – Heikin-Ashi ist bearisch und die rohe Kerze schließt tiefer.
- **Lila (1)** – Heikin-Ashi ist bearisch, während die rohe Kerze höher schließt.
- **Grau (2)** – Neutraler Fallback, wenn der Trend nicht bestimmt werden kann.

Um das MetaTrader-Buffer-Glätten nachzuahmen, hält der Indikator ein rollendes Fenster (`CompBars`) vorheriger Heikin-Ashi-Werte. Wenn der neueste Körper innerhalb des Hoch-/Tief-Envelops einer gespeicherten Kerze bleibt, wird die vorherige Farbe wiederverwendet. Dies verhindert Whipsaws bei kleinen Rücksetzern, genau wie die Quellimplementierung.

## Handelsregeln
1. Den vom `CandleType` konfigurierten Zeitrahmen abonnieren und nur abgeschlossene Kerzen auswerten (`SignalBar` wählt aus, wie viele geschlossene Bars vom letzten Historiepunkt zurückgeblickt wird).
2. Wenn eine **bullische Farbe** (Werte 1 oder 4) erscheint und das vorherige Signal nicht bullisch war:
   - Jeden Short schließen, wenn `EnableShortExits` aktiv ist.
   - Eine Long-Position öffnen (oder von Short zu Long drehen), wenn `EnableLongEntries` wahr ist.
3. Wenn eine **bearische Farbe** (Werte 0 oder 3) erscheint und das vorherige Signal nicht bearisch war:
   - Jeden Long schließen, wenn `EnableLongExits` aktiv ist.
   - Eine Short-Position öffnen (oder von Long zu Short drehen), wenn `EnableShortEntries` wahr ist.
4. Jede Seite kann zusätzliches Volumen pyramidisieren, wenn sich der Preis um mindestens `PriceStepPoints` (umgerechnet in Preis mit dem `PriceStep` des Instruments) zugunsten des Trades bewegt. Die kumulative Anzahl von Einstiegen pro Richtung ist durch `MaxPositions` begrenzt.

## Pyramidisierungsverhalten
- `PriceStepPoints` spiegelt den MetaTrader-"PriceStep"-Input wider: Sobald der unrealisierte Gewinn diese Distanz vom Durchschnittseinstiegspreis überschreitet, fügt der Bot das Basis-`Volume` erneut hinzu.
- `MaxPositions` begrenzt die Gesamtzahl gestapelter Einstiege, einschließlich des Ersthandels. Auf `1` setzen, um Wiedereinstiege vollständig zu deaktivieren.

## Risikomanagement
`StopLossPoints` und `TakeProfitPoints` werden in Instrumentenpunkten gemessen, genau wie im ursprünglichen EA. Sie werden in absolute Preisabstände über `Security.PriceStep` umgewandelt und über `StartProtection` angewendet. Einen Parameter auf null setzen, um den jeweiligen Schutz zu deaktivieren.

## Parameter
- `CandleType` – Zeitrahmen für die TTM-Trend-Berechnung (Standard: 4-Stunden-Kerzen).
- `CompBars` – Anzahl der historischen Heikin-Ashi-Kerzen für die Farbenglättung (Standard: 6).
- `SignalBar` – Anzahl der Bars zurück von der letzten abgeschlossenen Kerze zur Auswertung (Standard: 1 → letzter geschlossener Bar).
- `PriceStepPoints` – Minimale günstige Bewegung in Punkten vor dem Pyramidisieren (Standard: 300).
- `MaxPositions` – Maximale Anzahl kumulativer Einstiege pro Richtung (Standard: 10).
- `EnableLongEntries` / `EnableShortEntries` – Long/Short-Öffnungen bei Farbwechseln ein-/ausschalten.
- `EnableLongExits` / `EnableShortExits` – Erzwungene Exits beim Erscheinen der entgegengesetzten Farbe ein-/ausschalten.
- `StopLossPoints` – Schutz-Stop-Abstand in Punkten (Standard: 1000).
- `TakeProfitPoints` – Gewinnziel-Abstand in Punkten (Standard: 2000).

## Verwendungshinweise
- Die TTM-Trend-Farbenlogik ist empfindlich gegenüber dem gewählten Zeitrahmen; das ursprüngliche System verwendete den H4-Chart, aber jeder `CandleType` kann geliefert werden.
- Da der Indikator mit Heikin-Ashi-Körpern arbeitet, können plötzliche Gaps einen Farbwechsel nicht sofort auslösen – auf die nächste abgeschlossene Kerze zur Bestätigung warten.
- `PriceStepPoints` auf null setzen, wenn ein Einstiegssystem ohne Pyramidisierung gewünscht wird.
