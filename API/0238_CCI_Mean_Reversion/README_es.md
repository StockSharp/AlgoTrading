# Estrategia de Reversión a la Media con CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
El Commodity Channel Index (CCI) mide cuánto se aleja el precio de su promedio estadístico. Esta estrategia entra cuando el CCI se desvía de su propia media por un gran margen, esperando un retroceso rápido una vez que el impulso se desvanece.

Las pruebas indican un retorno anual promedio de aproximadamente 151%. Funciona mejor en el mercado de acciones.

Una operación larga ocurre cuando el CCI cae por debajo del promedio menos `DeviationMultiplier` veces la desviación estándar. Se abre una operación corta cuando el CCI sube por encima del promedio más ese multiplicador. La posición se cierra cuando el CCI vuelve a cruzar el valor medio.

Este sistema es adecuado para traders a corto plazo que prefieren configuraciones contrarias. Un stop-loss basado en el movimiento porcentual ayuda a limitar el riesgo si el mercado no logra revertirse rápidamente.

## Detalles
- **Criterios de entrada**:
  - **Largo**: CCI < Avg - DeviationMultiplier * StdDev
  - **Corto**: CCI > Avg + DeviationMultiplier * StdDev
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando CCI > Avg
  - **Corto**: Salir cuando CCI < Avg
- **Stops**: Sí, stop-loss porcentual.
- **Valores predeterminados**:
  - `CciPeriod` = 20
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Mean reversion
  - Dirección: Ambos
  - Indicadores: CCI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

