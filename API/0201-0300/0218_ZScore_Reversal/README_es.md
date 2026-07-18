# Estrategia de Reversión Z-Score
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Reversión Z-Score mide cuánto se desvía el precio de una media móvil en términos de desviaciones estándar. El Z-Score resultante destaca condiciones estadísticamente extendidas que pueden revertirse hacia la media.

Las pruebas indican un retorno anual promedio de aproximadamente 91%. Funciona mejor en el mercado de acciones.

Se abre una operación larga cuando el Z-Score cae por debajo de un umbral negativo, señalando un mercado sobrevendido. Se toma una operación corta cuando el Z-Score sube por encima del umbral positivo. La posición se cierra una vez que el Z-Score cruza de vuelta a través de cero, indicando que el precio se ha normalizado.

Esta técnica es atractiva para los traders de reversión a la media que prefieren niveles de entrada objetivos. El porcentaje de stop-loss mantiene los movimientos adversos manejables mientras se espera la reversión.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Z-Score < -Umbral
  - **Corto**: Z-Score > Umbral
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando el Z-Score cruza por encima de 0
  - **Corto**: Salir cuando el Z-Score cruza por debajo de 0
- **Stops**: Sí, stop-loss porcentual.
- **Valores predeterminados**:
  - `LookbackPeriod` = 20
  - `ZScoreThreshold` = 2.0m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(10)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Z-Score
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
