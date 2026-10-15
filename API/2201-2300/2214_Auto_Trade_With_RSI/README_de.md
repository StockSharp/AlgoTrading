# Automatisierter Handel mit RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie mittelt die letzten RSI-Werte, um Handelssignale zu erzeugen. Sie berechnet einen Standard-Relative Strength Index (RSI) über eine konfigurierbare Periode und wendet dann einen einfachen gleitenden Durchschnitt auf den RSI selbst an. Trades werden eröffnet, wenn der gemittelte RSI vordefinierte Schwellenwerte kreuzt, und geschlossen, wenn der entgegengesetzte Schwellenwert erreicht wird.

## Handelslogik

1. **RSI-Berechnung**
   - Der Indikator verwendet `RsiPeriod`, um den RSI basierend auf Kerzenschlusskursen zu berechnen.
2. **RSI-Mittelung**
   - Die letzten `AveragePeriod` RSI-Werte werden durch einen einfachen gleitenden Durchschnitt geglättet.
3. **Einstiegsregeln**
   - Wenn `BuyEnabled` `true` ist und keine Position offen ist, wird eine **Kauforder** gesendet, wenn der gemittelte RSI `BuyThreshold` (Standard 55) überschreitet.
   - Wenn `SellEnabled` `true` ist und keine Position offen ist, wird eine **Verkaufsorder** gesendet, wenn der gemittelte RSI unter `SellThreshold` (Standard 45) fällt.
4. **Ausstiegsregeln**
   - Wenn `CloseBySignal` `true` ist, werden offene Positionen bei entgegengesetzten Signalen geschlossen:
     - Long-Positionen schließen, wenn der gemittelte RSI unter `CloseBuyThreshold` (Standard 47) fällt.
     - Short-Positionen schließen, wenn der gemittelte RSI über `CloseSellThreshold` (Standard 52) steigt.

## Parameter

- `BuyEnabled` – Long-Einstiege aktivieren oder deaktivieren.
- `SellEnabled` – Short-Einstiege aktivieren oder deaktivieren.
- `CloseBySignal` – Ausstiege bei entgegengesetzten RSI-Signalen erlauben.
- `RsiPeriod` – RSI-Berechnungslänge.
- `AveragePeriod` – Anzahl der RSI-Werte für die Mittelung.
- `BuyThreshold` – gemittelter RSI-Wert, über dem eine Long-Position eröffnet wird.
- `SellThreshold` – gemittelter RSI-Wert, unter dem eine Short-Position eröffnet wird.
- `CloseBuyThreshold` – gemittelter RSI-Wert, unter dem eine Long-Position geschlossen wird.
- `CloseSellThreshold` – gemittelter RSI-Wert, über dem eine Short-Position geschlossen wird.
- `CandleType` – Kerzentyp für Abonnements.

## Hinweise

Diese Strategie zeigt, wie Indikatorwerte durch Bindung in der StockSharp High-Level-API kombiniert werden können. Trailing-Stop- und Geldverwaltungsfunktionen aus der ursprünglichen MQL-Version werden der Einfachheit halber weggelassen.

