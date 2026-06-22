# JS MA Day-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **JS MA Day-Strategie** handelt auf Basis eines einfachen gleitenden Durchschnitts, der auf Tageskerzen mit dem Medianpreis berechnet wird. Die Strategie vergleicht die Position des gleitenden Durchschnitts relativ zum Eröffnungspreis jedes Tages und eröffnet Positionen, wenn der Trend des gleitenden Durchschnitts einen Crossover des Eröffnungspreises bestätigt.

## Indikatoren

- Einfacher gleitender Durchschnitt (Medianpreis)

## Parameter

| Name | Beschreibung | Standard |
|------|-------------|----------|
| `MaPeriod` | Periode des einfachen gleitenden Durchschnitts. | `3` |
| `Reverse` | Kehrt Handelssignale um. Wenn aktiviert, werden Kaufsignale zu Verkaufssignalen und umgekehrt. | `false` |
| `CandleType` | Für Berechnungen verwendeter Kerzentyp. Standard sind Tages-Zeitrahmen-Kerzen. | `TimeFrame(1 day)` |

## Einstiegsregeln

1. Bewertet den täglichen einfachen gleitenden Durchschnitt (SMA) und die täglichen Eröffnungspreise.
2. **Kaufen** wenn:
   - Der aktuelle SMA liegt unter dem vorherigen SMA.
   - Der aktuelle SMA liegt über dem heutigen Eröffnungspreis.
   - Der vorherige SMA liegt unter dem SMA vor zwei Tagen.
   - Der vorherige SMA liegt über dem Eröffnungspreis des Vortages.
3. **Verkaufen** wenn:
   - Der aktuelle SMA liegt über dem vorherigen SMA.
   - Der aktuelle SMA liegt unter dem heutigen Eröffnungspreis.
   - Der vorherige SMA liegt über dem SMA vor zwei Tagen.
   - Der vorherige SMA liegt unter dem Eröffnungspreis des Vortages.
4. Wenn `Reverse` aktiviert ist, werden Kauf- und Verkaufsbedingungen getauscht.

## Ausstiegsregeln

- Positionen werden durch den Aufruf von `StartProtection` geschlossen, was die Konfiguration von Schutzorders wie Stop Loss oder Take Profit über die Plattformeinstellungen ermöglicht.

## Hinweise

- Die Strategie verarbeitet nur abgeschlossene Kerzen.
- Das Ordervolumen wird durch die `Volume`-Eigenschaft der Basisklasse definiert.
- Es gibt noch keine Python-Version dieser Strategie.
