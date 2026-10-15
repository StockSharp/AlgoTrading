# DVD-Level-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine vereinfachte Übersetzung des originalen MQL5-Expertenberaters "DVD Level". Sie verwendet den Range Action Verification Index (RAVI) zur Bestimmung der Marktrichtung. RAVI wird mit exponentiellen gleitenden Durchschnitten mit Perioden 2 und 24 auf 1-Stunden-Kerzen berechnet.

## Parameter
- `Volume` – Ordervolumen für Trades.

## Logik
1. 1-Stunden-Kerzen abonnieren und EMA(2) und EMA(24) berechnen.
2. `RAVI = (EMA2 - EMA24) / EMA24 * 100` berechnen.
3. Wenn RAVI unter null kreuzt, kauft die Strategie, wenn sie flat oder short ist.
4. Wenn RAVI über null kreuzt, verkauft die Strategie, wenn sie flat oder long ist.
5. Der integrierte Positionsschutz wird über `StartProtection()` aktiviert.

Der Ansatz erfasst potenzielle Umkehrungen, wenn der kurzfristige Momentum vom langfristigen Trend abweicht.
