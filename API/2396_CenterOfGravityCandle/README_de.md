# Center-of-Gravity-Candle-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den MetaTrader-Experten "Exp_CenterOfGravityCandle" unter Verwendung der StockSharp High-Level-API. Der Experte handelt synthetische Kerzen, die vom Center of Gravity Candle Indikator generiert werden. Jede synthetische Kerze wird durch Anwendung von John Ehlers' Center-of-Gravity-Berechnung auf die Eröffnungs-, Hoch-, Tief- und Schlusskurse und anschließende Glättung der Ergebnisse mit einem konfigurierbaren gleitenden Durchschnitt erstellt. Die Farbe der synthetischen Kerze (bullisch, bärisch oder neutral) ist das einzige Handelssignal.

## Indikatorlogik

1. Jede eingehende Marktkerze wird nach vollständigem Schließen verarbeitet.
2. Für jede Preiskomponente (Eröffnung, Hoch, Tief, Schluss) berechnet die Strategie zwei gleitende Durchschnitte: einen einfachen MA und einen linear gewichteten MA mit dem durch **Period** definierten Zeitraum.
3. Das Produkt dieser beiden Durchschnitte wird durch den Preisschritt des Instruments dividiert und mit der konfigurierten Methode (**Ma Method**) und Länge (**Smooth Period**) geglättet.
4. Das synthetische Hoch und Tief werden so erzwungen, dass sie die synthetische Eröffnung/Schluss einschließen, damit die Kerzenkörper konsistent mit der MetaTrader-Implementierung bleiben.
5. Die Kerzenfarbe wird durch Vergleich der synthetischen Eröffnung und des Schlusses bestimmt: Eröffnung unter Schluss = bullisch (Farbe 2), Eröffnung über Schluss = bärisch (Farbe 0), sonst neutral (Farbe 1).

## Handelsregeln

1. Die Strategie führt einen rollierenden Verlauf der synthetischen Kerzenfarben und untersucht die durch **Signal Bar** definierte Kerze (Standard = vorherige abgeschlossene Kerze).
2. Wenn die untersuchte synthetische Kerze bullisch wird und die vorherige Kerze nicht bullisch war:
   - Eine bestehende Short-Position schließen, wenn **Enable Sell Close** `true` ist.
   - Eine neue Long-Position öffnen, wenn **Enable Buy Open** `true` ist.
3. Wenn die untersuchte synthetische Kerze bärisch wird und die vorherige Kerze nicht bärisch war:
   - Eine bestehende Long-Position schließen, wenn **Enable Buy Close** `true` ist.
   - Eine neue Short-Position öffnen, wenn **Enable Sell Open** `true` ist.
4. Markteinstiege verwenden das aus **Money Management** und **Margin Mode** berechnete Volumen. Negative Werte für **Money Management** werden als feste Losgröße behandelt. Bei verlustbasierten Modi approximiert der Algorithmus das Risiko pro Trade anhand der konfigurierten Stop-Loss-Distanz.
5. `StartProtection` wird aktiviert, um automatisch Take-Profit- und Stop-Loss-Orders gemäß den Distanzen **Take Profit** und **Stop Loss** in Preisschritten zu platzieren.

## Parameter

- **Money Management** – Anteil des Kontowerts zur Ableitung des Ordervolumens (negative Werte = festes Los). Optimierbar.
- **Margin Mode** – Interpretation des Money-Management-Parameters (eigenkapitalbasiert, saldobasiert, verlustbasiert oder festes Los).
- **Stop Loss** – Stop-Loss-Distanz in Preisschritten. Wird sowohl für Schutzorders als auch für risikobasiertes Positionssizing verwendet.
- **Take Profit** – Take-Profit-Distanz in Preisschritten. Angewendet über `StartProtection`.
- **Open Long / Open Short** – Eröffnung von Long/Short-Positionen bei entsprechenden Signalen erlauben.
- **Close Long / Close Short** – Schließen von Long/Short-Positionen beim gegenteiligen Signal erlauben.
- **Candle Type** – Zeitrahmen der Kerzen für die Indikatorberechnung.
- **Center of Gravity Period** – Basisperiode für den einfachen und linear gewichteten gleitenden Durchschnitt. Optimierbar.
- **Smoothing Period** – Länge des auf die synthetischen Kerzen angewendeten Glättungs-MA. Optimierbar.
- **Smoothing Method** – Gleitender-Durchschnitt-Typ in der Glättungsphase (SMA, EMA, SMMA oder LWMA).
- **Signal Bar** – Index der synthetischen Kerze zur Signalgenerierung (0 = aktuell, 1 = vorherig usw.).

## Hinweise

- Die Indikatorberechnung ist in C# implementiert, um die originale MetaTrader-Logik zu reproduzieren, ohne manuelle Puffer oder historische Sammlungen.
- Die Volumenberechnung verwendet StockSharp-Portfolioinformationen und kann aufgrund von Plattformunterschieden leicht von MetaTrader-Ergebnissen abweichen.
- Die Strategie arbeitet vollständig auf abgeschlossenen Kerzen und handelt niemals auf Teilbalken.
