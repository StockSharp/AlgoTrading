# VR ZVER-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die VR ZVER-Strategie ist ein Trendfolge-System, das drei Bestätigungsschichten kombiniert: ein schnelles/langsames/sehr langsames EMA-Stack, den Stochastischen Oszillator und den Relative Strength Index (RSI). Alle aktiven Filter müssen übereinstimmen, bevor eine Position eröffnet wird, was dabei hilft, Trades während turbulenter und widersprüchlicher Marktphasen zu vermeiden. Die Konvertierung behält die ursprüngliche Break-Even- und Schutzlogik bei, während die High-Level-API von StockSharp verwendet wird.

## Marktregime-Erkennung
1. **EMA-Struktur** – Die Standardkonfiguration verwendet exponentielle gleitende Durchschnitte mit den Perioden 3, 5 und 7. Ein Long-Bias erfordert, dass der schnelle EMA über dem langsamen EMA liegt und der langsame EMA über dem sehr langsamen EMA bleibt. Ein Short-Bias kehrt diese Beziehung um.
2. **Stochastischer Oszillator** – Das %K/%D-Paar wird sowohl auf Richtung als auch auf Niveau geprüft. Long-Trades erfordern, dass %K unter dem unteren Band und über %D liegt, was einen überverkauften Abprall signalisiert. Short-Trades erfordern, dass %K über dem oberen Band und unter %D liegt, was auf eine überkaufte Umkehr hindeutet.
3. **RSI-Filter** – Der RSI muss unter dem unteren Schwellenwert liegen, um Long-Einstiege zu erlauben, oder über dem oberen Schwellenwert, um Short-Trades zu ermöglichen.

Nur wenn jeder aktivierte Filter übereinstimmt, sendet die Strategie eine Marktorder mit dem konfigurierten Volumen.

## Risikomanagement
- **Stop Loss** – Jeder Einstieg projiziert einen preisbasierten Stop mit der `StopLossPips`-Einstellung multipliziert mit der Pip-Größe des Instruments. Long-Positionen gehen aus, wenn das Kerzentief den Stop durchsticht, während Short-Positionen schließen, wenn das Kerzenhoch ihren Stop erreicht.
- **Take Profit** – Ein symmetrisches Take-Profit-Niveau wird angewendet. Wenn die aktuelle Kerze das Ziel zugunsten des Trades erreicht, wird die Position sofort geschlossen.
- **Breakeven-Schutz** – Nachdem der Preis die `BreakevenPips`-Distanz vorangeschritten ist, wird ein Breakeven-Modus aktiviert. Jede Rückbewegung zum Einstiegspreis wird die Position abflachen, um Kapital zu erhalten.
- **Order-Bereinigung** – Alle aktiven Orders werden vor dem Eröffnen eines neuen Trades storniert, um unbeabsichtigtes Stapeln zu vermeiden.

## Parameter
| Parameter | Beschreibung |
|-----------|--------------|
| `CandleType` | Für Berechnungen verwendete Kerzenserie. |
| `UseMovingAverage` | Aktiviert oder deaktiviert den EMA-Trendfilter. |
| `FastMaPeriod`, `SlowMaPeriod`, `VerySlowMaPeriod` | Perioden für schnelle, langsame und sehr langsame EMAs. |
| `UseStochastic` | Schaltet die stochastische Bestätigungsschicht um. |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing` | Periodeneinstellungen für den Stochastischen Oszillator. |
| `StochasticUpperLevel`, `StochasticLowerLevel` | Überkauft- und Überverkauft-Schwellenwerte für %K. |
| `UseRsi` | Aktiviert oder deaktiviert die RSI-Bestätigungsschicht. |
| `RsiPeriod` | RSI-Mittelungsperiode. |
| `RsiUpperLevel`, `RsiLowerLevel` | RSI-Schwellenwerte, die Überkauft-/Überverkauft-Bereiche definieren. |
| `StopLossPips`, `TakeProfitPips` | Distanzen (in Pips) für Stop-Loss- und Take-Profit-Platzierung. |
| `BreakevenPips` | Preisfortschritt, der vor dem Aktivieren des Breakeven-Schutzes erforderlich ist. |
| `Volume` | Menge für jede Marktorder. |

## Implementierungshinweise
- Die Pip-Größe wird aus dem Preisschritt des Instruments und der Anzahl der Dezimalstellen abgeleitet. Instrumente mit 3 oder 5 Dezimalstellen wenden automatisch die Standard-10x-Anpassung an, die in der ursprünglichen MQL-Version verwendet wird.
- Alle Indikatordaten werden über `BindEx` abgerufen, wodurch sichergestellt wird, dass die Strategie nur auf abgeschlossene Kerzen mit finalisierten Indikatorwerten reagiert.
- Die Strategie ist standardmäßig flat; Positionen werden niemals umgekehrt, ohne die bestehende Exposure zuerst zu schließen.
