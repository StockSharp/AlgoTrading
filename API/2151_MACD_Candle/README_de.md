# MACD-Kerzen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert den MetaTrader-Experten "Exp_MACDCandle". Sie wandelt die Farbausgabe eines MACD-basierten Kerzen-Indikators mithilfe der StockSharp High-Level-API in Handelssignale um.

## Konzept

Der MACD Candle-Indikator baut synthetische Kerzen aus MACD-Werten auf, die auf den Eröffnungs- und Schlusskursen berechnet werden. Liegt der auf dem Schlusskurs berechnete MACD über dem auf dem Eröffnungskurs berechneten MACD, gilt die Kerze als bullisch (Farbe 2). Das Gegenteil ergibt eine bärische Kerze (Farbe 0). Eine neutrale Farbe (1) erscheint, wenn beide Werte gleich sind.

Die Strategie eröffnet Long-Positionen, wenn nach einer nicht-bullischen Kerze eine bullische erscheint, und eröffnet Short-Positionen, wenn eine bärische Kerze auf eine nicht-bärische folgt. Bestehende Positionen werden in die neue Richtung umgekehrt.

## Parameter

- `FastLength` – schnelle EMA-Periode für MACD (Standard 12).
- `SlowLength` – langsame EMA-Periode für MACD (Standard 26).
- `SignalLength` – Signallinie-Periode für MACD (Standard 9).
- `CandleType` – für Berechnungen verwendeter Kerzentyp, Standard `TimeFrameCandle` mit einem Vier-Stunden-Zeitraum.

Alle Parameter sind konfigurierbar und unterstützen die Optimierung.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der MACD auf dem Schlusskurs steigt über den MACD auf dem Eröffnungskurs, während die vorherige Kerze nicht bullisch war.
- **Short-Einstieg**: Der MACD auf dem Eröffnungskurs steigt über den MACD auf dem Schlusskurs, während die vorherige Kerze nicht bärisch war.
- **Ausstieg**: Die Strategie schließt die aktuelle Position, wenn ein entgegengesetztes Signal auftritt; kein expliziter Stop-Loss oder Take-Profit wird angewendet.

## Hinweise

- Die Strategie verwendet Market-Orders (`BuyMarket` und `SellMarket`).
- Signale werden nur auf abgeschlossenen Kerzen ausgewertet, um Rauschen zu vermeiden.
- Das Beispiel dient Bildungszwecken und beinhaltet kein Risikomanagement.
