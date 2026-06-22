# Estrategia Bezier de Desviación Estándar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia negocia puntos de giro en la volatilidad usando un indicador de desviación estándar. Interpreta los mínimos y máximos locales del indicador como posibles reversiones en la acción del precio. Cuando la desviación estándar forma un valle, el sistema espera que la volatilidad se expanda al alza y entra en una posición larga. Cuando aparece un pico, vende en corto anticipando una contracción de la volatilidad.

El enfoque está diseñado para operaciones tanto largas como cortas en un marco temporal de cuatro horas por defecto. No aplica órdenes de stop-loss, centrándose en cambio en salidas basadas en señales.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El valor de la desviación estándar en la barra anterior es menor que sus vecinos (mínimo local).
  - **Corto**: El valor de la desviación estándar en la barra anterior es mayor que sus vecinos (máximo local).
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Una señal opuesta desencadena una reversión.
- **Stops**: No.
- **Valores predeterminados**:
  - `StdDev Period` = 9.
  - `Candle Type` = velas de 4 horas.
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Desviación estándar
  - Stops: No
  - Complejidad: Simple
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
