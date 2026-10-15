# MA Rounding Candle-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist eine Interpretation des originalen MQL5-Expertenberaters "MA Rounding Candle". Sie verwendet zwei geglättete gleitende Durchschnitte, die auf die Eröffnungs- und Schlusskurse der Kerzen angewendet werden. Die relative Position dieser Durchschnitte definiert die Farbe einer synthetischen Kerze: grün, wenn der geglättete Schlusskurs über der Eröffnung liegt, rot, wenn der Schluss unterhalb der Eröffnung liegt, und grau, wenn beide gleich sind. Ein Farbwechsel gegenüber dem vorherigen Balken erzeugt Handelssignale.

## Algorithmus

1. Für jede abgeschlossene Kerze werden die Eröffnungs- und Schlusswerte mit einem einfachen gleitenden Durchschnitt konfigurierbarer Länge geglättet.
2. Die Kerzenfarbe wird durch den Vergleich der geglätteten Werte definiert:
   - **Aufwärtskerze** – der geglättete Schlusskurs ist höher als die geglättete Eröffnung.
   - **Abwärtskerze** – der geglättete Schlusskurs ist niedriger als die geglättete Eröffnung.
   - **Neutral** – beide Werte sind gleich.
3. War die vorherige Kerze eine Aufwärtskerze und ist die aktuelle keine Aufwärtskerze, eröffnet die Strategie eine Long-Position und schließt alle Short-Positionen.
4. War die vorherige Kerze eine Abwärtskerze und ist die aktuelle keine Abwärtskerze, eröffnet die Strategie eine Short-Position und schließt alle Long-Positionen.

## Parameter

- **MaLength** – Periode der glättenden gleitenden Durchschnitte (Standard 12).
- **CandleType** – Zeitrahmen der verarbeiteten Kerzen.

## Hinweise

Die Strategie zeigt, wie Signale eines benutzerdefinierten Indikators ausschließlich mit integrierten StockSharp-Tools nachgebildet werden können. Es wird kein Stop-Loss oder Take-Profit verwendet; Positionen werden sofort umgekehrt, wenn das gegenteilige Signal erscheint.
