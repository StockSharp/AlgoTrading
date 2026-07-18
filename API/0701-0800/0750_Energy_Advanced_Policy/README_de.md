# Strategie für Fortgeschrittene Energiepolitik
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Energy Advanced Policy**-Strategie kombiniert politisches Sentiment mit grundlegenden technischen Filtern.

- **Long**: EMA(21) über EMA(55), RSI unterhalb der Überkauft-Zone, Bollinger-Bänder nicht komprimiert.
- **Ausstieg**: RSI kreuzt über die Überkauft-Zone oder EMA-Trend dreht um.

## Parameter
- `NewsSentiment` – manuelles Sentiment.
- `EnableNewsFilter` – Politik-Sentiment-Überschreibung aktivieren.
- `EnablePolicyDetection` – Erkennung politischer Ereignisse erlauben.
- `PolicyVolumeThreshold` – Volumen-Spike-Multiplikator.
- `PolicyPriceThreshold` – Preisänderungsschwelle (%).
- `RsiLength` – RSI-Periode.
- `RsiOverbought` – RSI-Überkauft-Level.
- `FastLength` – schnelle EMA-Periode.
- `SlowLength` – langsame EMA-Periode.
- `BbLength` / `BbMult` – Bollinger-Bänder-Einstellungen.

Indikatoren: RSI, EMA, Bollinger Bands.
