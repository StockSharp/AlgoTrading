# MLTrendE-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt in Richtung eines gewichteten gleitenden Durchschnitts (WMA) und baut Positionen optional aus, wenn sich der Preis günstig entwickelt.

## Logik

- Berechnung eines WMA der ausgewählten Kerzen-Reihe.
- Wenn keine Position offen ist:
  - **Handelstyp 0**: Long-Position eröffnen, wenn der Schlusskurs über dem WMA liegt, oder Short-Position, wenn er darunter liegt.
  - **Handelstyp 1**: immer eine Long-Position eröffnen.
  - **Handelstyp 2**: immer eine Short-Position eröffnen.
- Wenn eine Position offen ist und das festgelegte Gewinnziel erreicht, wird ein weiterer Trade mit skaliertem Volumen hinzugefügt.
- Sobald die maximale Anzahl an Trades erreicht ist, wird die gesamte Position beim nächsten Gewinnziel geschlossen.

## Parameter

- `Volume` – Basis-Handelsvolumen.
- `Multiplier1` – Volumenmultiplikator für den zweiten Trade.
- `Multiplier2` – Volumenmultiplikator für den dritten Trade.
- `TakeProfit` – Gewinn in Preiseinheiten zum Skalieren oder Schließen.
- `Map` – Periode des gewichteten gleitenden Durchschnitts.
- `MaxTrades` – maximale Anzahl aufeinanderfolgender Trades.
- `TradeType` – 0 Trendfolge, 1 erzwinge Long, 2 erzwinge Short.
- `CandleType` – Zeitrahmen der analysierten Kerzen.

## Hinweise

Die Strategie verwendet nur abgeschlossene Kerzen und Marktorders. Sie verwaltet weder Stops noch Risiko; nutzen Sie bei Bedarf Kontoschutz.
