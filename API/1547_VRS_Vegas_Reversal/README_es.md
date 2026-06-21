# Estrategia de Reversión VRS Vegas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de reversión que utiliza las mechas de las velas.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 37%. Funciona mejor en el mercado de criptomonedas.

El sistema busca grandes picos relativos al precio de cierre. Una mecha inferior grande activa una entrada larga mientras que una mecha superior grande activa una entrada corta. Las posiciones se cierran cuando el precio se mueve el doble del tamaño del pico en beneficio.

## Detalles

- **Criterios de entrada**:
  - **Largo**: mecha inferior ≥ Spike% * close y sin pico superior.
  - **Corto**: mecha superior ≥ Spike% * close y sin pico inferior.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: Objetivo en entrada ± (pico * 2).
- **Stops**: No.
- **Valores predeterminados**:
  - `SpikePercent` = 0.025
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Price action
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
