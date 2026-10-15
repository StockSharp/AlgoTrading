# Estrategia Super Take
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia alterna entre posiciones largas y cortas e incrementa el take profit después de cada operación perdedora utilizando un multiplicador martingala. El stop loss es fijo mientras que el take profit se restablece al valor base tras una operación ganadora. Al cambiar siempre de dirección y ajustar los objetivos después de las pérdidas, la estrategia intenta recuperar los drawdowns anteriores.

Una nueva posición se abre únicamente cuando no hay ninguna posición activa. La primera operación es larga por defecto. Cada operación posterior se abre en la dirección opuesta a la última posición cerrada.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Sin posición activa y la última posición cerrada fue corta o no existe.
  - **Corto**: Sin posición activa y la última posición cerrada fue larga.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Cerrar la posición cuando el precio alcanza el take profit dinámico o el stop loss fijo.
- **Stops**: Stop loss fijo, take profit dinámico con martingala tras operaciones perdedoras.
- **Valores predeterminados**:
  - `TakeProfit` = 10
  - `StopLoss` = 15
  - `MartinFactor` = 1.8
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Simple
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
