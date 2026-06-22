# Estrategia DoubleUp2 CCI MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

DoubleUp2 es una estrategia estilo martingala que combina el Commodity Channel Index (CCI) y el MACD.
Abre posiciones cortas cuando ambos indicadores muestran valores positivos extremos y posiciones largas cuando ambos son extremadamente negativos.
Después de una operación perdedora, el tamaño de la posición se duplica buscando recuperar las pérdidas anteriores.
Las operaciones rentables se cierran una vez que el precio avanza un número fijo de puntos.

## Detalles

- **Criterios de Entrada**:
  - **Largo**: `CCI < -Threshold` y `MACD < -Threshold`.
  - **Corto**: `CCI > Threshold` y `MACD > Threshold`.
- **Largo/Corto**: Ambos.
- **Criterios de Salida**:
  - Señal opuesta o el precio se mueve `ExitDistance` puntos en ganancia.
- **Stops**: Sin stop-loss explícito.
- **Valores predeterminados**:
  - `CCI Period` = 8
  - `MACD Fast` = 13
  - `MACD Slow` = 33
  - `MACD Signal` = 2
  - `Threshold` = 230
  - `Base Volume` = 0.1
  - `ExitDistance` = `120 * price step`
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: CCI, MACD
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
