# Estrategia de Acciones de Baja Volatilidad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este factor defensivo de renta variable busca la "anomalía de baja volatilidad": la observación de que las acciones con movimientos de precio más tranquilos suelen ofrecer rendimientos ajustados al riesgo superiores. La volatilidad se calcula como la desviación estándar de los rendimientos diarios durante una ventana retrospectiva (60 días hábiles por defecto).

En el primer día hábil de cada mes, el universo se clasifica por volatilidad realizada. La estrategia va larga en el decil de menor volatilidad y corta en el decil de mayor volatilidad, asignando pesos iguales en dólares dentro de cada grupo. Las posiciones se mantienen hasta el siguiente rebalanceo mensual y no se utilizan stops explícitos.

Las pruebas retrospectivas muestran una curva de capital más suave y menores caídas que el mercado amplio, lo que hace que el enfoque sea atractivo para inversores que buscan exposición a renta variable con riesgo reducido.

## Detalles

- **Criterios de entrada**: Ordenación mensual por volatilidad retrospectiva; largo decil más bajo,
  corto decil más alto
- **Largo/Corto**: Ambos
- **Criterios de salida**: Próximo rebalanceo mensual
- **Stops**: No
- **Valores predeterminados**:
  - `VolWindowDays` = 60
  - `Deciles` = 10
  - `MinTradeUsd` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Volatilidad
  - Dirección: Ambos
  - Indicadores: Desviación estándar
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
