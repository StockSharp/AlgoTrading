# BandsPrice-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine Übertragung des **i-BandsPrice**-Experten aus MetaTrader. Sie verwendet Bollinger Bänder, um die relative Position des Kurses innerhalb des Kanals zu messen und reagiert, wenn der Wert die Extremzonen verlässt.

## Logik

1. Bollinger Bänder mit konfigurierbarer Periode und Abweichung aufbauen.
2. Die Kursposition innerhalb des Bandes als Wert zwischen -50 und +50 berechnen.
3. Den Wert mit einem einfachen gleitenden Durchschnitt glätten.
4. Einen Farbcode generieren:
   - `4` wenn der geglättete Wert über dem oberen Level liegt.
   - `0` wenn der geglättete Wert unter dem unteren Level liegt.
   - Andere Zahlen repräsentieren Zwischenzonen.
5. Eine Long-Position wird eröffnet, wenn der Indikator die obere Zone verlässt (`4` → nicht `4`).
6. Eine Short-Position wird eröffnet, wenn der Indikator die untere Zone verlässt (`0` → positiv).
7. Long-Positionen werden geschlossen, wenn der Wert nicht-positiv wird.
8. Short-Positionen werden geschlossen, wenn der Wert nicht-negativ wird.

## Parameter

| Name | Beschreibung |
|------|--------------|
| **BuyOpen** | Long-Einstiege aktivieren. |
| **SellOpen** | Short-Einstiege aktivieren. |
| **BuyClose** | Schließen von Long-Positionen aktivieren. |
| **SellClose** | Schließen von Short-Positionen aktivieren. |
| **BandsPeriod** | Periode der Bollinger Bänder. |
| **BandsDeviation** | Abweichung für die Bänder. |
| **Smooth** | Glättungslänge für den internen Wert. |
| **UpLevel** | Oberer Schwellenwert, Standard `25`. |
| **DnLevel** | Unterer Schwellenwert, Standard `-25`. |
| **CandleType** | Kerzen-Zeitrahmen für Berechnungen. |

## Hinweise

Diese Strategie zeigt, wie man indikatorbasierte Logik von MetaTrader nach StockSharp migriert, indem man die High-Level-API mit `SubscribeCandles` und `Bind` verwendet.
