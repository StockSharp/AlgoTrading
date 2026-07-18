# Screener Mean-Reversion-Kanal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie handelt einen Mean-Reversion-Kanal, der aus einem gleitenden Durchschnitt und ATR aufgebaut ist. Sie verkauft, wenn der Kurs über dem oberen Band schließt, und kauft, wenn der Kurs unter dem unteren Band schließt. Positionen werden geschlossen, wenn der Kurs zur Mittellinie zurückkehrt.

## Details
- Einstieg: Schluss über dem oberen Band -> Short, Schluss unter dem unteren Band -> Long.
- Ausstieg: Kurs kreuzt zurück zur Mittellinie.
- Indikatoren: SMA und ATR.
- Stops: keine.
