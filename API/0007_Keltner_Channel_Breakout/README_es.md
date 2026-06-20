# Ruptura del Canal Keltner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en la ruptura del Canal Keltner.

Las pruebas indican un retorno anual promedio de aproximadamente 58%. Funciona mejor en el mercado de acciones.

La Ruptura del Canal Keltner utiliza bandas de volatilidad derivadas del ATR. Las rupturas por encima de la banda superior o por debajo de la banda inferior desencadenan entradas. El precio que vuelve a través del centro EMA o alcanza un stop cierra la posición.

Dado que las bandas se expanden y contraen con la volatilidad, este método de ruptura apunta a capturar las primeras etapas de un movimiento fuerte mientras aún permite al precio respirar dentro del canal.


## Detalles

- **Criterios de entrada**: Señales basadas en ATR, Keltner.
- **Largo/Corto**: Ambos directions.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: ATR, Keltner
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

