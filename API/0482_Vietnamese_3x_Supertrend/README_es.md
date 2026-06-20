# Estrategia Vietnamese 3x Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia apila tres indicadores SuperTrend con diferentes longitudes ATR y multiplicadores. Escala en posiciones largas cuando la tendencia lenta es bajista y las tendencias más rápidas muestran oportunidades de pullback. Un stop de break-even opcional protege las ganancias una vez que el precio se mueve favorablemente.

## Detalles

- **Criterios de entrada**:
  - SuperTrend lento en tendencia bajista.
  - **Long 1**: Tendencia media alcista y tendencia rápida bajista.
  - **Long 2**: Tendencia media bajista y precio por encima de la línea del SuperTrend rápido.
  - **Long 3**: Tendencia rápida bajista y ruptura por encima del máximo más alto durante la tendencia bajista rápida.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - Todos los SuperTrend giran al alza y la vela cierra bajista.
  - Precio de entrada promedio por encima del cierre actual.
  - Stop de break-even opcional si está habilitado.
- **Stops**: Stop de break-even opcional.
- **Valores predeterminados**:
  - `FastAtrLength` = 10
  - `FastMultiplier` = 1
  - `MediumAtrLength` = 11
  - `MediumMultiplier` = 2
  - `SlowAtrLength` = 12
  - `SlowMultiplier` = 3
  - `UseHighestOfTwoRedCandles` = False
  - `UseEntryStopLoss` = True
  - `UseAllDowntrendExit` = True
  - `UseAvgPriceInLoss` = True
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo
  - Indicadores: SuperTrend
  - Stops: Opcional
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
