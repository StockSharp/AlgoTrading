# Estrategia Color Zerolag X10 Ma
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port simplificado del ejemplo de MetaTrader **Exp_ColorZerolagX10MA.mq5**. Utiliza una media móvil exponencial de cero rezago para detectar cambios de pendiente. Cuando la media móvil gira hacia arriba después de decrecer durante dos barras, la estrategia abre o revierte a una posición larga. Por el contrario, cuando la media móvil gira hacia abajo después de aumentar, abre o revierte a una posición corta.

La lógica imita la idea original donde un conjunto combinado de diez medias móviles suavizadas produce una única línea codificada por colores. Aquí reemplazamos ese indicador complejo con el `ZeroLagExponentialMovingAverage` integrado de StockSharp para mantener la implementación compacta y reutilizable. El sistema trabaja en el marco temporal de velas seleccionado y puede habilitar o deshabilitar acciones individuales (abrir/cerrar largo/corto) mediante parámetros.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `ZLEMA[t-2] > ZLEMA[t-1]` y `ZLEMA[t] > ZLEMA[t-1]`.
  - **Corto**: `ZLEMA[t-2] < ZLEMA[t-1]` y `ZLEMA[t] < ZLEMA[t-1]`.
- **Largo/Corto**: Ambas direcciones soportadas.
- **Criterios de salida**:
  - Las posiciones largas se cierran cuando aparece una señal corta y `BuyPosClose` está habilitado.
  - Las posiciones cortas se cierran cuando aparece una señal larga y `SellPosClose` está habilitado.
- **Stops**: Ninguno por defecto; las salidas dependen de señales opuestas.
- **Valores predeterminados**:
  - `Length` = 20.
  - `CandleType` = marco temporal de 4 horas.
  - Todos los indicadores de acción (`BuyPosOpen`, `SellPosOpen`, `BuyPosClose`, `SellPosClose`) habilitados.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Único
  - Stops: No
  - Complejidad: Simple
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
