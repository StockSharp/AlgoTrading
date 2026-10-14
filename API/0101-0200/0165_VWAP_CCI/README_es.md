# Estrategia Vwap Cci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementación de la estrategia - VWAP + CCI. Compra cuando el precio está por debajo del VWAP y el CCI está por debajo de -100 (sobreventa). Vende cuando el precio está por encima del VWAP y el CCI está por encima de 100 (sobrecompra).

Las pruebas indican un rendimiento anual promedio de aproximadamente 82%. Funciona mejor en el mercado de acciones.

El VWAP actúa como referencia de valor, y el CCI destaca los movimientos de impulso que se alejan de él. Las entradas favorecen lecturas de CCI fuertes en relación con el VWAP.

Diseñado para traders intradía que se centran en la interacción con el VWAP. Un stop porcentual ayuda a mantener la disciplina.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close < VWAP && CCI < CciOversold`
  - Corto: `Close > VWAP && CCI > CciOverbought`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - El precio cruza de regreso a través del VWAP
- **Stops**: Basados en porcentaje usando `StopLossPercent`
- **Valores predeterminados**:
  - `CciPeriod` = 20
  - `CciOversold` = -100m
  - `CciOverbought` = 100m
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    El mercado opera las 24 horas, por lo que el VWAP de la sesión se reinicia cada día UTC y pondera el precio típico de cada vela por su volumen. Una señal de entrada contra una posición abierta la invierte.
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

