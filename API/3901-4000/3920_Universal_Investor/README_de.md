# Universelle Anlegerstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine direkte Portierung des Expertenberaters **Universal Investor** MetaTrader 4. Es kombiniert einen exponentiellen gleitenden Durchschnitt (EMA) und einen linear gewichteten gleitenden Durchschnitt (LWMA), um die kurzfristige Trendrichtung zu bestätigen, und führt den Handel mit einer Position mit adaptiver Positionsgröße durch.

## Handelslogik

1. Abonnieren Sie den konfigurierten `CandleType` und berechnen Sie sowohl EMA als auch den LWMA mit dem durch `MovingPeriod` definierten Zeitraum.
2. Speichern Sie die beiden neuesten Werte jedes gleitenden Durchschnitts, damit die Logik die `iMA(..., shift = 1/2)`-Aufrufe des ursprünglichen EA nachahmt.
3. Erzeugen Sie ein **Kaufsignal**, wenn der vorherige LWMA über dem vorherigen EMA liegt, beide Durchschnittswerte gestiegen sind und es kein entgegengesetztes Signal für dieselbe Kerze gibt.
4. Erzeugen Sie ein **Verkaufssignal**, wenn der vorherige LWMA unter dem vorherigen EMA liegt, beide Durchschnittswerte gefallen sind und es kein entgegengesetztes Signal für dieselbe Kerze gibt.
5. Schließen Sie eine offene Long-Position, sobald der LWMA unter EMA fällt (Spiegellogik für Shorts).
6. Berechnen Sie das Handelsvolumen anhand des Strategieparameters `Volume`, erhöhen Sie es, um die `MaximumRisk`-Anforderung zu erfüllen, wenn der Portfoliowert groß genug ist, und reduzieren Sie es nach aufeinanderfolgenden Verlustgeschäften gemäß `DecreaseFactor`.
7. Senden Sie Marktaufträge mit `BuyMarket`/`SellMarket` und behalten Sie den Einstiegspreis im Auge, um gewinnende oder verlierende Ausstiege zu erkennen.

Die Strategie hält jeweils nur eine Position offen und kehrt sich erst nach einem vollständigen Schließen sofort um, wodurch das Verhalten des ursprünglichen MetaTrader-Skripts reproduziert wird.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `CandleType` | Für Berechnungen verwendete Kerzenreihe. |
| `MovingPeriod` | Zeitraum sowohl für EMA als auch für LWMA. |
| `MaximumRisk` | Anteil des Eigenkapitals (0,05 = 5 %), der zur Berechnung des Mindestpositionsvolumens verwendet wird. |
| `DecreaseFactor` | Reduziert das Volumen nach aufeinanderfolgenden Verlustgeschäften (0 deaktiviert die Funktion). |
| `Volume` | Basisvertragsvolumen an `BuyMarket`/`SellMarket` übergeben. |

## Indikatoren

- `ExponentialMovingAverage`
- `LinearWeightedMovingAverage`

## Notizen

- Aufträge werden nur für geschlossene Kerzen erteilt, die mit EA übereinstimmen, das auf `Time[0]`-Prüfungen basiert.
- Die Logik der Positionsgröße spiegelt die Funktion MetaTrader `LotsOptimized` wider, einschließlich der risikobasierten Komponente und des Verluststreak-Multiplikators.
