# Estrategia de Cierre por Cruce de MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia monitorea una media móvil simple (MA) y cierra automáticamente cualquier posición abierta cuando el cierre de la vela cruza la línea de la MA. Está diseñada para traders que gestionan las entradas manualmente o con otros sistemas, pero quieren una salida automatizada cuando el tendencia se invierte.

La lógica rastrea la relación entre el precio de cierre y la MA. Cuando una nueva vela finalizada cruza de un lado de la MA al otro, la estrategia envía una orden de mercado para cerrar la posición. No se abren nuevas posiciones.

## Detalles

- **Criterios de entrada**: Ninguno. Las posiciones deben abrirse externamente.
- **Criterios de salida**:
  - **Largo**: Cierre anterior por encima de la MA y cierre actual por debajo de la MA activa una venta para cerrar.
  - **Corto**: Cierre anterior por debajo de la MA y cierre actual por encima de la MA activa una compra para cerrar.
- **Largo/Corto**: Se soportan ambas direcciones.
- **Stops**: No se usan. El cruce de la MA actúa como señal de salida.
- **Valores predeterminados**:
  - `MA Period` = 50.
  - `Candle Type` = Marco temporal de 1 minuto.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Único
  - Stops: No
  - Complejidad: Simple
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Moderado

