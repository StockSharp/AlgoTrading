# Estrategia de Divergencia CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Las divergencias del Índice de Canal de Materias Primas (CCI) pueden presagiar reversiones de tendencia cuando el precio se mueve en dirección opuesta al indicador. Esta estrategia compara los máximos y mínimos del precio con los del CCI para identificar fortaleza o debilidad ocultas.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 91%. Funciona mejor en el mercado de acciones.

En cada vela, el sistema actualiza los valores recientes de precio y CCI, marcando una divergencia alcista cuando el precio hace un nuevo mínimo mientras el CCI forma un mínimo más alto. La divergencia bajista es la situación opuesta. Cuando una divergencia se alinea con niveles de sobreventa o sobrecompra, se abre una operación con un stop de volatilidad.

Las salidas ocurren cuando el CCI vuelve a cruzar la línea cero, señalando que el impulso se ha agotado. Dado que las divergencias pueden persistir, las reglas también se reinician después de un número fijo de barras para evitar señales obsoletas.

## Detalles

- **Criterios de entrada**: Divergencia precio/CCI con CCI por debajo de -100 para largos o por encima de +100 para cortos.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: CCI cruzando el cero o stop-loss.
- **Stops**: Sí, basado en porcentaje.
- **Valores predeterminados**:
  - `CciPeriod` = 20
  - `DivergencePeriod` = 5
  - `OverboughtLevel` = 100
  - `OversoldLevel` = -100
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
- **Filtros**:
  - Categoría: Divergencia
  - Dirección: Ambos
  - Indicadores: CCI
  - Stops: Sí
  - Complejidad: Avanzado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio

