# Exp 2XMA Ichimoku Oszillator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert die Logik des originalen MetaTrader-Expertenberaters "Exp_2XMA_Ichimoku_Oscillator", indem zwei Ichimoku-artige Preisenveloppen kombiniert werden, die mit konfigurierbaren gleitenden Durchschnitten geglättet werden. Die StockSharp-Implementierung verwendet die High-Level-Strategie-API und konzentriert sich auf kerzenbasierte Signalerzeugung, während die Positionsverwaltungsregeln des Quellalgorithmus beibehalten werden.

## Kernidee

1. Zwei Donchian-artige Mittelpunkte werden auf dem ausgewählten Zeitrahmen berechnet:
   - Der **schnelle Mittelpunkt** mittelt das höchste Hoch und tiefste Tief über `UpPeriod1` und `DownPeriod1` Balken.
   - Der **langsame Mittelpunkt** führt dieselbe Operation mit `UpPeriod2` und `DownPeriod2` Balken durch.
2. Jeder Mittelpunkt wird durch einen gleitenden Durchschnitt (`Method1`, `Method2`) mit den Längen `XLength1` und `XLength2` geglättet. Die verfügbaren Glättungsmethoden sind Einfach, Exponentiell, Geglättet und Linear Gewichtet.
3. Der Oszillatorwert ist die Differenz zwischen den beiden geglätteten Mittelpunkten. Vier Farbzustände beschreiben sein Verhalten:
   - `PositiveRising` (0): Oszillator liegt über null und steigt.
   - `PositiveFalling` (1): Oszillator liegt über null und verliert Dynamik.
   - `NegativeRising` (3): Oszillator liegt unter null, steigt aber in Richtung null.
   - `NegativeFalling` (4): Oszillator liegt unter null und fällt weiter.
   - `Neutral` (2) wird während der Aufwärmphase zugewiesen.
4. Signale werden anhand der Farben der Balken bei `SignalBar` und dem unmittelbar vorherigen Balken (`SignalBar + 1`) ausgewertet, was die Pufferverschiebung in der MQL-Version widerspiegelt.

## Handelslogik

- **Long-Einstieg**: erlaubt, wenn `EnableBuyOpen` wahr ist. Wenn die Farbe des älteren Balkens (`SignalBar + 1`) aufsteigend war (0 oder 3) und der neuere Balken (`SignalBar`) zu einer fallenden Farbe (1 oder 4) wechselte, schließt die Strategie jede Short-Position (`EnableSellClose`) und öffnet/erweitert eine Long-Position mit `Volume + |Position|` Einheiten.
- **Short-Einstieg**: erlaubt, wenn `EnableSellOpen` wahr ist. Wenn die Farbe des älteren Balkens fallend war (1 oder 4) und der neuere Balken zu einer aufsteigenden Farbe (0 oder 3) wechselte, schließt die Strategie bestehende Longs (`EnableBuyClose`) und öffnet/erweitert eine Short-Position mit `Volume + |Position|` Einheiten.
- Alle Ausführungen erfolgen beim Schließen der Kerze, die den Auslöser erzeugt. Aufträge sind immer Marktaufträge und die Strategie wendet keine zusätzlichen Stop-Loss- oder Take-Profit-Niveaus an; sie verlässt sich ausschließlich auf Farbübergänge für Ausstiege.
- `StartProtection()` wird beim Start aktiviert, um die integrierten Sicherheitsprüfungen des Frameworks für unerwartete Positionen zu nutzen.

## Parameter

| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `CandleType` | Zeitrahmen für Indikatorberechnungen. | 4-Stunden-Kerzen |
| `UpPeriod1`, `DownPeriod1` | Rückblickfenster für den schnellen Mittelpunkt. | 6, 6 |
| `UpPeriod2`, `DownPeriod2` | Rückblickfenster für den langsamen Mittelpunkt. | 9, 9 |
| `XLength1`, `XLength2` | Glättungslängen für die zwei gleitenden Durchschnitte. | 25, 80 |
| `Method1`, `Method2` | Typen gleitender Durchschnitte (Einfach, Exponentiell, Geglättet, Gewichtet). | Einfach |
| `SignalBar` | Historische Balkenverschiebung zum Lesen der Oszillatorfarben. | 1 |
| `EnableBuyOpen`, `EnableSellOpen` | Long/Short-Einstiege umschalten. | true |
| `EnableBuyClose`, `EnableSellClose` | Long/Short-Ausstiege umschalten. | true |
| `Volume` | Basis-Handelsgröße; bestehende Positionen werden bei Umkehr zu diesem Wert addiert. | 1 |

## Verwendungshinweise

- Die Typen gleitender Durchschnitte decken die häufigsten Glättungsverhalten des ursprünglichen Experten ab. Erweiterte Optionen wie benutzerdefinierte XMA-Phasenanpassungen sind in StockSharp nicht verfügbar und wurden durch Standardindikatoren ersetzt.
- Da der Oszillator auf geschlossenen Kerzen berechnet wird, erscheinen Signale mit der gleichen Einbalken-Verzögerung, die die MQL-Implementierung verwendete (`SignalBar = 1`). Erhöhen Sie `SignalBar`, wenn Sie zusätzliche Bestätigungsbalken benötigen.
- Erwägen Sie, die Strategie mit externem Risikomanagement (Portfolio-Manager, Schutz-Stops) zu kombinieren, wenn Sie auf Live-Märkten handeln, da Ausstiege ausschließlich von Oszillatorfarbumkehrungen abhängen.
