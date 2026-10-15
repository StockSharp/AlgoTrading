# Stufic Stochastic-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert die Trenderkennung durch zwei gleitende Durchschnitte mit Momentum-Signalen des Stochastik-Oszillators.
Sie kauft, wenn der schnelle gleitende Durchschnitt über dem langsamen liegt und die %K-Linie des Stochastik die %D-Linie von unten unterhalb eines Überverkauft-Schwellenwerts kreuzt.
Sie verkauft, wenn der schnelle gleitende Durchschnitt unter dem langsamen liegt und %K die %D-Linie von oben oberhalb eines Überkauft-Schwellenwerts kreuzt.

## Logik
- Erkennt den Markttrend durch Vergleich eines schnellen und eines langsamen einfachen gleitenden Durchschnitts.
- Verwendet den Stochastik-Oszillator, um Momentum-Umkehrungen bei Extremniveaus zu finden.
- Eröffnet eine Long-Position, wenn der Trend aufwärts gerichtet ist und der Oszillator die überverkaufte Zone mit einem bullischen Kreuz verlässt.
- Eröffnet eine Short-Position, wenn der Trend abwärts gerichtet ist und der Oszillator die überkaufte Zone mit einem bärischen Kreuz verlässt.
- Positionen werden bei entgegengesetzten Signalen geschlossen oder umgekehrt. Ein Stop-Loss-Prozentsatz wird über den eingebauten Schutz angewendet.

## Parameter
- **FastMaPeriod** – Zeitraum des schnellen gleitenden Durchschnitts.
- **SlowMaPeriod** – Zeitraum des langsamen gleitenden Durchschnitts.
- **StochKPeriod** – Zeitraum für die %K-Linie des Stochastik.
- **StochDPeriod** – Glättungszeitraum für die %D-Linie.
- **OverboughtLevel** – oberer Schwellenwert für den Stochastik-Oszillator.
- **OversoldLevel** – unterer Schwellenwert für den Stochastik-Oszillator.
- **StopLossPercent** – Stop-Loss-Abstand ausgedrückt als Prozentsatz des Einstiegspreises.
- **CandleType** – Kerzenserie, die für Berechnungen verwendet wird.

## Indikatoren
- Einfacher gleitender Durchschnitt (schnell und langsam).
- Stochastik-Oszillator.

## Verwendung
Hängen Sie die Strategie an ein Wertpapier. Konfigurieren Sie die Parameter für den gewünschten Zeitrahmen und das Risikolevel. Starten Sie die Strategie, um mit dem Handel zu beginnen. Der Algorithmus verwaltet Positionen automatisch auf Basis der beschriebenen Bedingungen.
