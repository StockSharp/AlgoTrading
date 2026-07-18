# Estrategia dual AG MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una adaptación StockSharp del experto MetaTrader 4 **AG.mq4**. El robot opera con dos cálculos de convergencia y divergencia de media móvil (MACD) que utilizan diferentes conjuntos de parámetros. El MACD primario produce activadores de entrada, mientras que el MACD secundario (escalado) actúa como un filtro direccional para evitar operaciones contratendencias y controlar las salidas. La lógica refleja al experto MQL4 original al evaluar solo velas cerradas y reutilizar las comprobaciones de señales de la línea de señal que activaron las órdenes originales.

## Lógica de trading
- **Indicadores**
  - Primario MACD: EMA rápida = `FastEmaLength`, EMA lenta = `SlowEmaLength`, señal SMA = `SignalSmaLength`.
  - Secundario MACD: EMA rápida = `SlowEmaLength * 2`, EMA lenta = `FastEmaLength * 2`, señal SMA = `SignalSmaLength * 2`.
- **Entrada larga**
  - La línea principal primaria MACD está por encima de su línea de señal.
  - La línea de señal primaria MACD es negativa (debajo de la línea de flotación).
  - La línea principal secundaria MACD está por encima de su línea de señal.
  - La línea de señal secundaria MACD es negativa.
- **Entrada corta**
  - La línea principal primaria MACD está debajo de su línea de señal.
  - La línea de señal primaria MACD es positiva.
  - La línea principal secundaria MACD está debajo de su línea de señal.
  - La línea de señal secundaria MACD es positiva.
- **Reglas de salida**
  - Cierre posiciones largas cuando el MACD secundario se vuelva bajista mientras la línea de señal primaria se mantiene por encima de cero.
  - Cierre posiciones cortas cuando el MACD secundario se vuelva alcista mientras la línea de señal primaria permanece por debajo de cero.
- La estrategia sólo reacciona a las velas terminadas e ignora las barras sin terminar para evitar volver a pintarlas.

## Gestión de Puestos
- Todas las órdenes son órdenes de mercado con el volumen fijo definido por `OrderVolume`.
- `MaxOpenOrders` refleja la entrada original `ORDER` y limita el número total de órdenes activas más posiciones abiertas. Configúrelo en `0` para quitar la tapa.
- `StartProtection()` se habilita una vez que comienza la estrategia para que el administrador de riesgos StockSharp pueda monitorear la exposición abierta.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `OrderVolume` | Tamaño de lote base para nuevas operaciones. |
| `FastEmaLength` | Período EMA rápida del MACD primario. |
| `SlowEmaLength` | Período EMA lenta del MACD primario. |
| `SignalSmaLength` | Período de suavizado de señal para ambos MACD. |
| `MaxOpenOrders` | Número máximo de órdenes activas y posiciones abiertas combinadas. Establece `0` para ilimitado. |
| `CandleType` | Marco de tiempo utilizado para construir velas para ambos indicadores. |

## Notas
- El MACD secundario mantiene el mismo orden rápido/lento que en el EA original, incluso si el período rápido se vuelve más grande que el lento, para preservar los cálculos del autor.
- La estrategia no coloca órdenes pendientes; se abre o cierra en el mercado tan pronto como aparecen las condiciones.
- No se agregan niveles adicionales de stop-loss o take-profit porque el experto original se basó exclusivamente en las reversiones de señales.
