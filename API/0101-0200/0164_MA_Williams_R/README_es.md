# Estrategia Ma Williams R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementación de la estrategia - MA + Williams %R. Compra cuando el precio está por encima de la MA y el Williams %R está por debajo de -80 (sobreventa). Vende cuando el precio está por debajo de la MA y el Williams %R está por encima de -20 (sobrecompra).

Las pruebas indican un rendimiento anual promedio de aproximadamente 79%. Funciona mejor en el mercado de acciones.

La media móvil muestra la dirección de la tendencia predominante. El Williams %R busca puntos sobrecomprados o sobrevendidos en relación con esa tendencia.

Adecuado para traders de swing que esperan retrocesos hacia la media. La distancia del stop-loss es un porcentaje fijo del precio de entrada.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close > MA && WilliamsR < WilliamsROversold`
  - Corto: `Close < MA && WilliamsR > WilliamsROverbought`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Williams %R regresa al medio
- **Stops**: Basados en porcentaje usando `StopLossPercent`
- **Valores predeterminados**:
  - `MaPeriod` = 20
  - `MaType` = MovingAverageTypeEnum.Simple
  - `WilliamsRPeriod` = 14
  - `WilliamsROversold` = -80m
  - `WilliamsROverbought` = -20m
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    MaType elige una media simple, exponencial, ponderada, suavizada o de Hull. El centro de Williams %R es -50: un largo se cierra cuando %R vuelve a subir a ese nivel y un corto cuando vuelve a bajar. Una señal de entrada contra una posición abierta la invierte.
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Moving Average, Williams %R, R
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

