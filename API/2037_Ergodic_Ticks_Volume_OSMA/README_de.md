# Ergodic Ticks-Volumen-OSMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie adaptiert den MQL5-Experten "Exp_Ergodic_Ticks_Volume_OSMA" für StockSharp. Der ursprüngliche Experte verwendet einen benutzerdefinierten Indikator zur Bewertung des Tick-Volumen-Momentums. In dieser Version wird der benutzerdefinierte Indikator durch das MACD-Histogramm approximiert.

Die Strategie sucht nach aufeinanderfolgenden Anstiegen oder Rückgängen im Histogramm:
- Zwei steigende Schritte lösen einen Long-Einstieg aus und schließen alle Short-Positionen.
- Zwei fallende Schritte lösen einen Short-Einstieg aus und schließen alle Long-Positionen.

`StartProtection()` wird verwendet, um Konflikte mit bestehenden Positionen beim Start der Strategie zu vermeiden.

## Parameter
- `FastLength` – schnelle EMA-Periode für den MACD. Standard: 12.
- `SlowLength` – langsame EMA-Periode für den MACD. Standard: 26.
- `SignalLength` – Signal-EMA-Periode für den MACD. Standard: 9.
- `CandleType` – Kerzen-Zeitrahmen, Standard 8 Stunden.

## Handelslogik
1. Kerzen des gewählten `CandleType` abonnieren.
2. MACD-Histogramm für jede abgeschlossene Kerze berechnen.
3. Wenn das Histogramm für zwei aufeinanderfolgende Balken wächst, Shorts schließen und kaufen.
4. Wenn das Histogramm für zwei aufeinanderfolgende Balken fällt, Longs schließen und verkaufen.
5. Bei jeder neuen Kerze weiterverarbeiten.
