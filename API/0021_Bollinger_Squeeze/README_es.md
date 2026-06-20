# Bollinger Squeeze
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia basada en la compresión de las Bandas de Bollinger

Las pruebas indican un retorno anual promedio de aproximadamente 100%. Funciona mejor en el mercado de forex.

Bollinger Squeeze espera un ancho de banda estrecho que indique baja volatilidad. Una ruptura fuera de las bandas inicia una operación en esa dirección y sale cuando el momentum falla o aparece una ruptura opuesta.

La condición de compresión indica una próxima expansión de volatilidad. Una vez activada, la operación monta el movimiento de ruptura y depende de un stop ATR o cruce de banda para salir.


## Detalles

- **Criterios de entrada**: Señales basadas en Bollinger.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2m
  - `SqueezeThreshold` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Bollinger
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Neural Networks: No
  - Divergencia: No
  - Nivel de riesgo: Medio

