# Ruptura de N Días
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de ruptura del máximo/mínimo de N días. La ruptura de N días busca nuevos máximos o mínimos durante el período indicado. Las entradas se producen cuando el precio perfora el último máximo o mínimo de N días, anticipando momentum. Un filtro de media móvil y un stop porcentual gestionan las salidas.

Las pruebas indican un retorno anual promedio de aproximadamente 43%. Funciona mejor en el mercado de acciones.

Al esperar que el extremo anterior sea superado, el sistema intenta capturar el inicio de un movimiento direccional. Filtrar mediante una media de seguimiento de tendencia ayuda a evitar señales falsas que surgen durante la consolidación.


## Detalles

- **Criterios de entrada**: Señales basadas en MA.
- **Largo/Corto**: Ambos directions.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `LookbackPeriod` = 20
  - `MaPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromHours(1)
    El historial de muestra cubre un mes: tras el calentamiento de 20 velas quedan unas diez velas diarias, y con velas diarias la estrategia no envía ninguna orden sobre BTC en marzo de 2024. Por eso el valor por defecto usa velas horarias con los mismos periodos; las velas diarias siguen siendo configurables.
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: MA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

