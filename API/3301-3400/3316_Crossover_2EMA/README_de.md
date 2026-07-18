# Crossover-2-EMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie repliziert den MetaTrader-Expert-Advisor "Crossover_2EMA", indem sie die Beziehung zwischen einer schnellen und einer langsamen exponentiellen gleitenden Durchschnittslinie (EMA) auf Schlusskursen handelt. Wenn die schnelle EMA über die langsame steigt, geht der Algorithmus long. Fällt sie wieder darunter, kehrt der Algorithmus in eine Short-Position um. Der Ansatz hält die Position stets am aktuellen Fast/Slow-Trendzustand ausgerichtet und funktioniert daher als vollständig reversibles System.

## Handelslogik
1. Die konfigurierte Kerzenserie abonnieren und zwei EMAs mit benutzerdefinierten Perioden berechnen.
2. Den Spread zwischen schneller und langsamer EMA auf jeder abgeschlossenen Kerze verfolgen.
3. Ein Aufwärtskreuzen erkennen, wenn der Spread von nicht positiv zu positiv wechselt. Jede Short-Exposure schließen und eine Long-Position mit dem konfigurierten Handelsvolumen eröffnen.
4. Ein Abwärtskreuzen erkennen, wenn der Spread von nicht negativ zu negativ wechselt. Jede Long-Exposure schließen und eine Short-Position mit dem konfigurierten Handelsvolumen eröffnen.
5. Orders werden als Marktausführungen gesendet, um sofort auf das Crossover zu reagieren. Das Volumen wird bei Umkehr automatisch erhöht, damit die bestehende Position zuerst glattgestellt und danach eine neue eröffnet wird.

## Risikomanagement
- Die Strategie ruft beim Start `StartProtection()` auf, sodass StockSharps Standardschutzmechanismen konfiguriert werden können (z. B. Drawdown-Schutz, Handelszeitlimits oder Circuit Breaker).
- Positionsumkehrungen verwenden eine einzelne kombinierte Marktorder und reduzieren dadurch die Latenz gegenüber sequenziellem Ausstieg und Wiedereinstieg.

## Parameter
- **Candle Type:** Datenserie für EMA-Berechnungen.
- **Fast EMA Period:** Periode der schnellen EMA. Muss kleiner als die langsame EMA-Periode sein.
- **Slow EMA Period:** Periode der langsamen EMA. Muss größer als die schnelle EMA-Periode sein.

## Zusätzliche Hinweise
- Beide EMAs müssen vollständig gebildet sein, bevor der Handel beginnt, um vorzeitige Signale zu verhindern.
- Die Standardkonfiguration verwendet 12/24-Perioden-EMAs auf Ein-Minuten-Kerzen und spiegelt den ursprünglichen MQL-Expert-Advisor wider.
- Die Parameter sind als optimierbar markiert und erlauben Batch-Optimierung in StockSharp.
