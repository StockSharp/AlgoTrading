# Rotationsstrategie nach Asset-Klassen-Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Rotationsmodell allokiert Kapital in die Asset-Klassen mit dem stärksten jüngsten Momentum. Jede Periode rankt das System Asset-ETFs und hält die Führenden, während es Nachzügler meidet.

Das Rebalancing erfolgt monatlich, mit Cash als defensivem Asset, wenn kein Momentum positiv ist.

## Details

- **Daten**: Monatliche Gesamtrenditen von Asset-Klassen-ETFs.
- **Einstieg**: Die Top-N-Assets mit positivem Momentum halten.
- **Ausstieg**: Assets ersetzen, wenn sie aus dem Top-Ranking fallen.
- **Instrumente**: Breit aufgestellte Asset-Klassen-ETFs.
- **Risiko**: Verwendung eines Cash-Proxys und Positionsobergrenzen.

