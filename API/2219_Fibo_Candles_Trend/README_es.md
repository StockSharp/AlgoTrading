# Estrategia de Tendencia Fibo Candles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza la técnica personalizada **Fibo Candles** para determinar la dirección de la tendencia.
El indicador pinta cada vela en uno de dos colores basándose en una comparación de ratio de Fibonacci
entre el cierre actual y el rango máximo/mínimo reciente. Un cambio de color señala una posible
reversión. Cuando el color se vuelve alcista, la estrategia cierra cualquier posición corta y abre una larga.
Cuando el color se vuelve bajista, cierra cualquier posición larga y abre una corta.

El método se adapta a la volatilidad del mercado mediante un período de lookback y un nivel de Fibonacci seleccionable.
Un stop loss y take profit en puntos absolutos protegen cada operación.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El color de la vela actual cambia de bajista a alcista.
  - **Corto**: El color de la vela actual cambia de alcista a bajista.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Las posiciones existentes se cierran cuando aparece el color opuesto.
- **Stops**: Stop loss y take profit fijos en puntos mediante `StartProtection`.
- **Valores predeterminados**:
  - `Period` = 10 (velas utilizadas para medir el rango máximo/mínimo).
  - `Fibo Level` = 0.236 (ratio utilizado para la decisión de tendencia).
  - `Stop Loss` = 1000 puntos.
  - `Take Profit` = 2000 puntos.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Highest, Lowest
  - Stops: Sí
  - Complejidad: Medio
  - Marco temporal: Horario por defecto
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Moderado
