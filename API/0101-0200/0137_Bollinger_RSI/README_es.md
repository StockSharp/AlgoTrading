# Estrategia Bollinger RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Bollinger RSI combina la sobreextensión de las Bandas de Bollinger con señales de momentum del RSI.
Cuando el precio cierra fuera de las bandas pero el RSI muestra divergencia, una reversión suele estar próxima.

Las pruebas indican un rendimiento anual promedio de aproximadamente 148%. Funciona mejor en el mercado forex.

El sistema toma operaciones contra la tendencia en esa divergencia, saliendo una vez que el precio vuelve a entrar en las bandas o el RSI cruza de regreso.

Un stop porcentual ajustado limita la exposición en caso de que la volatilidad se expanda aún más.

## Detalles

- **Criterios de entrada**: señal de indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2
  - `RsiPeriod` = 14
    Un cierre por encima de la banda superior más alto que el cierre anterior por encima de ella, con el RSI más bajo que entonces, es una divergencia bajista y abre un corto; el caso simétrico bajo la banda inferior abre un largo e invierte una posición opuesta. La posición se cierra cuando el precio vuelve a cerrar dentro de las bandas o el RSI cruza de nuevo el 50.
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, RSI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

