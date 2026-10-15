# Estrategia XRSI Histograma Vol Directo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- **Fuente original**: `Exp_XRSI_Histogram_Vol_Direct.mq5`
- **Plataforma convertida**: API de estrategia de alto nivel StockSharp C#
- **Idea**: negociar reversiones cuando el histograma RSI suavizado ponderado por volumen cambia de pendiente
- **Datos**: instrumento único, marco temporal único (H4 por defecto)

La estrategia evalúa un oscilador personalizado construido a partir de valores RSI multiplicados por el volumen. Cuando la pendiente de este oscilador suavizado cambia, la estrategia revierte una posición o abre una nueva operación en la dirección opuesta. La lógica replica el enfoque de búfer de colores del asesor experto original rastreando la dirección de la pendiente de los últimos dos velas terminadas.

## Pila de indicadores y cálculos
1. **RSI** (`RsiPeriod`) se calcula en la serie de velas seleccionada y se centra alrededor de cero restando 50.
2. **Selección de volumen** usa ya sea el conteo de ticks o el volumen negociado, controlado por el parámetro `Use Tick Volume`.
3. **Oscilador ponderado por volumen** multiplica el RSI centrado por el volumen elegido, magnificando los movimientos que coinciden con mayor actividad.
4. **Suavizado** aplica la media móvil seleccionada (`SMA`, `EMA`, `SMMA`, `WMA`) con período `SmoothLength` tanto al oscilador como al flujo de volumen bruto. El indicador se considera listo solo después de que ambos valores suavizados estén formados.
5. **Detección de pendiente** compara el valor actual del oscilador suavizado con el anterior:
   - Valor más alto → color de pendiente `0` (subiendo)
   - Valor más bajo → color de pendiente `1` (bajando)
   - Plano → mantener el color anterior

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| Candle Type | Marco temporal H4 | Suscripción de vela objetivo. |
| RSI Period | 14 | Período de retrospección para el cálculo RSI. |
| Smoothing Length | 12 | Período de la media móvil aplicada tanto al oscilador como al volumen. |
| Smoothing Method | SMA | Tipo de media móvil (`SMA`, `EMA`, `SMMA`, `WMA`). |
| Use Tick Volume | `true` | Usar conteo de ticks (`true`) o volumen negociado (`false`). |
| Allow Buy Open | `true` | Habilitar apertura de posiciones largas. |
| Allow Sell Open | `true` | Habilitar apertura de posiciones cortas. |
| Allow Buy Close | `true` | Permitir cerrar posiciones largas en señal opuesta. |
| Allow Sell Close | `true` | Permitir cerrar posiciones cortas en señal opuesta. |

> **Nota**: A diferencia del indicador MQL original, los suavizadores avanzados como JJMA o VIDYA no están disponibles en el framework StockSharp. Por tanto, la estrategia expone las alternativas integradas más cercanas.

## Reglas de trading
1. Esperar hasta que ambos indicadores de suavizado tengan suficientes datos.
2. Determinar el color de pendiente de los últimos dos velas completadas.
3. **Si el color más antiguo es ascendente (`0`)**:
   - Cerrar cualquier posición corta abierta si se permite.
   - Si el color más reciente es descendente (`1`) y las entradas largas están permitidas, abrir una posición larga (refleja la lógica de reversión del EA).
4. **Si el color más antiguo es descendente (`1`)**:
   - Cerrar cualquier posición larga abierta si se permite.
   - Si el color más reciente es ascendente (`0`) y las entradas cortas están permitidas, abrir una posición corta.

La estrategia efectivamente negocia el "cambio de color" de la pendiente del histograma, ejecutando al cierre de la vela más nueva terminada.

## Consejos prácticos
- La lógica es sensible al marco temporal elegido. Prueba varios intervalos para coincidir con el comportamiento del EA original.
- Porque solo se usa la dirección de la pendiente, añadir un stop loss o take profit a través de `StartProtection` puede mejorar el control de riesgo en el trading en vivo.
- Usa la visualización en el terminal de gráficos para comparar la pendiente del oscilador StockSharp con el indicador MT5 original al validar el port.

## Diferencias de la versión MQL
- Los ayudantes de gestión de dinero (`TradeAlgorithms.mqh`) no están portados; la implementación de StockSharp depende del volumen de la estrategia base.
- Solo los métodos de suavizado compatibles con StockSharp están expuestos. Los modos no compatibles se comportan como SMA.
- Las órdenes se envían inmediatamente en la vela terminada, por lo que no se requiere el desplazamiento de tiempo explícito (`SignalBar` / `TimeShiftSec`).
- Los stops protectores no están codificados de forma fija; los usuarios pueden añadirlos a través de `StartProtection` si es necesario.

## Limitaciones
- Requiere una fuente de velas que proporcione ya sea conteos de ticks o totales de volumen para reproducir la amplitud del oscilador correctamente.
- La estrategia no dibuja el histograma personalizado en sí; se enfoca en la lógica de trading y superposiciones de gráficos opcionales para RSI.
