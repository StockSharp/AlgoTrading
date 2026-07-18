# Ma2Cci EMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie mit doppeltem Kreuzungsystem zweier exponentieller gleitender Durchschnitte, bestätigt durch einen Zero-Line-Break des Commodity Channel Index (CCI). Positionsgröße und Stop-Platzierung werden aus der Average-True-Range (ATR)-Volatilität und einem konfigurierbaren Risikoprozentsatz abgeleitet.

## Details

- **Daten**: Zeitbasierte Kerzen (Standard 1 Stunde) vom gewählten `Candle Type`-Parameter.
- **Einstieg**: Long gehen, wenn die schnelle EMA die langsame EMA nach oben kreuzt und der CCI auf derselben Kerze die Nulllinie nach oben durchbricht; Short bei der entgegengesetzten Kreuzung mit CCI-Bruch unter null.
- **Ausstieg**: Longs schließen, wenn die schnelle EMA wieder unter die langsame EMA kreuzt oder der Preis den festen Stop berührt; Shorts schließen, wenn die schnelle EMA über die langsame EMA kreuzt oder der Preis den Short-Stop erreicht.
- **Risiko**: Die Stop-Distanz entspricht dem Größeren aus ATR (Länge `AtrPeriod`) oder `MinStopPoints` multipliziert mit dem Kursschritt des Instruments. Die Handelsgröße ist der Portfoliowert mal `RiskPercent`, geteilt durch diese Stop-Distanz.
- **Instrumente**: Trendfolgende Forex- oder Index-Symbole, die Hedging in der ursprünglichen MetaTrader-Version unterstützen; auch für andere Assets mit klaren Momentum-Schwingungen geeignet.
- **Umgebung**: Entwickelt für Märkte mit Dauersitzungen, bei denen EMA/CCI-Signale mit ATR-basierten Risikokontrollen übereinstimmen.

## Parameter

- `CandleType` – Zeitrahmen und Datentyp für Berechnungen und Orderfluss.
- `FastMaPeriod` – Periode der schnellen EMA (Standard 10).
- `SlowMaPeriod` – Periode der langsamen EMA (Standard 37).
- `CciPeriod` – Lookback des CCI-Oszillators zur Momentum-Bestätigung (Standard 39).
- `AtrPeriod` – ATR-Länge zur Schätzung der aktuellen Volatilität für die Stop-Platzierung (Standard 3).
- `RiskPercent` – Anteil des aktuellen Portfolio-Eigenkapitals, der pro Trade riskiert wird (Standard 2%).
- `MinStopPoints` – Minimaler Stop-Abstand in Preisschritten zur Emulation des MetaTrader-Pip-Filters (Standard 15).

## Hinweise

- Funktioniert am besten bei liquiden Paaren und Indizes, bei denen EMA/CCI-Kreuzungen zuverlässig sind; dünne Märkte können vorzeitige Ausstiege auslösen.
- Da Stops nur beim Einstieg neu berechnet werden, hält die Strategie das Risikoprofil stabil und spiegelt die feste Stop-Loss-Logik des ursprünglichen MQL-Experten wider.
- Die Portfolio-Bewertung muss vom verbundenen Konto bereitgestellt werden, damit die Positionsgrößenbestimmung funktioniert; andernfalls fällt die Engine auf das `Volume` der Strategie oder das Mindestvolumen des Instruments zurück.
