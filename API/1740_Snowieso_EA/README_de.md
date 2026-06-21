# Snowieso-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert einen schnellen und langsamen **Linear Weighted Moving Average (LWMA)** mit **MACD** und dem **Kaufman Adaptive Moving Average (KAMA)**, um die Trendrichtung zu bestätigen.

## Funktionsweise
1. Abonnieren von Kerzen des gewählten Zeitrahmens.
2. Berechnung der Werte von Fast LWMA, Slow LWMA, MACD und KAMA.
3. **Long-Einstieg**: tritt auf, wenn die schnelle LWMA die langsame LWMA nach oben kreuzt, das MACD-Histogramm positiv ist und KAMA steigt.
4. **Short-Einstieg**: tritt auf, wenn die schnelle LWMA die langsame LWMA nach unten kreuzt, das MACD-Histogramm negativ ist und KAMA fällt.
5. Ein fester Stop Loss und Take Profit werden über `StartProtection` angewendet.

Die Strategie schließt entgegengesetzte Positionen vor dem Öffnen neuer und visualisiert Indikatoren und Trades auf einem Chart.

## Parameter
- `FastLength` – Periode der schnellen LWMA.
- `SlowLength` – Periode der langsamen LWMA.
- `MacdFast`, `MacdSlow`, `MacdSignal` – MACD-Konfiguration.
- `KamaLength` – Rückblickperiode für KAMA.
- `StopLossPoints` – absoluter Stop Loss in Preispunkten.
- `TakeProfitPoints` – absoluter Take Profit in Preispunkten.
- `CandleType` – Zeitrahmen der verarbeiteten Kerzen.

## Verwendung
Setzen Sie die Strategie auf dem gewählten Instrument ein. Der Algorithmus abonniert automatisch Kerzen und verwaltet Positionen basierend auf Indikatorsignalen. Die High-Level-API wird für die Datenbindung und Orderausführung verwendet.
