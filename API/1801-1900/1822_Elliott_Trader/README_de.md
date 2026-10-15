# Elliott Trader Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine Strategie, die gestaffelte Positionen eröffnet, wenn der Stochastik-Oszillator auf Vier-Stunden-Kerzen Extremwerte erreicht. Es wird zunächst eine Marktorder platziert, gefolgt von einem Raster aus Limitorders. Positionen werden geschlossen, sobald ein Gewinnziel erreicht ist und der Trend durch gleitende Durchschnitte und Bollinger Bänder bestätigt wird.

## Einstiegsregeln
- Stochastik-Oszillator (%K-Länge 21, Glättung 3) auf H4-Kerzen verwenden.
- Wenn %K ≥ **Überkauft**-Niveau:
  - Zum Marktpreis verkaufen.
  - Bis zu acht zusätzliche `SellLimit`-Orders oberhalb des aktuellen Preises in konfigurierten Pip-Abständen platzieren.
- Wenn %K ≤ **Überverkauft**-Niveau:
  - Zum Marktpreis kaufen.
  - Bis zu acht zusätzliche `BuyLimit`-Orders unterhalb des aktuellen Preises in konfigurierten Pip-Abständen platzieren.

## Ausstiegsregeln
- Realisierter Gewinn erreicht **ProfitTarget** und Preis bestätigt Trend:
  - Long-Positionen werden geschlossen, wenn der Kurs über dem unteren Bollinger Band liegt und die 200-Perioden-SMA über der 55-Perioden-SMA liegt.
  - Short-Positionen werden geschlossen, wenn der Kurs unter dem oberen Bollinger Band liegt und die 200-Perioden-SMA unter der 55-Perioden-SMA liegt.
- Ausstehende Kauf-Limits werden storniert, wenn %K ≥ 90 und die 200-Perioden-SMA ≤ 55-Perioden-SMA.
- Ausstehende Verkauf-Limits werden storniert, wenn %K ≤ 10 und die 200-Perioden-SMA ≥ 55-Perioden-SMA.

## Parameter
- `StochLength` – %K-Periode für den Stochastik.
- `OverboughtLevel` – Niveau, ab dem verkauft wird.
- `OversoldLevel` – Niveau, ab dem gekauft wird.
- `ProfitTarget` – erforderlicher realisierter Gewinn zum Schließen offener Positionen.
- `Order2Offset` … `Order9Offset` – Pip-Abstände für zusätzliche Limitorders.
- `CandleType` – Zeitrahmen der Kerzen, Standard 4 Stunden.

## Indikatoren
- StochasticOscillator
- BollingerBands
- SMA (200 und 55)
