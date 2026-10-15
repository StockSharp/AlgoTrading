# PSAR Multi-Timeframe-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie repliziert den Expertenberater MetaTrader **EA_PSar_002B**. Es wertet Parabolic SAR-Werte in drei Zeitrahmen (M15, M30 und H1) aus und verwaltet gleichzeitig Positionen in einem einminütigen Stream. Der Handel erfolgt direktional: Es kann jeweils nur eine Nettoposition aktiv sein und neue Trades erscheinen nur, wenn das vorherige Engagement flach ist. Der ursprüngliche Experte wurde für EURUSD auf dem M1-Chart entwickelt und der Port behält den gleichen Kontext bei.

## Handelslogik
1. **Parabolic SAR-Konvergenzfilter** – die neuesten SAR-Werte von M15, M30 und H1 müssen innerhalb von 19 Mindestpreisschritten voneinander liegen. Dadurch bleiben die drei Kurven „eng“, bevor ein Ausbruch zugelassen wird.
2. **Langer Eintrag** – eine der folgenden Sequenzen muss auftreten:
   - Die Werte für M15, M30 und H1 SAR liegen unter ihren jeweiligen aktuellen Tiefstständen, der vorherige H1 SAR lag über dem vorherigen H1-Hoch und der neue H1 SAR fällt unter den aktuellen H1-Tiefststand.
   - M15 und H1 SAR liegen unter ihren aktuellen Tiefstständen, während der vorherige M30 SAR über dem vorherigen M30-Hoch lag und der neue M30 SAR unter das aktuelle M30-Tief fällt.
   - M30 und H1 SAR liegen unter ihren aktuellen Tiefstständen, während der vorherige M15 SAR über dem vorherigen M15-Hoch lag und der neue M15 SAR unter das aktuelle M15-Tief fällt.
3. **Short-Einstieg** – Spiegelung der Bedingungen des Long-Setups mit umgekehrten Hochs/Tiefs.
4. **Take Profit / Stop Loss** – Limits werden in Punkten ausgedrückt (minimale Preiserhöhungen). Standardmäßig beträgt das Ziel 999 Punkte und der Schutzstopp 399 Punkte, was den MQL-Werten nach Normalisierung der 4/5-stelligen Notierungen entspricht.
5. **Dynamischer Ausstieg** – während eine Position offen ist, wird der M30 SAR überwacht.
   - Long-Positionen schließen, wenn der vorherige SAR unter dem vorherigen M1-Tief lag, der aktuelle SAR jedoch über das aktuelle M1-Hoch springt.
   - Shorts schließen, wenn der vorherige SAR über dem vorherigen M1-Hoch lag, der aktuelle SAR jedoch unter das aktuelle M1-Tief fällt.
   - Wenn der aktuelle M30 SAR den Einstiegspreis überschreitet, wird der Stop auf dieses SAR-Niveau verschoben.

## Geldmanagement
`UseMoneyManagement` reproduziert den Geldverwaltungswechsel von EA. Wenn deaktiviert, wird der Parameter `FixedVolume` verwendet. Wenn diese Option aktiviert ist, wird der angeforderte Prozentsatz des Portfoliokapitals in eine synthetische „Lot“-Größe umgewandelt, wobei die gleiche Formel wie in der MQL-Version verwendet wird (Prozent des freien Kapitals dividiert durch 100.000). Der Betrag wird an `Security.VolumeStep` angepasst und auf die Broker-Limits (`VolumeMin`/`VolumeMax`) begrenzt.

## Parameter
- `BaseCandleType` – Zeitrahmen für die Handelsverwaltung (standardmäßig M1).
- `FastSarCandleType`, `MediumSarCandleType`, `SlowSarCandleType` – Zeitrahmen für die SAR-Filter (Standard: 15 m, 30 m, 60 m).
- `EnableParabolicFilter` – spiegelt das Flag `sar2` von MQL wider; Wenn Sie es ausschalten, wird der Handel vollständig gestoppt.
- `TakeProfitPoints`, `StopLossPoints` – Offsets in Punkten (minimale Preiserhöhungen). Die Pip-Größe wird aus `Security.PriceStep` und `Security.Decimals` abgeleitet, um 3/5-stellige Forex-Kurse korrekt zu verarbeiten.
- `UseMoneyManagement`, `PercentMoneyManagement`, `FixedVolume` – oben beschriebene Lautstärkeregler.

## Konvertierungshinweise
- Es wird nur das übergeordnete StockSharp API verwendet. Alle Preisreihen werden über `SubscribeCandles().Bind(...)` abonniert und Indikatordaten werden über Bindungen statt über manuelle Puffer empfangen.
- Schutzanordnungen werden durch explizite Marktaustritte umgesetzt, genau wie das ursprüngliche Skript, das `OrderClose` aufrief.
- Der Broker-Ziffernkoeffizient von MQL wird durch die automatische Erkennung der Pip-Größe ersetzt (`PriceStep` × 10 für 3/5-stellige Instrumente).
- Der EA verbot den Handel mit Nicht-EURUSD-Symbolen oder Nicht-M1-Charts durch das Drucken von Nachrichten. In StockSharp bleiben die Strategieprotokolle stumm, aber das Verhalten wird hier dokumentiert.

## Anwendungstipps
1. Hängen Sie die Strategie mit Ein-Minuten-Kerzen für das Basisabonnement an EURUSD an. Die Zeitrahmen des Indikators können immer noch geändert werden, wenn Experimente gewünscht sind.
2. Stellen Sie sicher, dass die Sicherheitsmetadaten `PriceStep`/`Decimals` offenlegen. Ohne sie fallen die Stopp- und Zielentfernungen auf die Einheitsgröße 1 zurück.
3. Lassen Sie `EnableParabolicFilter` aktiviert; es entspricht dem Hauptschalter des EA. Deaktivieren Sie es nur, wenn Sie absichtlich möchten, dass die Strategie untätig bleibt.
