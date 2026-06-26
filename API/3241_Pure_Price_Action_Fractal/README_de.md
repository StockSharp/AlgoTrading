# Pure Price Action Fractal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Pure Price Action-Strategie** ist ein StockSharp-Port des MetaTrader-Expertenberaters "Pure Price Action" (MQL-ID 24291).
Sie kombiniert Ausbruchsbestätigung durch Bill-Williams-Fraktale mit einem auf einem höheren Zeitrahmen berechneten Momentum-Filter und einem langfristigen MACD-Trendfilter.
Der Algorithmus versucht, Trendfortsetzungs-Trades genau dann zu erfassen, wenn der Markt das jüngste Fraktalniveau erneut testet.

## Handelslogik
1. **Signalkerzen** – Handelsentscheidungen werden auf dem vom Benutzer gewählten Zeitrahmen getroffen (standardmäßig 15 Minuten).
2. **Fraktal-Berührungs-Bestätigung** – Ein Trade ist nur erlaubt, wenn die neueste abgeschlossene Kerze innerhalb eines Preisschritts des zuletzt bestätigten Fraktal-Niveaus schließt (oberes Fraktal für Shorts, unteres Fraktal für Longs).
3. **Direktionales Körpermuster** – Die absolute Körpergröße der neuesten Kerze muss kleiner sein als der Körper der vorherigen Kerze, während der vorherige Körper größer sein muss als die davor. Dies imitiert den Momentum-Rücksetzer-Filter des ursprünglichen EA.
4. **Gleitende Durchschnitte** – Zwei linear gewichtete gleitende Durchschnitte (LWMA 6 und LWMA 85 standardmäßig) liefern den Basistrend. Long-Trades erfordern den schnellen LWMA über dem langsamen; Short-Trades das Gegenteil.
5. **Momentum-Filter** – Ein 14-Perioden-Momentum-Indikator auf einem höheren Zeitrahmen (H1 standardmäßig) muss vom Gleichgewichtsniveau (100) um mindestens den konfigurierten Schwellenwert abweichen, während einer der drei letzten Momentum-Lesungen.
6. **MACD-Filter** – Ein MACD(12, 26, 9) auf einem höheren Zeitrahmen (monatlich standardmäßig) muss die Hauptlinie über der Signallinie für Longs und darunter für Shorts zeigen.
7. **Positionsgrößenbestimmung** – Die Strategie verwendet die `Volume`-Eigenschaft der Basisklasse `Strategy`. Wenn `Volume` nicht gesetzt ist, wird standardmäßig ein Kontrakt/Los verwendet. Der Parameter `MaxPosition` begrenzt die absolute Positionsgröße.

## Positionsmanagement
- **Anfangsschutz** – Optionale feste Stop-Loss- und Take-Profit-Abstände werden in Preisschritten angegeben und symmetrisch auf beide Seiten angewendet.
- **Trailing Stop** – Wenn aktiviert, verfolgt die Strategie den nach dem Einstieg erreichten höchsten/niedrigsten Preis mit dem konfigurierten Abstand.
- **Break-Even-Sperre** – Nachdem der Preis die Aktivierungsdistanz zurückgelegt hat, wird das Schutzniveau auf Einstieg ± Offset verschoben, um Gewinne zu sichern.
- **Manuelle Ausstiege** – Die Logik bewertet Stop-Loss-, Take-Profit-, Trailing- und Break-Even-Niveaus bei jeder abgeschlossenen Kerze und schließt die gesamte Position wenn eine Bedingung erfüllt ist.

## Parameter
- `CandleType` – Hauptsignal-Zeitrahmen (Standard: 15-Minuten-Zeitrahmen).
- `MomentumCandleType` – Zeitrahmen für den Momentum-Indikator (Standard: 1-Stunden-Zeitrahmen).
- `MacdCandleType` – Zeitrahmen für den MACD-Filter (Standard: 30-Tage-Zeitrahmen, simuliert Monatskerzen).
- `FastPeriod` / `SlowPeriod` – Perioden des schnellen und langsamen LWMA.
- `MomentumPeriod` – Länge des Momentum-Indikators.
- `MomentumThreshold` – Mindestabweichung des Momentum von 100.
- `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` – MACD-Konfiguration.
- `StopLossPoints`, `TakeProfitPoints` – Risikoschutz-Abstände in Preisschritten.
- `TrailingStopPoints` – Trailing-Abstand in Preisschritten.
- `BreakEvenTriggerPoints`, `BreakEvenOffsetPoints` – Break-Even-Auslöser und gesicherter Gewinnabstand.
- `MaxPosition` – Maximale absolute Positionsgröße der Strategie.
- `EnableTrailing`, `EnableBreakEven`, `UseStopLoss`, `UseTakeProfit` – Schalter für Risikomanagement-Blöcke.

## Hinweise
- Alle Kommentare im Code sind auf Englisch geschrieben, wie von den Projektrichtlinien gefordert.
- Die Strategie verlässt sich ausschließlich auf abgeschlossene Kerzen; intra-bar Signale werden nicht verarbeitet.
- Multi-Zeitrahmen-Abonnements werden verwendet, um das Verhalten des ursprünglichen Expertenberaters zu emulieren (M15-Signalkerzen, H1-Momentum, monatlicher MACD standardmäßig).
- Keine automatischen Tests in diesem Ordner. Die globale Repository-Testsuite soll unberührt bleiben, wie angefordert.
