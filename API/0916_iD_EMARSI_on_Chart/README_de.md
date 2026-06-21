# iD EMARSI im Chart-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Kreuzungen zwischen dem auf dem Preischart dargestellten RSI und seinem exponentiellen gleitenden Durchschnitt. Eine Long-Position wird eröffnet, wenn der RSI die EMA nach oben kreuzt, und eine Short-Position beim gegenteiligen Kreuzungssignal. Signale können nach einer Mindesttrenddauer gefiltert werden, und Positionen werden durch ein optionales Take-Profit- und Trailing-Stop-Level in Prozent geschützt.

## Parameter
- Kerzentyp
- RSI-Länge
- EMA-Länge
- Minimale Trendbars
- Gefilterte Signale verwenden
- Take Profit %
- Trailing Stop %
