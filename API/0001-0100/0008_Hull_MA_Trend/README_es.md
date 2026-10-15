# Tendencia Hull MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en la tendencia de la Hull Moving Average.

Las pruebas indican un retorno anual promedio de aproximadamente 61%. Funciona mejor en el mercado de criptomonedas.

La estrategia de Tendencia Hull MA monitorea la pendiente de la Hull Moving Average. Las pendientes ascendentes provocan largos y las pendientes descendentes provocan cortos, con un stop trailing de ATR protegiendo cada operación.

Su cálculo receptivo reduce el rezago en comparación con las medias móviles tradicionales, permitiendo al sistema reaccionar rápidamente al nuevo momentum. El stop de ATR ayuda a evitar grandes drawdowns si la pendiente cambia abruptamente.


## Detalles

- **Criterios de entrada**: Señales basadas en MA, ATR.
- **Largo/Corto**: Ambos directions.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `HmaPeriod` = 9
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MA, ATR
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

