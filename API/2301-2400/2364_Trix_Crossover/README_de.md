# TRIX-Kreuzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet zwei TRIX-Indikatoren (Triple Exponential Moving Average Oscillator) mit unterschiedlichen Perioden, um potenzielle Umkehrungen zu erkennen. Eine Long-Position wird eröffnet, wenn der schnelle TRIX einen lokalen Tiefpunkt bildet, während der langsame TRIX steigt. Eine Short-Position wird eröffnet, wenn der schnelle TRIX einen lokalen Hochpunkt bildet, während der langsame TRIX fällt.

## Parameter

- **Fast TRIX Period** – Periode des schnellen TRIX-Indikators.
- **Slow TRIX Period** – Periode des langsamen TRIX-Indikators.
- **Take Profit** – Gewinnziel in absoluten Preiseinheiten.
- **Stop Loss** – Maximalverlust in absoluten Preiseinheiten.
- **Candle Type** – Zeitrahmen oder Datentyp für Kerzen.

## Handelslogik

1. Abonnieren des ausgewählten Kerzentyps.
2. Berechnen der schnellen und langsamen TRIX-Werte bei jeder abgeschlossenen Kerze.
3. Long einsteigen, wenn der aktuelle TRIX-Wert höher als der vorherige ist, der vorherige niedriger als der davor liegende ist und der langsame TRIX steigt.
4. Short einsteigen, wenn der aktuelle TRIX-Wert niedriger als der vorherige ist, der vorherige höher als der davor liegende ist und der langsame TRIX fällt.
5. Es wird nur eine Position gleichzeitig gehalten.
6. Stop Loss und Take Profit Schutzmaßnahmen werden automatisch angewendet.

## Hinweise

Die Strategie ist eine Anpassung eines MQL5-Skripts und zeigt, wie man mit TRIX-Indikatoren in StockSharp arbeitet.
