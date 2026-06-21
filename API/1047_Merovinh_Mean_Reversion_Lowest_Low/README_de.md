# Merovinh - Mean Reversion Niedrigstes Tief
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kauft, wenn das aktuelle Tiefsttief eines Rückblickzeitraums aufeinanderfolgende vorherige Tiefs eine konfigurierbare Anzahl von Malen unterschreitet. Die Position wird geschlossen, sobald innerhalb desselben Zeitraums ein neues Hochpunkt erscheint.

## Parameter
- Bars — Rückblicklänge für Hochs/Tiefs.
- Number Of Lows — erforderliche Anzahl aufeinanderfolgender gebrochener Tiefs für den Einstieg.
- Start Date / End Date — Handelszeitraum.
- Candle Type — Kerzentyp.
