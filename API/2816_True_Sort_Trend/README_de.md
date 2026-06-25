# True Sort Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie repliziert das klassische „True Sort"-Template von MetaTrader, indem sie darauf wartet, dass sich fünf exponentielle gleitende Durchschnitte in strenger Reihenfolge ausrichten. Wenn sowohl die aktuelle als auch die vorherige abgeschlossene Kerze dieselbe bullische oder bärische Sortierung einhalten und der Average Directional Index (ADX) den Momentum bestätigt, öffnet die Strategie eine Position in Trendrichtung. Das Risiko wird durch optionale absolute Stop-Loss- und Take-Profit-Abstände zusammen mit einem Trailing-Stop kontrolliert, der nur aktiviert wird, nachdem der Preis weit genug zugunsten des Trades vorrückt.

## Funktionsweise

1. Fünf EMAs (schnell bis langsam: Standard 10, 20, 50, 100, 200 Perioden) auf der ausgewählten Kerzenserie aufbauen.
2. ADX mit einer konfigurierbaren Periode (Standard 24) berechnen, um zu qualifizieren, ob der Trend genug Stärke hat (Standard-Schwellenwert 20).
3. Nur im Moment des Kerzen-Schlusses analysieren wir die Indikatoren. Signale werden für unfertige Kerzen ignoriert, um vorzeitige Entscheidungen zu vermeiden.
4. Ein Long-Setup erfordert folgendes für die **aktuelle** und **vorherige** abgeschlossene Kerze:
   - `EMA_schnell > EMA_2 > EMA_3 > EMA_4 > EMA_langsam` (perfekte bullische Ausrichtung).
   - `ADX > Schwellenwert` um sicherzustellen, dass die Steigung signifikant ist.
5. Ein Short-Setup spiegelt das Obige mit allen umgekehrten Ungleichungen.
6. Positionen werden geschlossen, wenn die geordnete Ausrichtung bricht, wenn Schutzniveaus erreicht werden, oder wenn der Trailing-Stop eine konfigurierbare Menge an Gewinn zurückgibt.

Diese Logik hält die Strategie strikt in stark trendenden Märkten und erzwingt die Ausrichtung über zwei Bars, um Rauschen zu reduzieren.

## Handelsregeln

- **Einstieg**
  - **Long**: ADX über dem Schwellenwert und fünf EMAs von schnellster zu langsamster für sowohl die aktuelle als auch die vorherige fertige Kerze sortiert. Jede offene Short-Position wird zuerst geschlossen, dann wird ein neuer Long mit dem konfigurierten `Volume` eröffnet.
  - **Short**: ADX über dem Schwellenwert und EMAs in absteigender Reihenfolge für zwei aufeinanderfolgende Kerzen sortiert. Jede offene Long-Position wird geflacht, bevor der Short-Einstieg übermittelt wird.
- **Ausstieg**
  - Wenn die EMA-Ausrichtung ihre strenge Sortierung verliert, wird die Position sofort geschlossen.
  - Optionale Schutzausstiege:
    - Stop-Loss-Abstand in absoluten Preiseinheiten unterhalb (Long) oder oberhalb (Short) des Einstiegspreises.
    - Take-Profit-Abstand in absoluten Preiseinheiten jenseits des Einstiegspreises.
    - Trailing-Stop, der erst aktiviert wird, nachdem der Preis um `TrailingStopDistance + TrailingStepDistance` vorgerückt ist, und dann dem Preis bei `TrailingStopDistance` folgt.
  - Manuelle Schließungen oder externe Ausführungen setzen auch den internen Zustand zurück.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|--------------|---------|
| `CandleType` | Datentyp der für alle Berechnungen verwendeten Kerzen. | 1-Stunden-Zeitrahmen |
| `FastEmaLength` | Periode der schnellsten EMA (Einstiegsausrichtung). | 10 |
| `SecondEmaLength` | Periode der zweiten EMA. | 20 |
| `ThirdEmaLength` | Periode der dritten EMA. | 50 |
| `FourthEmaLength` | Periode der vierten EMA. | 100 |
| `SlowEmaLength` | Periode der langsamsten EMA, die den langfristigen Trend repräsentiert. | 200 |
| `AdxPeriod` | Durchschnittslänge für den ADX-Indikator. | 24 |
| `AdxThreshold` | Minimaler ADX-Wert, der erforderlich ist, um Trades zu erlauben. | 20 |
| `StopLossDistance` | Absoluter Preisabstand des Schutz-Stops (0 deaktiviert). | 0.005 |
| `TakeProfitDistance` | Absoluter Preisabstand des Gewinnziels (0 deaktiviert). | 0.015 |
| `TrailingStopDistance` | Abstand zwischen dem höchsten/niedrigsten Preis und dem Trailing-Ausstieg. | 0.0005 |
| `TrailingStepDistance` | Zusätzlicher Vorschub, der erforderlich ist, bevor der Trailing-Stop aktiviert oder bewegt wird. | 0.0001 |

Alle Distanzwerte werden in Preiseinheiten ausgedrückt. Für FX-Symbole mit vier oder fünf Dezimalstellen entsprechen Werte wie `0.005` ungefähr 50 Pips. Passen Sie die Zahlen an die Tick-Größe des gehandelten Instruments an.

## Hinweise und Tipps

- Funktioniert am besten auf trendenden Instrumenten wie wichtigen FX-Paaren oder Indizes auf Intraday- oder Swing-Zeitrahmen. Erhöhen Sie die EMA-Längen für Tagesbars oder verkürzen Sie sie für Scalping.
- Die Zwei-Kerzen-Bestätigung reduziert Whipsaws drastisch, kann aber zu späten Einstiegen führen. Erwägen Sie, den ADX-Schwellenwert und die EMA-Längen für Ihren Markt zu optimieren.
- Trailing-Stops bleiben inaktiv, bis der Preis `TrailingStopDistance + TrailingStepDistance` vom Einstieg aus vorrückt. Das Setzen des Schritts auf null imitiert das MetaTrader-Verhalten, bei dem das Trailing beginnt, sobald der Preis die Basisdistanz zurücklegt.
- Die Strategie stützt sich auf Market-Orders (`BuyMarket`, `SellMarket`). Konfigurieren Sie die `Volume`-Eigenschaft der Strategieinstanz, um das Positions-Sizing zu kontrollieren oder bei Bedarf mit dem Portfolio-Moneymanagement zu integrieren.
- Kombinieren Sie mit Sitzungsfiltern oder übergeordneter Zeitrahmen-Bestätigung, wenn Sie die Handelsstunden begrenzen müssen.
