# Bollinger-Ausbruch-Richtung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt Bollinger-Bands-Ausbrüche, gefiltert durch RSI. Richtung kann long, short oder beides sein. Verwendet feste Stop Loss und Take Profit basierend auf dem Risiko-Ertrags-Verhältnis.

## Details

- **Daten**: Preiskerzen.
- **Einstieg**: Long wenn Schlusskurs über dem oberen Band und RSI über der Mittellinie; Short wenn Schlusskurs unter dem unteren Band und RSI unter der Mittellinie.
- **Ausstieg**: Stop Loss und Take Profit aus Risiko/Ertrag.
- **Instrumente**: Beliebige Instrumente.
- **Risiko**: Konfigurierbare Stop- und Zielniveaus.
