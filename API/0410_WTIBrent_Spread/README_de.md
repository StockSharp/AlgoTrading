# WTI-Brent-Spread-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Trade zielt auf die Preisdifferenz zwischen WTI- und Brent-Rohöl. Weicht der Spread von historischen Normalwerten ab, setzt das System auf Mean Reversion, indem es die günstigere Sorte kauft und die teurere leerverkauft.

Positionen werden mit den Frontmonat-Futures gerollt und bei Spread-Konvergenz geschlossen.

## Details

- **Daten**: Frontmonat-Futures-Preise von WTI und Brent.
- **Einstieg**: Long in der günstigeren Sorte, Short in der teureren, wenn Spread > Schwellenwert.
- **Ausstieg**: Schließen, wenn der Spread zum Durchschnitt zurückkehrt oder beim Kontraktroll.
- **Instrumente**: Rohöl-Futures.
- **Risiko**: Dollar-neutral mit Stop bei Spread-Ausweitung.

