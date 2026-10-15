# Estrategia Martingala de InwCoin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa un enfoque martingala simple para posiciones largas en Bitcoin.
Admite tres señales de entrada opcionales: el histograma MACD cruza por encima de cero,
el %D del Stochastic RSI cruza por encima del nivel 20, o el precio rompe un canal basado en ATR.
Después de cada compra, el tamaño de la posición puede duplicarse cuando el precio cae un porcentaje configurado.
La posición completa se cierra cuando el beneficio alcanza un porcentaje especificado por encima del precio de entrada promedio.

## Detalles

- **Señales de entrada**
  - **MACD Line > 0**: el histograma cruza por encima de cero.
  - **STO RSI cross up**: la línea %D cruza por encima de 20 mientras %K está en zona de sobreventa.
  - **ATR Channel**: el precio de cierre cruza por encima de EMA más el multiplicador ATR.
- **Take profit**: la posición sale cuando el precio supera el precio promedio en el porcentaje configurado.
- **Martingala**: compras adicionales ocurren cuando el precio cae el porcentaje configurado desde el precio promedio.
- **Dirección**: Solo largos.

