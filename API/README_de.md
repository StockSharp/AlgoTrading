# StockSharp-API-Strategiekatalog
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Verzeichnis enthält in C# und Python implementierte Strategie-Beispiele für die StockSharp API. Die Strategieordner sind in Nummernbereiche (`0001-0100`, `0101-0200` usw.) aufgeteilt; die folgenden Seiten gruppieren alle Strategien nach ihrer primären Handelsidee.

Jeder Katalogeintrag verlinkt direkt auf beide Implementierungsordner und verwendet ein transparentes, theme-fähiges SVG-Logo.

**Strategien:** 3811

**Implementierungen:** C# und Python

## Strategietypen

- [Arbitrage, Paare und Relative Value (25)](StrategyTypes/arbitrage-pairs-relative-value_de.md) — Strategien, die Preisbeziehungen zwischen Instrumenten, Spreads oder verbundenen Vermögenswerten handeln, statt sich auf eine einzelne Richtungsprognose zu stützen.
- [Mittelwertrückkehr und Umkehrungen (299)](StrategyTypes/mean-reversion-reversals_de.md) — Gegentrend-Systeme suchen überdehnte Preise, erschöpfte Bewegungen oder gescheiterte Trends und handeln die Rückkehr zum Gleichgewicht oder zu einem Umkehrpunkt.
- [Ausbrüche und Kanäle (319)](StrategyTypes/breakouts-channels_de.md) — Strategien rund um das Verlassen einer Spanne, das Überschreiten von Unterstützung oder Widerstand oder das Durchlaufen einer berechneten Kanalgrenze.
- [Volumen, VWAP und Orderflow (63)](StrategyTypes/volume-vwap-order-flow_de.md) — Systeme, die Handelsvolumen, VWAP, Liquidität, Markttiefe oder Orderflow zur Bestimmung von Ein- und Ausstiegen verwenden.
- [Candlestick- und Preismuster (191)](StrategyTypes/candlestick-price-patterns_de.md) — Strategien, die Kerzenformationen, Chartstrukturen, Gaps, Pivots und andere wiederkehrende Muster direkt in der Price Action erkennen.
- [Saisonalität, Sitzungen und Ereignisse (92)](StrategyTypes/seasonal-session-event_de.md) — Zeitabhängige Systeme auf Basis von Sitzungen, Kalendern, geplanten Ereignissen, Eröffnungsspannen oder saisonalem Verhalten.
- [Statistik, adaptive Modelle und KI (77)](StrategyTypes/statistical-adaptive-ai_de.md) — Quantitative Strategien mit statistischer Schätzung, adaptiven Modellen, maschinellem Lernen, neuronalen Netzen oder Signalklassifikation.
- [Faktoren, Portfolio und Rotation (24)](StrategyTypes/factor-portfolio-rotation_de.md) — Multi-Asset-Ansätze, die Instrumente einstufen, Kapital nach Faktoren verteilen, Portfolios neu gewichten oder zwischen Märkten rotieren.
- [Grid, DCA und Positionsmanagement (143)](StrategyTypes/grid-dca-position-management_de.md) — Strategien mit Orderleitern, Mittelung, gestaffelten Einstiegen, Positionsgröße, Ausstiegen und laufendem Trade-Management.
- [Scalping und Ausführung (133)](StrategyTypes/scalping-execution_de.md) — Kurzfristige Systeme, bei denen Einstiegstiming, Spread, Orderplatzierung und Ausführung den Handelsvorteil bestimmen.
- [Volatilität und Optionen (78)](StrategyTypes/volatility-options_de.md) — Strategien auf Basis von Volatilitätsregimen, Spannenexpansion oder -kontraktion, Derivaten, Optionsbewertung und Volatilitätsrisiko.
- [Gleitende Durchschnitte und Kreuzungen (191)](StrategyTypes/moving-averages-crossovers_de.md) — Trendsysteme rund um Richtung, Ausrichtung, Verschiebung und Bänder gleitender Durchschnitte sowie schnelle und langsame Kreuzungen.
- [Direktionale Trendindikatoren (264)](StrategyTypes/directional-trend-indicators_de.md) — Strategien, die von Trend- und Richtungsstärke-Werkzeugen wie ADX/DMI, SuperTrend, Parabolic SAR, Ichimoku und Alligator geführt werden.
- [Momentum- und Oszillatortrend (206)](StrategyTypes/momentum-oscillator-trend_de.md) — Direktionale Strategien, bestätigt durch Momentum, MACD, RSI, CCI, Stochastik, ROC, Divergenzen und verwandte Oszillatoren.
- [Ausbrüche, Rücksetzer und Price Action (95)](StrategyTypes/breakouts-pullbacks-price-action_de.md) — Trendfortsetzungs-Einstiege über Ausbrüche, Rücksetzer, Kanäle, Swings, Kerzen, Retracements und Marktstruktur.
- [Adaptive, Multi-Timeframe- und spezialisierte Trends (277)](StrategyTypes/adaptive-multitimeframe-specialized-trend_de.md) — Adaptive, zeitrahmenübergreifende, modellgetriebene, hybride und spezialisierte Trendsysteme ohne Dominanz einer einzelnen klassischen Indikatorfamilie.
- [Oszillatoren und Indikatorsignale (203)](StrategyTypes/oscillators-indicator-signals_de.md) — Strategien, deren Hauptauslöser von Oszillatoren, Indikatorschwellen, Indikatorkreuzungen oder Divergenzen stammt.
- [Orders, Risiko und Positionsmanagement (194)](StrategyTypes/order-risk-position-management_de.md) — Systeme für Orderhandling, Größenbestimmung, Schutz, Grids, Recovery, Trailing und Verwaltung bestehender Positionen.
- [Indikatorkombinationen und Signallogik (319)](StrategyTypes/indicator-combinations-signal-logic_de.md) — Zusammengesetzte Einstiegsregeln aus Indikatorübereinstimmung, Schwellen, Kreuzungen, Divergenzen und Signalauswahl.
- [Preisniveaus, Muster und Marktstruktur (263)](StrategyTypes/price-levels-patterns-market-structure_de.md) — Spezialisierte Systeme auf Basis von Preisniveaus, Spannen, Pivots, Fibonacci-Geometrie, Wellen, Kerzen und Marktstruktur.
- [Quantitative, adaptive und experimentelle Strategien (25)](StrategyTypes/quantitative-adaptive-experimental_de.md) — Mathematische, statistische, lernende, adaptive, randomisierte und bewusst experimentelle Strategieentwürfe.
- [Werkzeuge, Panels, Alarme und Vorlagen (74)](StrategyTypes/tools-panels-alerts-templates_de.md) — Trading-Werkzeuge, UI-Panels, Alarme, Vorlagen, Testumgebungen, Charthilfen, Bibliotheken und Integrationsbeispiele.
- [Fundamentale, makroökonomische und asset-spezifische Strategien (22)](StrategyTypes/fundamental-macro-asset-specific_de.md) — Spezialisierte Logik zu Fundamentaldaten, Makrodaten, Berichten, Anlageklassen oder bestimmten Instrumenten und Märkten.
- [Zeit-, Sitzungs- und Ereignisregeln (13)](StrategyTypes/time-session-event-rules_de.md) — Zusammengesetzte Strategien, deren prägende Einschränkung eine Sitzung, ein Zeitfenster, ein Kalenderereignis oder ein wiederkehrender Zeitplan ist.
- [Direktionaler und regelbasierter Handel (111)](StrategyTypes/directional-rule-based-trading_de.md) — Spezialisierte direktionale Systeme mit ausdrücklichen Long/Short-, Kauf/Verkauf-, Trend-, Umkehr-, Einstiegs- oder Ausstiegsregeln.
- [Zusammengesetzte Expertensysteme (110)](StrategyTypes/composite-expert-systems_de.md) — Mehrkomponenten-, Hybrid- und Ensemble-Systeme sowie Roboter, Trader und Expert Advisors, die mehrere Mechanismen verbinden.

## Repository-Struktur

Jedes nummerierte Strategieverzeichnis enthält eine Übersicht sowie die Implementierungsordner `CS` und `PY`. Die Typseiten bieten Tabellen mit Kurzbeschreibungen und direkten Logo-Links.

## Kompatibilität

Die Beispiele sind für die [StockSharp API](https://github.com/StockSharp/StockSharp) konzipiert und können an Workflows mit StockSharp Designer, Shell und Runner angepasst werden.
