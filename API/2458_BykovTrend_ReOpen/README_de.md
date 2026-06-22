# BykovTrend ReOpen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die BykovTrend ReOpen-Strategie verwendet die BykovTrend-Logik basierend auf den Indikatoren Williams %R und Average True Range. Ein Kaufsignal tritt auf, wenn der Trend bullisch wird, und ein Verkaufssignal, wenn er bärisch wird. Nach dem Einstieg in eine Position kann die Strategie bei jedem vordefinierten Preisschritt zusätzliche Positionen wiedereröffnen, solange der Trend anhält. Stop-Loss und Take-Profit werden vom letzten Einstiegspreis gemessen.

## Indikator
Die Strategie benötigt keine separate Indikatordatei. Sie berechnet Signale mithilfe von:
- **Williams %R** mit Periode `SSP`.
- **ATR** mit fester Periode 15.
Der Trend wechselt, wenn Williams %R die Schwellenwerte `-100 + K` und `-K` kreuzt, wobei `K = 33 - Risk`.

## Handelsregeln
1. Bei einem bullischen Signal werden Short-Positionen geschlossen (wenn erlaubt) und eine Long-Position eröffnet.
2. Bei einem bärischen Signal werden Long-Positionen geschlossen (wenn erlaubt) und eine Short-Position eröffnet.
3. Während eine Position offen ist, werden in derselben Richtung alle `Price Step` Einheiten neue Positionen hinzugefügt, bis `Max Positions` erreicht ist.
4. Jede Position hat Stop-Loss- und Take-Profit-Abstände gemessen vom letzten Einstiegspreis.

## Parameter
- `Risk` – Risikofaktor, der die Indikatorschwellenwerte definiert.
- `SSP` – Williams %R-Periode.
- `Price Step` – Preisabstand für das Hinzufügen einer neuen Position.
- `Max Positions` – maximale Anzahl offener Positionen pro Seite.
- `Stop Loss` – Stop-Loss-Abstand in Preiseinheiten.
- `Take Profit` – Take-Profit-Abstand in Preiseinheiten.
- `Enable Long Open` – Long-Positionen eröffnen erlauben.
- `Enable Short Open` – Short-Positionen eröffnen erlauben.
- `Enable Long Close` – Long-Positionen bei Gegensignal schließen erlauben.
- `Enable Short Close` – Short-Positionen bei Gegensignal schließen erlauben.
- `Candle Type` – für Berechnungen verwendeter Zeitrahmen.
