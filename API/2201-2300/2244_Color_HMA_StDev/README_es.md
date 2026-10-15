# Estrategia Color HMA StDev
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en la Media Móvil Hull con un filtro dinámico de desviación estándar.

El sistema observa cuánto se desvía el precio del HMA. Cuando el cierre supera el
promedio en un múltiplo elegido de la desviación estándar, la estrategia entra en largo, y viceversa para posiciones cortas.
Un multiplicador más amplio define una zona de salida para que las posiciones se cierren solo después de un retorno significativo dentro de la banda.

Este enfoque intenta capturar ráfagas rápidas de momentum evitando el ruido. La Media Móvil Hull reacciona rápidamente
a los cambios de tendencia, y la desviación estándar se adapta a la volatilidad permitiendo que los umbrales se expandan durante mercados turbulentos. La estrategia opera en ambas direcciones y no usa stops fijos, confiando en cambio en la
reversión a la media del precio hacia el HMA.

## Detalles

- **Criterios de entrada**: Cierre cruzando HMA ± K1 * StdDev.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Cierre cruzando HMA ± K2 * StdDev en dirección opuesta.
- **Stops**: Sin stop-loss ni take-profit fijos.
- **Valores predeterminados**:
  - `HmaPeriod` = 13
  - `StdPeriod` = 9
  - `K1` = 1.5m
  - `K2` = 2.5m
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoría: Tendencia, Volatilidad
  - Dirección: Ambos
  - Indicadores: HMA, Desviación estándar
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: 4h
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
