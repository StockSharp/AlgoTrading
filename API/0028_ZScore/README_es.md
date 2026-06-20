# ZScore
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el indicador Z-Score para trading de reversión a la media

Las pruebas indican un rendimiento anual promedio de aproximadamente 121%. Funciona mejor en el mercado de criptomonedas.

ZScore mide la desviación del precio respecto a una media móvil. Los Z-scores extremadamente altos o bajos sugieren sobreextensión e impulsan operaciones en la dirección opuesta. La operación finaliza cuando el Z-score se normaliza.

El Z-Score es un filtro flexible porque puede escalarse a cualquier serie temporal. Usar una salida ajustada a la volatilidad ayuda al sistema a adaptarse a las condiciones cambiantes del mercado.


## Detalles

- **Criterios de entrada**: Señales basadas en MA, ZScore.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `ZScoreEntryThreshold` = 2.0m
  - `ZScoreExitThreshold` = 0.0m
  - `MAPeriod` = 20
  - `StdDevPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: MA, ZScore
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

