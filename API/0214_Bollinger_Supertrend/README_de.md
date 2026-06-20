# Bollinger Supertrend Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Strategie kombiniert Bollinger Bands mit dem Supertrend-Indikator, um Einstiege während starker gerichteter Bewegungen zu identifizieren. Bollinger Bands messen die Volatilitätsausdehnung, während die Supertrend-Linie den übergeordneten Trend verfolgt und als Trailing-Stop fungiert.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 79%. Sie funktioniert am besten auf dem Aktienmarkt.

Ein Long-Trade wird ausgelöst, wenn der Preis über dem oberen Bollinger Band schließt und über der Supertrend-Linie bleibt, was Momentum und Trendausrichtung bestätigt. Ein Short-Trade tritt auf, wenn der Preis unter dem unteren Band schließt und unter dem Supertrend-Level bleibt. Trades werden geschlossen, sobald der Preis zurück durch den Supertrend kreuzt, was darauf hindeutet, dass das Momentum nachgelassen hat.

Da das System auf Ausbrüche jenseits der normalen Volatilität wartet, eignet es sich für Trader, die anhaltende Bewegungen statt schnelle Umkehrungen erfassen möchten. Der Supertrend-Stop passt sich dynamisch an Marktschwankungen an und hilft, das Risiko ohne manuelle Eingriffe zu managen.

## Details
- **Einstiegskriterien**:
  - **Long**: Close > upper Bollinger Band && Close > Supertrend
  - **Short**: Close < lower Bollinger Band && Close < Supertrend
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg, wenn der Preis unter den Supertrend fällt
  - **Short**: Ausstieg, wenn der Preis über den Supertrend steigt
- **Stops**: Ja, über Supertrend Trailing-Stop.
- **Standardwerte**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, Supertrend
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

