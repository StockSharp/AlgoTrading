# Williams Estrategia de índice direccional porcentual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Williams estrategia de índice direccional porcentual** recrea el MetaTrader 5 experto "Mt5 Williams % índice direccional EA" utilizando el nivel alto de StockSharp API. Combina el oscilador Williams %R con el índice direccional promedio (ADX) para identificar cambios de impulso y luego se basa en el índice de flujo de dinero (MFI) y el oscilador Stochastic para salir de las operaciones. La implementación procesa solo velas terminadas y utiliza vinculaciones de indicadores, por lo que cada decisión se basa en la última barra completada.

## Lógica de trading
1. **Alineación de tendencias**
   - Williams %R debe estar aumentando para operaciones largas o cayendo para operaciones cortas. La estrategia compara los valores de las dos barras previamente terminadas para evaluar la pendiente del impulso.
   - El componente de movimiento direccional del ADX (`+DI - -DI`) debe haber cruzado cero en la última barra cerrada: una transición de negativo a positivo confirma el impulso alcista, mientras que una transición de positivo a negativo confirma el impulso bajista.
2. **Reglas de entrada**
   - Si se cumplen ambas condiciones alcistas y la posición actual es plana o corta, la estrategia abre una orden de compra de mercado.
   - Si se cumplen ambas condiciones bajistas y la posición actual es plana o larga, la estrategia abre una orden de venta de mercado.
   - Cuando aparecen simultáneamente señales largas y cortas (raro, pero posible en valores idénticos), la operación se omite para evitar instrucciones contradictorias.
3. **Reglas de salida**
   - Las posiciones largas se cierran cuando el valor MFI de hace dos barras excede el nivel de sobrecompra o la línea principal Stochastic forma un patrón de valle local (`K[−2] > K[−1] < K[0]`).
   - Las posiciones cortas se cierran cuando el valor MFI de hace dos barras cae por debajo del nivel de sobreventa reflejado (`100 - level`) o la línea principal Stochastic forma un patrón de pico local (`K[−2] < K[−1] > K[0]`).
4. **Manejo de riesgos**
   - La conversión mantiene la mecánica de entrada y salida del asesor experto original. Las funciones de stop-loss y trailing de la fuente MQL no se reproducen; El control de riesgos debe gestionarse externamente o agregarse mediante protecciones StockSharp si es necesario.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `Candle Type` | Plazo para todos los cálculos de los indicadores. | plazo de 15 minutos |
| `Williams %R Period` | Período retrospectivo utilizado en el oscilador Williams %R. | 42 |
| `Directional Period` | Periodo para los cálculos de ADX (afecta a +DI/−DI). | 20 |
| `MFI Period` | Longitud del índice de flujo de dinero. | 19 |
| `MFI Level` | Umbral de sobrecompra utilizado para activar salidas. El nivel de sobreventa se calcula como `100 - value`. | 79 |
| `Stochastic %K` | Periodo %K del oscilador estocástico. | 22 |
| `Stochastic %D` | Periodo %D del oscilador estocástico. | 16 |
| `Stochastic Smoothing` | Suavizado adicional ("desaceleración") aplicado al oscilador estocástico. | 21 |

Todos los parámetros están expuestos como valores `StrategyParam`, por lo que se pueden optimizar o ajustar a través de la GUI StockSharp.

## Notas de uso
- Vincule la estrategia a cualquier instrumento y establezca un volumen adecuado antes de comenzar.
- La estrategia procesa solo velas completadas (`CandleStates.Finished`), lo que garantiza que los valores del indicador sean definitivos.
- La representación de gráficos está habilitada: Williams %R, ADX, MFI, Stochastic y las operaciones ejecutadas se trazan cuando hay un área de gráfico disponible.
- Para recrear el comportamiento original de MT5 con respecto a la gestión de paradas, considere agregar `StartProtection` o una lógica de riesgo personalizada según sea necesario.

## Diferencias con la versión MQL
- La conversión StockSharp utiliza enlaces de indicadores en lugar de la copia manual del búfer, pero las comprobaciones lógicas, incluida la validación de cruce por cero y los patrones de barras múltiples, siguen el asesor experto de MT5.
- Los filtros de sesión, la lógica de reintento y la gestión de paradas finales del código MQL se omiten intencionalmente para centrarse en el motor de señales principal solicitado para esta conversión.
