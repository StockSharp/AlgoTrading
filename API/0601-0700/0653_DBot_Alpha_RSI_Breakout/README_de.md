# Alpha RSI Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet SMA und RSI, um RSI-Kreuzungen über einem Schwellenwert zu erfassen, wenn der Preis über der SMA liegt. Der Trailing Stop aktiviert sich, nachdem der RSI ein Take-Profit-Niveau erreicht. Ausstieg bei RSI-Stop-Loss, Erreichen des Take-Profits oder Trailing Stop.

## Details

- **Daten**: Kurskerzen.
- **Einstieg**: Kaufen, wenn RSI das Einstiegsniveau von unten kreuzt und der Preis über der SMA liegt.
- **Ausstieg**: RSI unterhalb des Stop-Niveaus, RSI erreicht Take-Profit oder Preis fällt nach Aktivierung unter den Trailing Stop.
- **Instrumente**: beliebige.
- **Risiko**: RSI-basierter Stop-Loss und Trailing Stop nach Gewinn.
