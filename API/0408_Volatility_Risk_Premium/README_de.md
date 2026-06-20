# Volatilitätsrisikoprämien-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie verkauft Optionen, um die Volatilitätsrisikoprämie abzuschöpfen – in der Erwartung, dass die implizite Volatilität im Durchschnitt die realisierte übersteigt. Positionen werden delta-gehedgt, um die Prämie zu isolieren.

Das Short-Options-Engagement wird mit strengen Risikokontrollen und regelmäßigem Neuhedging gesteuert.

## Details

- **Daten**: Implizite Volatilität aus Optionen und realisierte Volatilität.
- **Einstieg**: Out-of-the-money-Optionen verkaufen, wenn implizit > realisiert.
- **Ausstieg**: Rückkauf bei Fälligkeit oder bei Volatilitätsspike.
- **Instrumente**: Index- oder FX-Optionen.
- **Risiko**: Delta-Hedging und Stop-Loss auf Vega.

