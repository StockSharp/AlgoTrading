# TrendGuard Scalper SSL + Hama Candle mit Konsolidierungszonen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert einen einfachen SSL-Kanal mit der Hama-Kerzenrichtung. Eine Long-Position wird eröffnet, wenn der Schlusskurs über dem SSL-Durchschnitt liegt, der Hama-Schlusskurs (EMA 20) über der langen Hama-Linie (EMA 100) liegt und der Preis über dem Hama-Schlusskurs bleibt. Short-Trades verwenden die entgegengesetzten Bedingungen. ATR wird verwendet, um Perioden niedriger Volatilität als mögliche Konsolidierungszonen zu markieren.

## Details
- **Einstieg**: SSL- und Hama-Trend stimmen überein und der Preis bestätigt das Signal.
- **Ausstieg**: feste Take‑Profit- und Stop‑Loss-Prozentsätze.
- **Indikatoren**: SMA, EMA, ATR.
- **Filter**: Konsolidierungserkennung nur zur Analyse.
