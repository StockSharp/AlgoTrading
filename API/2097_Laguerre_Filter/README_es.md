# Filtro Laguerre
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia opera los cruces entre un filtro Laguerre y un filtro FIR corto construido como una media móvil ponderada de precios medianos recientes.

- El filtro Laguerre suaviza el precio usando el parámetro Gamma para reducir el ruido.
- La línea FIR es una media móvil ponderada de 4 períodos con pesos simétricos.
- Cuando la línea FIR estaba por encima de la línea Laguerre y cruza por debajo de ella, la estrategia abre una posición larga.
- Cuando la línea FIR estaba por debajo y cruza por encima de la línea Laguerre, se abre una posición corta.
- Las posiciones opuestas se cierran cuando la relación entre las líneas se invierte.
- Un stop-loss en porcentaje del precio de entrada protege cada operación.

Este enfoque de reversión a la media intenta capturar retrocesos cuando el precio se desvía de la curva Laguerre suavizada.
