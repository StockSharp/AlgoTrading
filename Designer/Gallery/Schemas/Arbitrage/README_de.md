# Paarhandels-Strategie für BTC und TON
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Paarhandels-Strategie für BTC und TON handelt Bitcoin (BTC) und Toncoin (TON). Sie erkennt Arbitragemöglichkeiten, wenn das Preisverhältnis der beiden Werte eine konfigurierte Schwelle überschreitet.

![schema](schema.svg)

Die Strategie implementiert Mechanismen zum Kauf einer Kryptowährung bei gleichzeitigem Verkauf der anderen, mit dem Ziel, von vorübergehenden Abweichungen in ihren Werten zu profitieren. Dies macht die Strategie attraktiv für diejenigen, die Verdienstmöglichkeiten aus minimalen Marktfluktuationen suchen, ohne an den allgemeinen Markttrend gebunden zu sein.

## Installation

Zur Aktivierung und Nutzung dieser Strategie muss StockSharp Designer installiert sein. Die Strategie steht zum Download und zur Installation aus der [Strategie-Galerie](https://doc.stocksharp.com/de/topics/designer/strategy_gallery.html) zur Verfügung. Dies ermöglicht eine einfache Integration und Anpassung der Strategie gemäß den individuellen Anforderungen des Traders.

## Parameter

- **Vermögenswert 1**: TONUSDT@BNBFT
- **Vermögenswert 2**: BTCUSDT@BNBFT
- **Schwellenwert**: 0.02 (absolut)
- **Handelsvolumen**: 5000 (absolut)
- **Slippage**: 1.0 (absolut)
- **Max. Orders**: 3 (absolut)

## Funktionsweise

1. **Preisdatenerfassung**: Die Strategie nutzt die mitgelieferte Binance-Futures-Historie für BTC und TON.
2. **Preisberechnung**: Sie berechnet das Preisverhältnis zwischen BTC und TON.
3. **Signalgenerierung**: Wenn das Preisverhältnis den definierten Schwellenwert überschreitet, generiert die Strategie Kauf- und Verkaufssignale.
4. **Orderausführung**: Die Strategie führt Market Orders aus, um den unterbewerteten Vermögenswert zu kaufen und den überbewerteten zu verkaufen.
5. **Gewinnberechnung**: Sie berechnet den Gewinn auf Basis der ausgeführten Trades und überwacht den Markt auf weitere Möglichkeiten.

## Testing

Es ist wichtig, die Strategie auf historischen Daten zu testen, um ihre Effektivität und potenzielle Risiken zu bewerten, bevor sie auf dem realen Markt angewendet wird. Dies hilft dabei, optimale Parameter für den Schwellenwert der Preisabweichungen und das Kapitalmanagement zu bestimmen.


## Weitere Ressourcen

Für weitere Informationen und Ressourcen besuchen Sie die [StockSharp-Dokumentation](https://doc.stocksharp.com/de/).
