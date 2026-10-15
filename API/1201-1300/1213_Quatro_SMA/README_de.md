# Quatro SMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert drei schnelle einfache gleitende Durchschnitte (SMAs) mit einem langfristigen SMA und einem Volumenfilter. Eine Long-Position wird eröffnet, wenn der schnellste SMA über dem mittleren SMA liegt, der mittlere über dem langsamen SMA, der Preis über dem langen SMA liegt und das Volumen seinen Durchschnitt um einen konfigurierbaren Multiplikator überschreitet. Short-Positionen erfordern die entgegengesetzte Ausrichtung.

Die Position wird in mehreren Stufen geschlossen: bis zu drei Take-Profit-Niveaus und ein Stop-Loss können Teile des Trades schließen. Eine umgekehrte SMA-Ausrichtung schließt ebenfalls die Position.

## Details

- **Indikatoren**: SMA, Volumen
- **Zeitrahmen**: 4h
- **Typ**: Trendfolge mit Volumenbestätigung
- **Stops**: Drei Take-Profit-Niveaus und ein Stop-Loss
