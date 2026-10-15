# Estrategia MultiCapa de Aceleración/Deceleración
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia acumula hasta cinco entradas largas usando el oscilador Acceleration/Deceleration. Se coloca una orden de compra stop por encima del máximo de la barra cada vez que el impulso se desarrolla en la dirección de la tendencia identificada por los fractales y los dientes del Alligator. Cuando el oscilador se debilita o la tendencia se revierte, se cancelan todas las órdenes pendientes y se cierra la posición.

## Detalles

- **Criterios de entrada**:
  - Tendencia alcista confirmada cuando el precio rompe un fractal alcista por encima de los dientes del Alligator.
  - El oscilador AC imprime un patrón de barra verde y el cierre está por encima del filtro EMA.
  - Se colocan hasta cinco órdenes stop en el nivel de activación.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - La tendencia gira a bajista.
  - El oscilador se vuelve negativo.
- **Stops**: Usa stop loss basado en fractales.
- **Valores predeterminados**:
  - `EMA Length` = 100.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Solo largos
  - Indicadores: Múltiples
  - Stops: Sí
  - Complejidad: Complejo
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
