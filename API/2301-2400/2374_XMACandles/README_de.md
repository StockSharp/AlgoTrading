# XMA Candles-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Beschreibung
Die XMA Candles-Strategie überwacht die Richtung geglätteter Kerzen, die aus dem XMA (Exponential Moving Average) der Eröffnungs- und Schlusskurse berechnet werden. Eine Kerze gilt als **bullisch**, wenn der geglättete Eröffnungskurs unter dem geglätteten Schlusskurs liegt, und als **bärisch**, wenn der geglättete Eröffnungskurs darüber liegt. Die Strategie reagiert auf Farbwechsel dieser geglätteten Kerzen.

- Wenn eine neue bullische Kerze nach einer nicht-bullischen erscheint, schließt die Strategie alle Short-Positionen und öffnet eine Long-Position.
- Wenn eine neue bärische Kerze nach einer nicht-bärischen erscheint, schließt die Strategie alle Long-Positionen und öffnet eine Short-Position.

## Parameter
- `Length` – Anzahl der Perioden zur Glättung von Eröffnungs- und Schlusskursen.
- `CandleType` – Zeitrahmen der für Berechnungen verwendeten Kerzen.
- `BuyPosOpen` – erlaubt das Öffnen von Long-Positionen.
- `SellPosOpen` – erlaubt das Öffnen von Short-Positionen.
- `BuyPosClose` – erlaubt das Schließen von Long-Positionen bei bärischem Signal.
- `SellPosClose` – erlaubt das Schließen von Short-Positionen bei bullischem Signal.
- `StopLoss` – Schutz-Stop in Prozent.
- `TakeProfit` – Gewinnziel in Prozent.

## Handelsregeln
1. Auf den Abschluss jeder Kerze des gewählten Zeitrahmens warten.
2. Exponentielle gleitende Durchschnitte für Eröffnungs- und Schlusskurse berechnen.
3. Kerzenfarbe bestimmen:
   - Grün (bullisch), wenn geglättete Eröffnung < geglätteter Schluss.
   - Rot (bärisch), wenn geglättete Eröffnung > geglätteter Schluss.
4. Bei Farbwechsel zu bullisch: Shorts schließen und optional eine Long-Position eröffnen.
5. Bei Farbwechsel zu bärisch: Longs schließen und optional eine Short-Position eröffnen.
6. Schutz-Stops und Ziele werden durch integrierte Risikokontrollen verwaltet.

Diese Strategie ist eine Konvertierung des ursprünglichen MQL5-Experten „Exp_XMACandles".
