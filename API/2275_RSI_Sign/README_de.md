# RSI Sign Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie konvertiert den ursprünglichen **iRSISign** Expert Advisor aus MQL5 in die StockSharp High-Level-API. Sie kombiniert den Relative Strength Index (RSI) mit dem Average True Range (ATR), um Einstiegs- und Ausstiegssignale zu generieren.

Das System wartet auf abgeschlossene Kerzen eines benutzerdefinierten Zeitrahmens. Wenn der RSI die untere Schwelle von unten nach oben kreuzt, signalisiert dies eine potenzielle bullische Umkehr und eröffnet eine Long-Position oder schließt eine bestehende Short-Position. Umgekehrt, wenn der RSI unter die obere Schwelle fällt, wird eine Short-Position eingegangen oder eine aktive Long-Position geschlossen. Der ATR wird berechnet, aber nur für zusätzlichen Kontext verwendet, analog zum Originalindikator, der Signalpfeile mit ATR-Versatz anzeigte.

## Details

- **Einstiegskriterien**:
  - **Long**: Vorheriger RSI-Wert war unter `DownLevel` und aktueller RSI kreuzt darüber.
  - **Short**: Vorheriger RSI-Wert war über `UpLevel` und aktueller RSI kreuzt darunter.
- **Long/Short**: Beide Richtungen sind erlaubt und können unabhängig aktiviert werden.
- **Ausstiegskriterien**:
  - Ein entgegengesetztes Signal schließt die aktuelle Position, wenn das entsprechende Schließ-Flag aktiviert ist.
- **Stops**: Nicht implementiert. Risikomanagement kann bei Bedarf extern hinzugefügt werden.
- **Standardwerte**:
  - `RsiPeriod` = 14
  - `AtrPeriod` = 14
  - `UpLevel` = 70
  - `DownLevel` = 30
  - `CandleType` = 1-Stunden-Kerzen
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: RSI, ATR
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Flexibel
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

## Parameter

| Name | Beschreibung |
|------|--------------|
| `RsiPeriod` | RSI-Länge. |
| `AtrPeriod` | ATR-Länge. |
| `UpLevel` | RSI-Oberschwelle, die Verkaufssignale generiert. |
| `DownLevel` | RSI-Unterschwelle, die Kaufsignale generiert. |
| `CandleType` | Für Berechnungen verwendeter Kerzen-Zeitrahmen. |
| `BuyOpen` | Öffnen von Long-Positionen aktivieren. |
| `SellOpen` | Öffnen von Short-Positionen aktivieren. |
| `BuyClose` | Schließen bestehender Longs bei entgegengesetztem Signal erlauben. |
| `SellClose` | Schließen bestehender Shorts bei entgegengesetztem Signal erlauben. |

Die Strategie ist als Lehrbeispiel gedacht, das demonstriert, wie man einfache MQL5-Logik in das High-Level-Strategie-Framework von StockSharp übersetzt.
