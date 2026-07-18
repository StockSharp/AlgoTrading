# Vlado-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Momentum-Umkehrstrategie basierend auf dem klassischen Williams %R Oszillator von Larry Williams. Das System wartet, bis der Oszillator extreme Überverkauft- oder Überkauft-Lesungen erreicht, und kehrt dann die Position bei der nächsten abgeschlossenen Kerze um. Der StockSharp-Port behält den diskretionären Charakter der ursprünglichen MetaTrader-Implementierung bei und legt alle wichtigen Einstellungen als Parameter offen.

## Übersicht

- **Kategorie**: Mean-Reversion-Oszillator-Strategie.
- **Markt**: Jedes liquide Instrument, das stabile Kerzendaten liefert (Forex-Paare, Index-Futures, Krypto-Spot-Paare).
- **Zeitrahmen**: Konfigurierbar über `CandleType`. Standard: 1-Stunden-Kerzen, passend zum ursprünglichen Anwendungsbeispiel.
- **Richtung**: Long und Short. Die Engine hält stets höchstens eine Position und dreht um, wenn das entgegengesetzte Signal erscheint.
- **Indikator**: Williams %R mit konfigurierbarer Lookback-Länge und Schwellenwerten.

## Funktionsweise

1. Abonniert den ausgewählten Kerzendaten-Feed und berechnet Williams %R für jede abgeschlossene Kerze.
2. Verwendet den Standard-Überverkauft-Level von -75 und den Überkauft-Level von -25 (Werte sind negativ aufgrund der Oszillatorskala).
3. Wenn %R unter den Überverkauft-Level fällt, eröffnet die Strategie eine Long-Position oder dreht dahin um.
4. Wenn %R über den Überkauft-Level steigt, eröffnet die Strategie eine Short-Position oder dreht dahin um.
5. Orders werden mit `Volume + Math.Abs(Position)` bemessen, sodass eine Umkehr die bestehende Position schließt und die neue in einer einzigen Market-Order eröffnet.
6. Es werden kein expliziter Stop-Loss oder Take-Profit verwendet. Das Risiko wird durch die Indikatorlevels und den gewählten Zeitrahmen kontrolliert.
7. Jede Aktion wird über `LogInfo` protokolliert, was die Prüfung von Trades in der StockSharp-GUI oder in Log-Dateien erleichtert.

## Parameter

- `WilliamsPeriod`: Anzahl der Kerzen zur Berechnung des Oszillators. Höhere Werte glätten das Signal, niedrigere reagieren schneller.
- `OverboughtLevel`: Schwellenwert, ab dem der Markt als überkauft gilt (Standard -25). Kann optimiert werden.
- `OversoldLevel`: Schwellenwert, ab dem der Markt als überverkauft gilt (Standard -75). Kann optimiert werden.
- `CandleType`: Kerzentyp und Zeitrahmen für alle Berechnungen. Funktioniert mit Zeitrahmen, Volumenkerzen oder Range-Bars.
- `Volume` (von `Strategy` geerbt): Definiert die Basis-Ordergröße. An Kontogröße und Risikobereitschaft anpassen.

## Handelsregeln

- **Long-Einstieg**: Ausgelöst, wenn `%R <= OversoldLevel` und die aktuelle Position flat oder short ist.
- **Short-Einstieg**: Ausgelöst, wenn `%R >= OverboughtLevel` und die aktuelle Position flat oder long ist.
- **Ausstieg**: Implizit durch die Umkehrorder, wenn ein entgegengesetztes Signal erscheint.
- **Positionsmanagement**: Immer eine offene Position. Der Algorithmus führt keine Pyramidisierung oder gestaffelte Ausstiege durch.

## Zusätzliche Hinweise

- Funktioniert am besten in seitwärtslaufenden oder langsam trendenden Märkten, in denen Oszillatoren zwischen Extremen pendeln können.
- Es wird empfohlen, die Strategie mit externen Risikokontrollen (Eigenkapital-Stops, Session-Filter) für den Livehandel zu kombinieren.
- Die Implementierung enthält Chart-Rendering: der Hauptbereich zeigt Kerzen und Trades, während ein zweites Panel Williams %R darstellt.
- Für weitere Forschung konzipiert: Jeder Parameter unterstützt die Optimierung innerhalb der StockSharp-Optimierer.
