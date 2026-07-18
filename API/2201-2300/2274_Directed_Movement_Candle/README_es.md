# Estrategia de Vela de Movimiento Dirigido
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia monitorea el Índice de Fuerza Relativa (RSI) en los cierres de velas. Cuando el RSI sale de la zona neutral y cruza niveles definidos por el usuario, la estrategia abre posiciones en la dirección del impulso y cierra cualquier exposición opuesta.

## Detalles

- **Indicador**: Índice de Fuerza Relativa con `RsiPeriod` ajustable.
- **HighLevel**: valor del RSI que indica impulso alcista.
- **MiddleLevel**: umbral neutral mantenido como referencia.
- **LowLevel**: valor del RSI que indica impulso bajista.
- **Entrada**:
  - Largo cuando el RSI sube por encima de `HighLevel` después de estar por debajo.
  - Corto cuando el RSI cae por debajo de `LowLevel` después de estar por encima.
- **Salida**: La señal opuesta cierra la posición existente antes de abrir una nueva.
- **Largo/Corto**: Ambas direcciones.
- **Stops**: No se usan por defecto.
- **Valores predeterminados**:
  - `RsiPeriod` = 14
  - `HighLevel` = 70
  - `MiddleLevel` = 50
  - `LowLevel` = 30
  - `CandleType` = marco temporal de 5 minutos
