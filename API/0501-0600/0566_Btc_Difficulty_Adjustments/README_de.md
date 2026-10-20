# BTC Difficulty Adjustments

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Handelsregeln

Erfordert `DifficultyHistoryFile` mit Kopfzeile `Time,Difficulty`. Veröffentlichungen sind strikt steigende ISO-UTC-Zeitstempel mit `Z`; die Schwierigkeit muss positiv sein. Die Datei wird beim Start gelesen. Fehlende oder ungültige Daten führen zu einem eindeutigen Fehler. Die Schwierigkeit wird nicht aus Marktpreisen abgeleitet.

Je abgeschlossener Kerze werden nur bis zu ihrem `OpenTime` veröffentlichte Werte verwendet. Der erste Wert bildet die Basis; unveränderte Werte erzeugen kein Signal. Änderung: `100*(difficulty/previous-1)`. Ohne Schwellenmodus kauft ein Anstieg und verkauft ein Rückgang. Im Schwellenmodus gilt nur `abs(change)>=Threshold`: Rückgang kauft, Anstieg verkauft. Die letzte zulässige beobachtete Änderung entscheidet. Ausgehend von null wird `Volume`, beim Richtungswechsel `Volume+abs(Position)` gehandelt. Offene Orders blockieren überlappende Entscheidungen. Kein automatischer Stop.

## Parameter

| Parameter | Standardwert |
|---|---|
| `CandleType` | `1d` |
| `ThresholdMode` | `false` |
| `Threshold` | `10` |
| `DifficultyHistoryFile` | `erforderlich` |
