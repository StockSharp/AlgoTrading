# Estrategia de Convergencia de Precio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia estima la probabilidad de que el precio suba o baje comparando la suma de los valores OHLC4 de velas alcistas y bajistas. Se abre una posición larga cuando la probabilidad de subida supera el 50%, y una posición corta cuando la probabilidad de bajada supera el 50%.

Las pruebas indican un retorno anual promedio de aproximadamente el 37%. Tiene mejor desempeño en el mercado de criptomonedas.

La estrategia puede operar sobre todo el historial o en una ventana deslizante definida por el parámetro `Range`. El valor OHLC4 de cada vela se utiliza para ponderar las contribuciones de los movimientos alcistas y bajistas.

## Detalles

- **Criterios de entrada**: Una probabilidad de subida superior al 50% activa una entrada larga; una probabilidad de bajada superior al 50% activa una entrada corta.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `FullHistory` = true
  - `Range` = 200
  - `CandleType` = 1 minute
- **Filtros**:
  - Categoría: Estadístico
  - Dirección: Ambos
  - Indicadores: Personalizado
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
