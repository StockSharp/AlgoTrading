# Fußballclub-Arbitrage-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie sucht nach Arbitragemöglichkeiten zwischen Fan-Tokens von Fußballclubs, die an mehreren Handelsplätzen gehandelt werden. Durch die Beobachtung von Preisspreads und Funding-Rate-Ungleichgewichten werden gegenläufige Long- und Short-Positionen eröffnet, um Fehlbewertungen zu nutzen.

Ein Trade wird ausgelöst, wenn der Spread zwischen den Börsen einen Schwellenwert überschreitet. Positionen werden abgesichert und geschlossen, wenn sich die Preise annähern oder ein Schutz-Stop erreicht wird.

## Details

- **Daten**: Fan-Token-Preise und Funding Rates.
- **Einstieg**: Gegenläufige Positionen öffnen, wenn Spread > X%.
- **Ausstieg**: Schließen, wenn Spread < Y% oder beim Zeit-Stop.
- **Instrumente**: An Börsen gelistete Fan-Tokens.
- **Risiko**: Fest prozentualer Stop zum Schutz vor Slippage.

