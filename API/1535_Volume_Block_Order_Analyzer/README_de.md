# Volumen-Block-Order-Analysator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Vereinfachte Strategie, basierend auf dem TradingView-Skript **"Volume Block Order Analyzer"**. Sie misst, wie große Volumenspitzen die Preisrichtung beeinflussen, und akkumuliert diesen Effekt im Laufe der Zeit. Wenn der kumulierte Einfluss benutzerdefinierte Schwellenwerte überschreitet, eröffnet die Strategie Trades und schützt sie mit einem Trailing-Stop.

## Details

- **Einstieg**: Kumulierter Einfluss über oder unter dem Schwellenwert.
- **Ausstieg**: Trailing-Stop basierend auf Prozent vom Einstieg.
- **Long/Short**: Beide.
- **Indikatoren**: SMA.
- **Zeitrahmen**: Beliebig.

Dieser Port konzentriert sich auf die Kernidee; viele visuelle Merkmale des Originalskripts sind weggelassen.
