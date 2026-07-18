# Strategie zum Erfassen großer Kursbewegungen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie geht Long, wenn der Preis über dem oberen Bollinger Band schließt und alle aktivierten Filter die Bewegung bestätigen. Sie kann auch Short gehen, wenn der Preis unter dem unteren Band schließt. Zu den Filtern gehören RSI, ADX, ATR, EMA-Trendrichtung und MACD. Es wird ein fester prozentualer Stop-Loss angewendet, Positionen werden geschlossen, wenn der Preis zum mittleren Band zurückkehrt, und ein optionaler Zwangsgewinn schließt bei ungewöhnlich großen Kerzen.

## Details
- **Einstiegskriterien:**
  - **Long:** Schlusskurs > oberes Bollinger Band und alle aktiven Filter bestanden.
  - **Short:** Schlusskurs < unteres Bollinger Band und alle aktiven Filter bestanden.
- **Long/Short:** Beide (konfigurierbar).
- **Ausstiegskriterien:**
  - Preis kreuzt das mittlere Bollinger Band.
  - Optionaler Zwangsgewinn bei großen Kerzen.
- **Stops:** Fester prozentualer Stop-Loss.
- **Standardwerte:** Bollinger-Länge = 40, Stop-Loss = 2%, Zwangsgewinn-Schwellenwert = 5%.
- **Filter:** RSI (14), ADX (28), ATR (14), EMA (350), MACD (12,26,9).
