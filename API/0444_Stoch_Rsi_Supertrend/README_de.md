# Stochastic RSI SuperTrend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses System verbindet die schnellen Schwingungen des Stochastic RSI mit einem
Trendfilter und einem vereinfachten SuperTrend-Modell. Der Oszillator hebt kurzfristige
Momentum-Extreme hervor, während der gleitende Durchschnitt und die ATR-Bänder den
dominanten Trend definieren. Trades werden nur eröffnet, wenn die %K-Linie %D innerhalb
der relevanten Zone kreuzt und der übergeordnete Trend ausgerichtet ist, was Fehlsignale
in Seitwärtsphasen reduziert.

Die Standardkonfiguration konzentriert sich auf Long-Trades, kann aber optional
Short-Einstiege aktivieren. Die Strategie ist für Intraday- bis Swing-Zeitrahmen
konzipiert, in denen Stochastic RSI-Signale häufig auftreten und die ATR-basierten
Bänder einen volatilitätsadaptiven Bias liefern. Ausstiege erfolgen bei entgegengesetzten
Kreuzungen, sodass der Markt laufen kann, bis das Momentum nachlässt.

## Details

- **Einstiegskriterien**:
  - **Long**: Schlusskurs über Trend-MA, %K < 20, %K kreuzt %D von unten, SuperTrend zeigt Aufwärtstrend.
  - **Short**: Schlusskurs unter Trend-MA, %K > 80, %K kreuzt %D von oben, SuperTrend zeigt Abwärtstrend.
- **Long/Short**: Long standardmäßig, Short optional.
- **Ausstiegskriterien**:
  - **Long**: %K > 80 und kreuzt %D von oben.
  - **Short**: %K < 20 und kreuzt %D von unten.
- **Stops**: Standardmäßig keine; können extern hinzugefügt werden.
- **Standardwerte**:
  - RSI-Periode = 14, Stochastic-Länge = 14.
  - MA-Typ = EMA, MA-Länge = 100.
  - ATR-Periode = 10, ATR-Faktor = 3.0.
- **Filter**:
  - Kategorie: Momentum + Trend
  - Richtung: Überwiegend Long
  - Indikatoren: RSI, ATR, Gleitender Durchschnitt
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Kurz/mittel
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
