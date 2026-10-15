# BullsBearsEyes EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist ein StockSharp-Port des **BullsBearsEyes EA** für MetaTrader 5. Sie baut den benutzerdefinierten Indikator neu auf, indem die klassischen Bulls Power- und Bears Power-Oszillatoren mit demselben vierstufigen IIR-Smoothing aus dem Originalcode kombiniert werden. Das resultierende Verhältnis oszilliert zwischen 0 und 1 und spiegelt das Übergewicht von Verkäufern oder Käufern wider. Wenn das Verhältnis auf **0** fällt, gilt der Markt als von Bären ausgewaschen und die Strategie bereitet einen Long-Einstieg vor. Wenn das Verhältnis auf **1** steigt, gilt der Bullendruck als erschöpft und die Strategie sucht nach einem Short-Einstieg. Alle Berechnungen werden nur auf vollständig geschlossenen Kerzen durchgeführt, replicierend die MQL-Implementierung, die `custom[1]` bei der Geburt jeder neuen Bar auswertete.

## Handelslogik
1. Die konfigurierte Kerzenreihe abonnieren und Bulls Power- und Bears Power-Indikatoren binden.
2. Bei jeder fertigen Kerze werden die Indikatorwerte durch dieselbe IIR-Smoothing-Kaskade (`L0` – `L3`) wie der originale EA geführt.
3. Das Verhältnis `CU / (CU + CD)` wird berechnet. Eine rein bärische Sequenz macht `CU` gleich null, während eine rein bullische Sequenz `CD` auf null zwingt.
4. Die Strategie speichert das Verhältnis der vorherigen Kerze und verwendet es als handelbares Signal:
   - Vorheriges Verhältnis gleich **0** ⇒ Short-Positionen schließen und Long-Position eröffnen.
   - Vorheriges Verhältnis gleich **1** ⇒ Long-Positionen schließen und Short-Position eröffnen.
   - Zwischenwerte werden ignoriert, um dem Quellcode treu zu bleiben.
5. Orders werden mit dem aktuellen `Volume`-Wert gesendet und netten automatisch die entgegengesetzte Position vor dem Öffnen einer neuen.

## Risikomanagement
- **Stop Loss / Take Profit** – in Pips ausgedrückt, in absolute Preise übersetzt mit Pip-Größenerkennung identisch zur MT5-Implementierung (5- und 3-stellige Instrumente werden über den Schritt-Multiplikator gehandhabt).
- **Trailing Stop / Trailing Step** – identische Logik: sobald der Preis um `TrailingStop + TrailingStep` vorrückt, wird der Stop bewegt, um eine konstante `TrailingStop`-Distanz vom aktuellen Schluss beizubehalten. Long- und Short-Positionen werden symmetrisch verwaltet.
- Schutz-Niveaus werden immer dann neu berechnet, wenn sich die Nettoposition ändert, um sicherzustellen, dass der durchschnittliche Positionspreis für weitere Berechnungen verwendet wird.
- Die Strategie schließt die gesamte Position, wenn ein Schutzniveau innerhalb des aktuellen Kerzenbereichs verletzt wird.

## Sitzungsfilter
Der optionale Zeitfilter entspricht den Expertenberater-Eingaben:
- `Use Time Control` – aktiviert/deaktiviert den Filter.
- `Start Hour` – inklusive Startzeit (0–23).
- `End Hour` – exklusive Endzeit (0–23). Wenn die Endzeit kleiner als die Startzeit ist, erstreckt sich die Sitzung über Mitternacht.
Während eingeschränkter Stunden enthält sich die Strategie vom Öffnen neuer Positionen, verwaltet aber weiterhin Stops und Trailing für bestehende Trades.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `Period` | Mittelungslänge für Bulls/Bears Power. | 13 |
| `Gamma` | Glättungsfaktor für den vierstufigen Filter (0–1). | 0.6 |
| `StopLossPips` | Stop-Loss-Abstand in Pips. | 150 |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. | 150 |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips (0 deaktiviert Trailing). | 25 |
| `TrailingStepPips` | Minimaler Vorstoß, bevor der Trailing Stop sich bewegen kann. | 5 |
| `UseTimeControl` | Aktiviert den Handelssitzungsfilter. | true |
| `StartHour` | Erste Handelsstunde (inklusiv). | 10 |
| `EndHour` | Letzte Handelsstunde (exklusiv). | 16 |
| `CandleType` | Kerzentyp/Zeitrahmen für die Analyse. | 1-Stunden-Kerzen |

## Zusätzliche Hinweise
- Die High-Level-API `SubscribeCandles().Bind(...)` wird verwendet, um die originalen Berechnungen ohne manuelles Kerzensammeln zu replizieren.
- Indikatorwerte werden erst nach dem Kerzenschluss verarbeitet (`CandleStates.Finished`).
- Die Pip-Größenerkennung fällt auf `1` zurück, wenn der Instrumentenschritt nicht verfügbar ist, sodass die Strategie in synthetischen Testumgebungen ausgeführt werden kann.
- Inline-Kommentare in der C#-Datei erläutern jeden logischen Abschnitt für einfachere Wartung und Vergleich mit der MQL-Quelle.
