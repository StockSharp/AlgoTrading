# Moving-Average-Regenbogen-Strategie (Stormer)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie zeichnet einen Regenbogen aus zwölf gleitenden Durchschnitten. Trades werden eröffnet, wenn der Trend bestätigt ist und der Preis einen der Durchschnitte berührt.

Eine Long-Position öffnet sich, wenn der Preis ein neues Hoch erreicht, alle mittleren Durchschnitte nach oben zeigen und die Kerze oberhalb des Mittelwerts aller Durchschnitte schließt. Eine Short-Position öffnet sich bei den entgegengesetzten Bedingungen.

Der Stop-Loss wird auf den zuletzt berührten gleitenden Durchschnitt gesetzt. Der Take-Profit wird als Vielfaches der Distanz zwischen Einstiegspreis und Stop-Loss berechnet.

## Details

- **Indikatoren**: 12 gleitende Durchschnitte konfigurierbaren Typs.
- **Long**: Aufwärtstrend, neues Hoch und vorheriger Berührungspreis.
- **Short**: Abwärtstrend, neues Tief und vorheriger Berührungspreis.
- **Ausstieg**: Stop-Loss am berührten Durchschnitt, Ziel = Einstieg ± Distanz * Faktor. Optionaler Umkehrausstieg bei Trendumkehrsignalen.
- **Parameter**: Typ des gleitenden Durchschnitts, Längen, Zielfaktor, Umkehroptionen.
- **Zeitrahmen**: Beliebig.
