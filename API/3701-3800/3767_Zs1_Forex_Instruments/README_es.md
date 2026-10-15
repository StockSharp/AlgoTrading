# Estrategia de instrumentos Forex Zs1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce la lógica de red cubierta del experto MetaTrader **Zs1_www_forex-instruments_info**. El algoritmo abre un par de compra/venta simultáneo, monitorea qué tan lejos viaja el precio desde el punto de partida y reacciona a cinco zonas comerciales discretas. El tramo superviviente de la cobertura se promedia con multiplicadores de martingala, mientras que la cesta está protegida por una salida basada en acciones.

## Comportamiento central

- Abrir una cobertura de mercado inicial (una compra y una venta) con el volumen base configurado.
- Una vez que cualquiera de los tramos se vuelva rentable, ciérrelo y mantenga el lado perdedor como orden de anclaje.
- Realice un seguimiento del desplazamiento de precios utilizando el parámetro `Orders Space (pips)`. Cuando se alcanza una nueva zona, ejecuta la misma lógica de bifurcación que el experto original:
  - Zona −2: cerrar la cesta con beneficio, en caso contrario promediar contra el movimiento.
  - Zona −1: añade una posición opuesta al ancla inicial.
  - Zona 0: añade una posición en la dirección del ancla.
  - Zona +1: cerrar la cesta con beneficio, en caso contrario abrir el lado opuesto.
- Siempre que haya tres o más operaciones activas, salga inmediatamente si la ganancia flotante no es negativa.
- Una vez cerradas todas las posiciones, el ciclo se reinicia automáticamente.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `Orders Space (pips)` | Distancia en pips entre niveles de cuadrícula adyacentes. |
| `Zone Offset (pips)` | Amortiguador adicional que debe superarse antes de que se confirme una nueva zona. |
| `Initial Volume` | Volumen base utilizado para la cobertura de apertura y para el escalado de martingala. |

## Notas

- Los multiplicadores de martingala siguen la secuencia de túnel original (1, 3, 6, 12,...).
- La validación de volumen respeta las restricciones mínimas, máximas y de pasos de seguridad antes de enviar cualquier pedido.
- Todas las decisiones están impulsadas por las mejores actualizaciones de oferta/demanda de los datos de Nivel 1, que coinciden con la lógica basada en ticks de la versión MQL.
