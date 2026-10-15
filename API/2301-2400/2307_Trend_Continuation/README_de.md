# Trendfortsetzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie identifiziert die Fortsetzung des vorherrschenden Trends mithilfe eines Paares exponentieller gleitender Durchschnitte auf Preisdaten. Eine Long-Position wird eröffnet, wenn der schnelle EMA den langsamen EMA von unten nach oben kreuzt, was eine Aufwärtsfortsetzung signalisiert. Eine Short-Position wird eröffnet, wenn der schnelle EMA den langsamen EMA von oben nach unten kreuzt.

## Parameter
- **Fast EMA Length** – Periode für den schnellen EMA (Standard: 20).
- **Candle Type** – Zeitrahmen der Kerzen (Standard: 4 Stunden).
- **Stop Loss** – Schutz-Stop-Loss angewendet über `StartProtection` (Standard: 1000).
- **Take Profit** – Gewinnziel angewendet über `StartProtection` (Standard: 2000).

## Funktionsweise
1. Beim Start abonniert die Strategie die ausgewählte Kerzenserie und erstellt zwei EMA-Indikatoren.
2. Jede abgeschlossene Kerze wird verarbeitet, um Kreuzungen zwischen dem schnellen und langsamen EMA zu erkennen.
3. Eine Kreuzung von unten nach oben eröffnet eine Long-Position und schließt jede Short-Position. Die entgegengesetzte Kreuzung eröffnet eine Short-Position und schließt jede Long-Position.
4. Das Risikomanagement wird über die integrierten Stop-Loss- und Take-Profit-Parameter gehandhabt.

Dieses Beispiel ist eine vereinfachte Konvertierung des ursprünglichen MQL-Experten `Exp_TrendContinuation`.
