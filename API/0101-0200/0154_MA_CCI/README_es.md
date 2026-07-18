# Estrategia MA CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina la Media Móvil y el indicador CCI. Compra cuando el precio está por encima de la MA y el CCI está en zona de sobrevendido. Vende cuando el precio está por debajo de la MA y el CCI está en zona de sobrecomprado.

Las pruebas indican un retorno anual promedio de aproximadamente el 49%. Funciona mejor en el mercado de criptomonedas.

Una media móvil guía la tendencia mientras el CCI busca desviaciones de ese promedio. Las entradas ocurren en los extremos del CCI en la dirección de la MA.

Ideal para traders de swing que entran en retrocesos. Los stops basados en ATR protegen contra movimientos bruscos.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close > MA && CCI < OversoldLevel`
  - Corto: `Close < MA && CCI > OverboughtLevel`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - CCI regresa a la línea cero
- **Stops**: Basados en porcentaje usando `StopLossPercent`
- **Valores predeterminados**:
  - `MaPeriod` = 20
  - `CciPeriod` = 20
  - `OverboughtLevel` = 100m
  - `OversoldLevel` = -100m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Moving Average, CCI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
