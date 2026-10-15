# Estrategia Hurst Future Lines of Demarcation
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia utiliza una FLD (Future Line of Demarcation) suavizada y tres longitudes de ciclo (señal, trading, tendencia). Entra cuando el precio cruza la FLD de señal en estados de tendencia específicos y sale en un cruce entre los valores seleccionados.

## Detalles

- **Criterios de entrada**:
  - Comprar cuando el precio cruza hacia arriba la FLD de señal mientras el estado de tendencia es igual a 1.
  - Vender cuando el precio cruza hacia abajo la FLD de señal mientras el estado de tendencia es igual a 6.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Cerrar posición cuando `CloseTrigger1` cruza `CloseTrigger2` en dirección opuesta al trade.
- **Stops**: No.
- **Valores predeterminados**:
  - `SmoothFld` = false
  - `FldSmoothing` = 5
  - `SignalCycleLength` = 5
  - `TradeCycleLength` = 20
  - `TrendCycleLength` = 80
  - `CloseTrigger1` = Price
  - `CloseTrigger2` = Trade
