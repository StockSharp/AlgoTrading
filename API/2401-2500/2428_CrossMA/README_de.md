# CrossMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt einen einfachen gleitenden Durchschnitt-Crossover mit einem ATR-basierten Stop Loss. Eine Long-Position wird eröffnet, wenn der schnelle SMA den langsamen SMA von unten nach oben kreuzt. Eine Short-Position wird eröffnet, wenn der schnelle SMA den langsamen SMA von oben nach unten kreuzt. Nach dem Einstieg in eine Position wird ein Stop Loss einen ATR-Abstand vom Einstiegspreis platziert und auf jeder neuen Kerze überprüft.

## Parameter
- Kerzentyp
- Periode des schnellen SMA
- Periode des langsamen SMA
- ATR-Periode
- Volumen
