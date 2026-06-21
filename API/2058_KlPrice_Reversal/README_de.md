# KlPrice Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine C#-Konvertierung des ursprünglichen MQL5-Experten **exp_i-KlPrice.mq5**. Sie implementiert ein Umkehrsystem basierend auf einem normalisierten Kursoszillator. Der Oszillator vergleicht den aktuellen Kurs mit einem geglätteten Kursband, das aus einem gleitenden Durchschnitt und dem Average True Range (ATR) abgeleitet wird. Das Überschreiten vordefinierter Grenzen erzeugt Handelssignale.

## Funktionsweise

1. Ein einfacher gleitender Durchschnitt (SMA) glättet den Schlusskurs.
2. Ein Average True Range (ATR) schätzt die Marktvolatilität.
3. Der Oszillator wird berechnet als:
   
   `jres = 100 * (Close - (SMA - ATR)) / (2 * ATR) - 50`
4. Der Oszillatorwert wird fünf Farbzonen zugeordnet:
   - **4** – über dem oberen Level
   - **3** – zwischen null und dem oberen Level
   - **2** – zwischen dem oberen und unteren Level
   - **1** – zwischen dem unteren Level und null
   - **0** – unter dem unteren Level
5. Eine Long-Position wird eröffnet, wenn der Oszillator die Zone 4 verlässt. Eine Short-Position wird eröffnet, wenn er die Zone 0 verlässt. Bestehende Positionen werden geschlossen, wenn der Oszillator null kreuzt.

## Parameter

| Name | Beschreibung |
|------|--------------|
| `CandleType` | Zeitrahmen für Preisdaten. |
| `PriceMaLength` | SMA-Periode zur Preisglättung. |
| `AtrLength` | ATR-Periode zur Berechnung des Kursbandes. |
| `UpLevel` | Oberer Schwellenwert des Oszillators. |
| `DownLevel` | Unterer Schwellenwert des Oszillators. |
| `EnableBuy` | Eröffnung von Long-Positionen erlauben. |
| `EnableSell` | Eröffnung von Short-Positionen erlauben. |

## Verwendung

1. Eine Instanz von `KlPriceReversalStrategy` erstellen.
2. Gewünschte Parameter einstellen.
3. Strategie an ein Portfolio und ein Wertpapier anhängen.
4. Strategie starten, um Signale zu erhalten und Orders zu platzieren.

Die Strategie verwendet Market-Orders über `BuyMarket` und `SellMarket`. Der Positionsschutz wird durch `StartProtection()` aktiviert.

## Hinweise

- Die Implementierung approximiert den ursprünglichen MQL-Indikator mithilfe der integrierten StockSharp-Indikatoren (`SimpleMovingAverage` und `AverageTrueRange`).
- Alle Berechnungen werden nur auf abgeschlossenen Kerzen durchgeführt.
