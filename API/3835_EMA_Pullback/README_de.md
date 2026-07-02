# EMA Pullback-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die EMA Pullback-Strategie ist eine High-Level-Portierung des MetaTrader „Ema“-Expertenberaters. Es beobachtet ein Paar exponentieller gleitender Durchschnitte (EMA), wobei die Perioden 5 und 10 anhand der mittleren Kerzenpreise berechnet werden. Wenn ein bullischer oder bärischer Crossover auftritt, wartet die Strategie darauf, dass der Preis in Richtung des Extrems der vorherigen Kerze zurückkehrt, bevor sie in die Richtung des Crossovers eintritt. Feste Take-Profit- und Stop-Loss-Werte, gemessen in Preispunkten, steuern das Risiko, sobald die Position geöffnet ist.

## Handelslogik
1. Abonnieren Sie die konfigurierte Kerzenserie (Standard: 5-Minuten-Zeitrahmen) und berechnen Sie zwei EMAs zum Medianpreis `(high + low) / 2`.
2. Erkennen Sie einen bullischen Crossover, wenn der schnelle EMA den langsamen EMA kreuzt, oder einen bärischen Crossover, wenn der schnelle EMA den langsamen EMA kreuzt.
3. Aktivieren Sie einen Pullback-Einstieg, nachdem der Crossover erfolgt ist:
   - Warten Sie bei einem Long-Setup, bis der Schlusskurs auf das vorherige Kerzenhoch abzüglich des `MoveBackPoints`-Versatzes zurückgeht, während der schnelle EMA um mindestens zwei Preispunkte über dem langsamen EMA bleibt.
   - Warten Sie bei einem kurzen Setup, bis der Schlusskurs zum vorherigen Kerzentief zuzüglich des `MoveBackPoints`-Versatzes zurückkehrt, während der langsame EMA um mindestens zwei Preispunkte über dem schnellen EMA bleibt.
4. Wenn die Pullback-Bedingung erfüllt ist, senden Sie eine Marktorder mit dem konfigurierten Handelsvolumen.
5. Berechnen Sie beim Einstieg die statischen Take-Profit- und Stop-Loss-Werte mithilfe der Einstellungen `TakeProfitPoints` und `StopLossPoints`, umgewandelt in absolute Preisabweichungen vom Einstiegspreis.
6. Beobachten Sie jede fertige Kerze und schließen Sie die Position, sobald entweder das Take-Profit- oder Stop-Loss-Niveau durch das Hoch/Tief der Kerze berührt wird.

## Parameter
| Name | Standard | Beschreibung |
|------|---------|-------------|
| `TradeVolume` | `0.1` | Für jede Marktorder verwendetes Volumen. |
| `FastLength` | `5` | Periode des schnellen EMA, angewendet auf Durchschnittspreise. |
| `SlowLength` | `10` | Zeitraum der langsamen EMA angewendet auf mittlere Preise. |
| `MoveBackPoints` | `3` | Rückzugsdistanz in Preispunkten, gemessen vom Extrem der vorherigen Kerze. |
| `TakeProfitPoints` | `5` | Take-Profit-Distanz, in Preispunkten. |
| `StopLossPoints` | `20` | Stop-Loss-Distanz, in Preispunkten. |
| `CandleType` | `5m` | Zeitrahmen für Kerzenabonnements und Indikatorberechnungen. |

## Notizen
- Es werden nur vollständig ausgebildete Kerzen verarbeitet, um vorzeitige Signale zu vermeiden.
- Die Strategie gleicht die Eigenschaft `Strategy.Volume` beim Start automatisch mit dem Parameter `TradeVolume` aus.
- Alle Berechnungen basieren auf dem Instrument `PriceStep`, um punktbasierte Entfernungen in absolute Preise umzurechnen.
- Die Strategie eröffnet jeweils höchstens eine Position und erfordert einen neuen EMA-Crossover, bevor ein weiterer Trade vorbereitet wird.
