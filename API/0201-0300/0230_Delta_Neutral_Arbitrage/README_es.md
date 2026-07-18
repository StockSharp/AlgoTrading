# Estrategia de Arbitraje Delta Neutral
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia de arbitraje opera el diferencial entre dos activos correlacionados manteniendo la posición combinada cerca de delta neutral. Al equilibrar una posición larga en un activo contra una corta en otro, intenta beneficiarse de la reversión a la media del diferencial en lugar de la dirección del mercado.

Las pruebas indican un retorno anual promedio de aproximadamente 43%. Funciona mejor en el mercado de acciones.

Se entra en un diferencial largo cuando el z-score de la diferencia de precios cae por debajo de `-EntryThreshold`. El primer activo se compra y el segundo se vende en igual tamaño. Un diferencial corto hace lo contrario cuando el z-score sube por encima del umbral positivo. La operación se cierra una vez que el diferencial regresa a la media móvil.

La operativa delta neutral es popular entre los traders cuantitativos que buscan exposición de baja volatilidad. Aunque está cubierta, la protección con stop-loss se aplica igualmente para protegerse contra la divergencia extrema entre los activos.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Spread Z-Score < -EntryThreshold
  - **Corto**: Spread Z-Score > EntryThreshold
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando el diferencial vuelve a cruzar por encima de la media
  - **Corto**: Salir cuando el diferencial vuelve a cruzar por debajo de la media
- **Stops**: Sí, stop-loss porcentual sobre el valor del diferencial.
- **Valores predeterminados**:
  - `LookbackPeriod` = 20
  - `EntryThreshold` = 2m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Arbitraje
  - Dirección: Ambos
  - Indicadores: Spread statistics
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio

