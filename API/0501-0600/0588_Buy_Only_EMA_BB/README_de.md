# Nur-Kauf-Strategie mit EMA und BB
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie öffnet eine Long-Position, wenn der Preis über der EMA schließt.
Der anfängliche Stop-Loss wird am unteren Bollinger Band platziert und verschiebt sich zur EMA, wenn der Preis über dem oberen Band schließt.
Der Take-Profit wird anhand eines Reward-to-Risk-Verhältnisses basierend auf dem Abstand zum Band festgelegt.
Nachdem der Take-Profit erreicht wurde, wartet die Strategie darauf, dass der Preis unter die EMA kreuzt, bevor ein neuer Einstieg erlaubt wird.

## Details
- **Einstiegskriterien:** Close über EMA ohne aktive Blockierung und ohne offene Position.
- **Long/Short:** Nur Long.
- **Ausstiegskriterien:** Preis kreuzt unter das Stop-Level oder erreicht den Take-Profit.
- **Stops:** Anfangsstopp am unteren Band, Verschiebung zur EMA nach einer starken Bewegung.
- **Standardwerte:** EMA-Länge = 40, Bandabweichung = 0.7, Reward-to-Risk-Verhältnis = 3.
