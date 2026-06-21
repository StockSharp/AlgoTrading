# Renko RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera ladrillos Renko utilizando señales de sobrecompra/sobreventa del RSI.

Las pruebas muestran un rendimiento moderado y funciona mejor en mercados con tendencias Renko claras.

Renko RSI utiliza ladrillos Renko construidos a partir del ATR y aplica un RSI corto. Un cruce por encima del nivel de sobreventa activa una compra, mientras que una caída por debajo del nivel de sobrecompra activa una venta.

## Detalles

- **Criterios de entrada**: RSI cruza los niveles de sobreventa o sobrecompra.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `RenkoAtrLength` = 14
  - `RsiLength` = 2
  - `RsiOverbought` = 80
  - `RsiOversold` = 20
  - `CandleType` = Renko ATR(14)
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: RSI, Renko
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Renko
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
