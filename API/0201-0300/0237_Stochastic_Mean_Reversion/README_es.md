# Estrategia de Reversión a la Media con Stochastic Oscillator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia mide el Stochastic Oscillator contra su propia media móvil para localizar oscilaciones sobreextendidas. Cuando %K se mueve varios desviaciones estándar lejos de su media, la expectativa es que el indicador regrese hacia valores típicos.

Las pruebas indican un retorno anual promedio de aproximadamente 64%. Funciona mejor en el mercado de divisas.

Se coloca una operación larga cuando el %K del Stochastic cae por debajo de la banda inferior definida por el promedio menos `Multiplier` veces la desviación estándar. Una operación corta ocurre cuando %K supera la banda superior. Las posiciones se cierran una vez que %K cruza de vuelta a través de su línea promedio.

El método está diseñado para traders a corto plazo que les gusta operar en extremos de sobrecompra y sobreventa. El stop-loss protege contra el impulso sostenido que no logra revertirse a la media.

## Detalles
- **Criterios de entrada**:
  - **Largo**: %K < Avg - Multiplier * StdDev
  - **Corto**: %K > Avg + Multiplier * StdDev
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando %K > Avg
  - **Corto**: Salir cuando %K < Avg
- **Stops**: Sí, stop-loss porcentual.
- **Valores predeterminados**:
  - `StochPeriod` = 14
  - `KPeriod` = 3
  - `DPeriod` = 3
  - `AveragePeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Mean reversion
  - Dirección: Ambos
  - Indicadores: Stochastic Oscillator
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

