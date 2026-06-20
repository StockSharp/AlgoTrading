# Estrategia de Reversión Heikin-Ashi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Las velas Heikin-Ashi suavizan el ruido y destacan la dirección de la tendencia. Un cambio de una serie de velas HA bajistas a una alcista, o viceversa, puede indicar un cambio de momentum. Esta estrategia opera esos cambios de color y utiliza un stop porcentual para protección.

Las pruebas indican una rentabilidad anual media de aproximadamente el 145%. Funciona mejor en el mercado de criptomonedas.

La lógica calcula los valores Heikin-Ashi a partir de las velas regulares. Cuando el cierre HA cruza por encima de la apertura HA tras una secuencia bajista, se toma una posición larga. Un cruce por debajo tras una racha alcista abre una posición corta. El stop se coloca a un porcentaje fijo desde la entrada.

El método es simple pero efectivo durante oscilaciones irregulares cuando los gráficos de velas tradicionales son ruidosos.

## Detalles

- **Criterios de entrada**: La vela Heikin-Ashi cambia de color.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop-loss.
- **Stops**: Sí, basado en porcentaje.
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Heikin-Ashi
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

