# Estrategia Pin Bar Magic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Detecta pin bars alcistas y bajistas dentro de una tendencia definida por un trío de medias móviles. Las órdenes se colocan en los extremos de la vela y se cancelan después de algunos barras si no se ejecutan. El tamaño de la posición se calcula a partir de un porcentaje de riesgo del capital y la distancia del stop basada en ATR.

El método busca capturar reversiones bruscas en soporte o resistencia significativos. Sale de las posiciones cuando las EMAs rápida y media se cruzan en la dirección opuesta, señalando debilidad de la tendencia.

## Detalles

- **Criterios de entrada**:
  - **Largo**: EMA rápida > EMA media > SMA lenta, pin bar alcista que perfora una de las medias.
  - **Corto**: EMA rápida < EMA media < SMA lenta, pin bar bajista que perfora una de las medias.
- **Criterios de salida**:
  - La EMA rápida cruza la EMA media en la dirección opuesta.
- **Indicadores**:
  - SMA lenta (período 50)
  - EMA media (18) y EMA rápida (6)
  - ATR (longitud 14)
- **Stops**: Riesgo de posición = EquityRisk% de la cuenta con stop en ATR * multiplicador.
- **Valores predeterminados**:
  - `EquityRisk` = 3
  - `AtrMultiplier` = 0.5
  - `SlowSmaLength` = 50
  - `MediumEmaLength` = 18
  - `FastEmaLength` = 6
  - `AtrLength` = 14
  - `CancelEntryBars` = 3
- **Filtros**:
  - Reversión de acción del precio
  - Funciona en velas de 1h por defecto
  - Indicadores: EMA, SMA, ATR
  - Stops: Sí
  - Complejidad: Alto
