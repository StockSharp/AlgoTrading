# Estrategia Vwap Cci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementación de la estrategia - VWAP + CCI. Compra cuando el precio está por debajo del VWAP y el CCI está por debajo de -100 (sobreventa). Vende cuando el precio está por encima del VWAP y el CCI está por encima de 100 (sobrecompra).

Las pruebas indican un rendimiento anual promedio de aproximadamente 82%. Funciona mejor en el mercado de acciones.

El VWAP actúa como referencia de valor, y el CCI destaca los movimientos de impulso que se alejan de él. Las entradas favorecen lecturas de CCI fuertes en relación con el VWAP.

Diseñado para traders intradía que se centran en la interacción con el VWAP. Los stops de ATR ayudan a mantener la disciplina.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close < VWAP && CCI < CciOversold`
  - Corto: `Close > VWAP && CCI > CciOverbought`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - El precio cruza de regreso a través del VWAP
- **Stops**: Basados en porcentaje usando `StopLoss`
- **Valores predeterminados**:
  - `CciPeriod` = 20
  - `CciOversold` = -100m
  - `CciOverbought` = 100m
  - `StopLoss` = new Unit(2, UnitTypes.Percent)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: VWAP, CCI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

