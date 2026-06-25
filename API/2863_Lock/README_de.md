# Lock-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Lock-Strategie recreiert den klassischen "Lock"-Expertenberater aus MetaTrader: Sie unterhält immer ein abgesichertes Paar aus Long- und Short-Positionen und recycelt diese, bis eine Gewinn-Sicherungs-Bedingung erfüllt ist. Der Algorithmus ist für Instrumente mit kleinen Tick-Größen ausgelegt, bei denen ein fixer Pip-basierter Take-Profit angewendet werden kann.

## Handelsablauf

1. **Initiale Absicherung** – sobald Marktdaten verfügbar sind, öffnet die Strategie eine Long- und eine Short-Position mit gleichem Volumen. Wenn beide Aufträge ausgeführt werden, wird das Volumen für die nächste Absicherung mit dem Faktor `LotExponential` multipliziert.
2. **Take-Profit-Verwaltung** – jedes Bein speichert seinen Einstiegspreis. Wenn der Kerzenschluss um `TakeProfitPips` (in Instrument-Ticks umgerechnet) vom Einstieg abweicht, wird das Bein mit einem Marktauftrag geschlossen. Die entgegengesetzte Seite bleibt offen und bewahrt das absicherungsähnliche Verhalten der MQL-Version.
3. **Neu-Absicherung** – wenn die Gesamtzahl der aktiven Beine eins oder null ist, öffnet die Strategie sofort ein frisches Paar. Wenn keine offenen Beine vorhanden sind, wird das Basisvolumen auf `LotSize` zurückgesetzt, bevor das neue Paar erstellt wird.
4. **Volumenkontrolle** – die Hilfsmethode `AdjustVolume` erzwingt Exchange-Beschränkungen: Sie rundet Volumina auf den `VolumeStep` der Sicherheit, begrenzt sie durch `MinVolume` und `MaxVolume`, und bricht die Skalierung ab, wenn der angepasste Wert null wird.

## Gewinn-Sicherungs-Bedingung

Die ursprüngliche MQL-Logik überwacht Kontostand versus Eigenkapital: Wenn der Kontostand das Eigenkapital um `ExcessBalanceOverEquity` übersteigt und das Eigenkapital mindestens `MinProfit` über dem zuletzt gesperrten Kontostand liegt, wird jedes Bein geschlossen. Die C#-Implementierung spiegelt dieses Verhalten wider, indem das bei flacher Position beobachtete Eigenkapital verfolgt und als laufendes Guthaben behandelt wird. Sobald die Bedingung ausgelöst wird, werden alle Beine liquidiert und das Basisguthaben wird aktualisiert, bevor der Zyklus mit `LotSize` neu startet.

## Parameter

- `LotSize` – Basisvolumen für den ersten Absicherungszyklus (Standard: `0.1m`).
- `TakeProfitPips` – Pip-Abstand zum Schließen jedes Beins (Standard: `100`). Ein Wert von `0` deaktiviert den automatischen Ausstieg.
- `LotExponential` – Multiplikator, der nach dem erfolgreichen Öffnen beider Beine auf das aktuelle Volumen angewendet wird (Standard: `2m`).
- `ExcessBalanceOverEquity` – tolerierte Lücke zwischen Kontostand und Eigenkapital vor der Gewinnabsicherung (Standard: `3000m`).
- `MinProfit` – zusätzliches Eigenkapitalwachstum, das vor dem Schließen aller Beine erreicht werden muss (Standard: `500m`).
- `CandleType` – Zeitrahmen, der die Strategielogik antreibt (Standard: 1-Minuten-Zeitrahmen).

## Implementierungshinweise

- Die Pip-Größe wird aus `Security.PriceStep` und `Security.Decimals` neu berechnet, sodass sich die Strategie an 3/5-stellige FX-Symbole sowie Standard-Futures oder -Aktien anpasst.
- Aufträge werden mit Marktausführung platziert, was das Verhalten des MQL-Experten widerspiegelt, der Marktaufträge mit Broker-seitigen Take-Profits sendet.
- Die Strategie behält eine vollständige Historie der abgesicherten Beine, die mehrere gestapelte Positionen auf jeder Seite ermöglicht, genau wie das Quellskript es erlaubte.
