# Trigger-Line-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Trigger-Line-Strategie kombiniert eine gewichtete Trendlinie mit einem Least-Squares-Moving-Average (LSMA). Eine Long-Position wird eröffnet, wenn die gewichtete Trendlinie über die LSMA kreuzt, während eine Short-Position eröffnet wird, wenn sie darunter kreuzt.

## Funktionsweise
- **Long-Einstieg**: die gewichtete Trendlinie kreuzt über die LSMA.
- **Long-Ausstieg**: die gewichtete Trendlinie kreuzt unter die LSMA.
- **Short-Einstieg**: die gewichtete Trendlinie kreuzt unter die LSMA.
- **Short-Ausstieg**: die gewichtete Trendlinie kreuzt über die LSMA.
- **Indikatoren**: Gewichteter gleitender Durchschnitt, Lineare Regression (LSMA).

## Parameter
- **WT Period** – Rückblickperiode für die gewichtete Trendlinie.
- **LSMA Period** – Glättungsperiode für die LSMA.
- **Candle Type** – Zeitrahmen der für Berechnungen verwendeten Kerzen.
