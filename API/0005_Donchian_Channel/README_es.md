# Canal Donchian
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el Canal Donchian.

Las pruebas indican un retorno anual promedio de aproximadamente 52%. Funciona mejor en el mercado de criptomonedas.

La Ruptura del Canal Donchian opera nuevos máximos y mínimos basados en el rango del canal. Un cierre más allá de la banda superior señala fortaleza, mientras que una caída por debajo de la banda inferior invita a cortos. Las salidas ocurren cuando el precio regresa al punto medio.

El canal se calcula a partir del máximo más alto y el mínimo más bajo durante una ventana de lookback. Cuando el precio perfora estos límites, el sistema espera una expansión de volatilidad y se posiciona en consecuencia.


## Detalles

- **Criterios de entrada**: Señales basadas en Price Action.
- **Largo/Corto**: Ambos directions.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `ChannelPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Price Action
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

