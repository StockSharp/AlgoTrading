# Estrategia de DynamicRS_C
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el asesor experto de MetaTrader **Exp_DynamicRS_C** usando la API de alto nivel de StockSharp. Evalúa las transiciones de color del indicador personalizado DynamicRS_C para detectar soporte y resistencia dinámicos. Cuando la línea se vuelve magenta (índice de color `0`) favorece configuraciones alcistas, y cuando se vuelve azul-violeta (índice de color `2`) favorece configuraciones bajistas. El port de StockSharp mantiene el mismo tiempo de señal, indicadores de permiso y estructura stop/take que el robot fuente.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La vela terminada seleccionada por `SignalBar` cambia el color del indicador de cualquier cosa excepto `0` a `0`. La estrategia opcionalmente cierra un corto existente antes de entrar, replicando la compuerta `SellPosClose` original, y luego abre un largo si `AllowBuyEntry` está habilitado.
  - **Corto**: La vela evaluada cambia el color del indicador de cualquier cosa excepto `2` a `2`. La estrategia opcionalmente cierra un largo existente (`AllowBuyExit`) y luego abre un corto si `AllowSellEntry` está habilitado.
- **Largo/Corto**: Opera en ambas direcciones con interruptores independientes para entradas y salidas.
- **Criterios de salida**:
  - Las posiciones largas se cierran cuando aparece una señal corta y `AllowBuyExit` es verdadero, o cuando se alcanzan los límites de stop-loss / take-profit.
  - Las posiciones cortas se cierran cuando aparece una señal larga y `AllowSellExit` es verdadero, o cuando se activan los límites de riesgo.
- **Stops**: `StopLossPoints` y `TakeProfitPoints` son desplazamientos de precio absolutos desde el precio de entrada. Establecer cualquier valor en cero desactiva esa protección.
- **Filtros**:
  - `SignalBar` determina cuántas velas completadas hacia atrás se inspeccionan para un cambio de color, imitando la búsqueda del buffer original (`CopyBuffer(..., SignalBar, 2)`).
  - `CandleType` selecciona el marco temporal usado tanto para el indicador como para la lógica de negociación (por defecto: velas de 4 horas, coincidiendo con el EA).

## Parámetros

- `CandleType` – Serie de velas procesada por la estrategia.
- `Length` – Profundidad de retrovisión usada por el indicador DynamicRS_C para comparar máximos/mínimos (`Length` en MQL).
- `SignalBar` – Número de velas completamente cerradas hacia atrás usadas para la evaluación de señales (equivalente a la entrada del EA `SignalBar`).
- `AllowBuyEntry` / `AllowSellEntry` – Permite abrir posiciones largas/cortas en sus respectivas señales.
- `AllowBuyExit` / `AllowSellExit` – Permite cerrar posiciones largas/cortas existentes cuando aparece la señal opuesta.
- `StopLossPoints` – Distancia de pérdida absoluta desde el precio de entrada. Cuando es positivo cierra largos por debajo y cortos por encima de la entrada.
- `TakeProfitPoints` – Distancia de ganancia absoluta desde el precio de entrada. Cuando es positivo cierra largos por encima y cortos por debajo de la entrada.
- `Volume` – Tamaño de orden base heredado de `Strategy.Volume`. La cantidad adicional se agrega automáticamente para aplanar posiciones opuestas cuando la señal solicita una reversión.

## Lógica del indicador

El `DynamicRsCIndicator` incluido reproduce el comportamiento del buffer de color del script MetaTrader:

- Rastrea los últimos máximos y mínimos sobre la ventana `Length` configurada y la barra inmediatamente anterior.
- Cuando un máximo local es menor que el máximo anterior y el máximo hace `Length` barras, y también está por debajo del valor anterior del indicador, el buffer cambia al color `0` (magenta) y el valor se ajusta a ese máximo.
- Cuando un mínimo local es mayor que el mínimo anterior y el mínimo hace `Length` barras, y está por encima del valor anterior del indicador, el buffer cambia al color `2` (azul-violeta) y el valor se ajusta a ese mínimo.
- De lo contrario, el indicador mantiene su valor anterior. El color neutro `1` actúa como puente entre los estados de tendencia exactamente como en el algoritmo original.

Al vincular este indicador a través de `BindEx`, la estrategia recibe tanto el valor numérico como el índice de color discreto, asegurando que la evaluación de señales y el tiempo de operación coincidan con el comportamiento del experto fuente.
