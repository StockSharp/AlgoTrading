# Estrategia de Reversión a la Media con RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia rastrea el índice de fuerza relativa y mide su distancia desde un nivel promedio. Cuando el RSI se desvía más de un múltiplo de su desviación estándar reciente, el algoritmo espera un retroceso hacia la media.

Las pruebas indican un retorno anual promedio de aproximadamente 61%. Funciona mejor en el mercado cripto.

Se abre una operación larga cuando el RSI cae por debajo de la banda inferior definida por el promedio menos `Multiplier` veces la desviación estándar. Se toma una operación corta cuando el RSI sube por encima de la banda superior. Las salidas ocurren cuando el RSI regresa a su media móvil.

El método es adecuado para traders que buscan señales objetivas de sobrecompra y sobreventa. Usar una banda basada en volatilidad adapta los umbrales a las condiciones actuales del mercado mientras un stop-loss mantiene las pérdidas limitadas.

## Detalles
- **Criterios de entrada**:
  - **Largo**: RSI < Avg - Multiplier * StdDev
  - **Corto**: RSI > Avg + Multiplier * StdDev
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando RSI > Avg
  - **Corto**: Salir cuando RSI < Avg
- **Stops**: Sí, stop-loss porcentual.
- **Valores predeterminados**:
  - `RsiPeriod` = 14
  - `AveragePeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Mean reversion
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

