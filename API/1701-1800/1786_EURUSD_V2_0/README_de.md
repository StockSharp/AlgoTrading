# EURUSD V2.0-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Mean-Reversion-System für EURUSD unter Verwendung eines langfristigen einfachen gleitenden Durchschnitts (SMA) und eines Volatilitätsfilters auf Basis des Average True Range (ATR).

## Strategielogik

- Berechnung eines SMA der Länge *MA Length* auf dem gewählten Kerzentyp.
- **Short**-Einstieg, wenn der Preis über dem SMA liegt und innerhalb von *Buffer* Pips zurückkommt, während der ATR unter *ATR Threshold* liegt.
- **Long**-Einstieg, wenn der Preis unter dem SMA liegt und sich innerhalb von *Buffer* Pips nähert bei niedrigem ATR.
- Die Positionsgröße ergibt sich aus dem Kontostand und dem *Risk Factor Z*.
- Stop-Loss und Take-Profit werden in festen Pip-Abständen vom Einstiegspreis gesetzt.
- Nach dem Ausstieg wartet das System, bis der Preis *Noise Filter* Pips vom Einstiegsniveau entfernt ist, bevor ein neuer Trade erlaubt wird.

## Parameter

- **MA Length** – Periode des einfachen gleitenden Durchschnitts (Standard 218).
- **Buffer (pips)** – maximaler Abstand vom SMA für den Einstieg (Standard 0).
- **Stop Loss (pips)** – Stop-Loss-Abstand vom Einstieg (Standard 20).
- **Take Profit (pips)** – Take-Profit-Abstand vom Einstieg (Standard 350).
- **Noise Filter (pips)** – Abstand zur Rückstellung der Handelserlaubnis (Standard 50).
- **ATR Length** – ATR-Berechnungsperiode (Standard 200).
- **ATR Threshold (pips)** – maximaler ATR für neue Positionen (Standard 40).
- **Max Spread (pips)** – maximal erlaubter Spread (Standard 4).
- **Risk Factor Z** – Money-Management-Faktor (Standard 2).
- **Candle Type** – Zeitrahmen der verarbeiteten Kerzen (Standard 15 Minuten).

Diese Strategie verwendet Marktorders für Ein- und Ausstiege.
