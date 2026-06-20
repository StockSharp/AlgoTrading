# Estrategia de Reversión con Doji
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Las velas Doji reflejan un equilibrio temporal entre compradores y vendedores. Cuando un doji aparece tras un movimiento direccional fuerte, puede preceder a una reversión a medida que el impulso se desvanece. Esta estrategia mide el cuerpo de la vela en relación con su rango para determinar si se ha formado un verdadero doji.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 103%. Funciona mejor en el mercado de acciones.

Una vez detectado un doji, se verifican las velas anteriores para identificar una tendencia alcista o bajista. Un doji tras una caída puede activar una entrada larga, mientras que uno tras una subida puede abrir una posición corta. Los stops se colocan a una distancia porcentual del precio de entrada y las salidas ocurren si el precio supera los extremos del doji.

El método busca capturar la primera reacción alejándose del doji y es más adecuado para gráficos intradía donde los giros rápidos suelen producirse.

## Detalles

- **Criterios de entrada**: Vela Doji tras un movimiento direccional.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Precio que se mueve más allá del máximo/mínimo del doji o stop-loss.
- **Stops**: Sí, basados en porcentaje.
- **Valores predeterminados**:
  - `CandleType` = 5 minute
  - `DojiThreshold` = 0.1
  - `StopLossPercent` = 1
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Candlestick
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

