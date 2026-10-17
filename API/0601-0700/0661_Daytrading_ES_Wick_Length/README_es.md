# Estrategia de Daytrading ES por Longitud de Mecha
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en una posición larga cuando la longitud total de la mecha de una vela supera su media móvil más un desplazamiento, y sale después de mantener la posición durante un número fijo de barras.

### Reglas verificables (C# y Python)

En cada **vela terminada de un minuto**, la mecha total es `W = max(0, High − max(Open, Close)) + max(0, min(Open, Close) − Low)`. Se usan las **20 velas anteriores**, sin incluir la actual, para calcular `VWMA(W) = sum(Wᵢ × Volumeᵢ) / sum(Volumeᵢ)`; si todo el volumen es cero se utiliza la media simple. `MaType=Simple` usa siempre la media sin ponderación. `MaOffset=10` representa **10 ticks mínimos del instrumento**; umbral `VWMA(W) + 10 × Security.PriceStep`. Después de 20 velas de preparación, se **compra únicamente** desde posición plana cuando `W > umbral`. Tras **18 velas terminadas adicionales**, se vende toda la posición restante. Sin cortos, stop-loss ni take-profit. Predeterminados: `MaLength=20`, `MaType=VolumeWeighted`, `MaOffset=10`, `HoldPeriods=18`, `CandleType=1 minuto`.



## Detalles

- **Criterios de entrada**: Longitud total de mecha mayor que la media móvil con desplazamiento.
- **Criterios de salida**: Posición cerrada después de mantener `Hold periods` barras.
- **Largo/Corto**: Solo largos.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `MA length` = 20
  - `MA type` = VolumeWeighted
  - `MA offset` = 10
  - `Hold periods` = 18
  - `Candle type` = velas de 1 minuto
- **Filtros**:
  - Categoría: Volatilidad
  - Dirección: Largo
  - Indicadores: Moving Average, longitud de mecha
  - Stops: No
  - Complejidad: Simple
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
