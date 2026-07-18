# CCI + EMA-Strategie mit prozentualem oder ATR TP/SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert den Commodity Channel Index (CCI) mit einem optionalen EMA-Trendfilter und RSI-Bestätigung.
Positionen werden eröffnet, wenn der CCI extreme Zonen verlässt und optionale Filter den Handel zulassen.
Take-Profit und Stop-Loss können entweder als Prozentsätze des Einstiegspreises oder mithilfe von ATR-basierten Niveaus mit einem Risiko-Ertrags-Verhältnis berechnet werden.

## Details

- **Einstiegsbedingungen:**
  - **Long:** CCI kreuzt den überverkauften Level von unten, Preis über EMA (falls aktiviert), RSI unter überverkauft (falls aktiviert).
  - **Short:** CCI kreuzt den überkauften Level von oben, Preis unter EMA (falls aktiviert), RSI über überkauft (falls aktiviert).
- **Ausstiegsbedingungen:**
  - Take-Profit- oder Stop-Loss-Niveaus erreicht.
  - Long-Positionen schließen, wenn CCI den überkauften Level von unten kreuzt.
  - Short-Positionen schließen, wenn CCI den überverkauften Level von oben kreuzt.

Die Standardparameter folgen dem Originalskript.
