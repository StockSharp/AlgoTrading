# Estrategia de ensueño
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia Daydream** es una conversión directa del asesor experto MQL4 *Daydream by Cothool*. El robot original opera en el gráfico USD/JPY H1 observando las rupturas de un canal de precios reciente y luego gestionando las operaciones con una toma de ganancias virtual. Este puerto StockSharp mantiene la misma lógica central mientras usa el nivel alto API: los canales Donchian entregan los niveles de ruptura, las órdenes se realizan a través de `BuyMarket` / `SellMarket` y toda la lógica de seguimiento se maneja dentro de la estrategia sin colocar órdenes reales de toma de ganancias en el intercambio.

Características clave:

- Sistema de ruptura de posición única que solo cambia de dirección después de que una vela se cierra fuera de los extremos del canal anterior.
- Toma de ganancias virtual medida en pips que aumenta con el precio para bloquear las ganancias y cierra las operaciones cuando se alcanza.
- Limitación de entrada para que solo pueda ocurrir una acción comercial (apertura/cierre) por vela, reflejando la restricción MQL4 `LastOrderTime`.

## Lógica de trading

1. Cree un canal Donchian con `ChannelPeriod` velas completadas y almacene los niveles superior/inferior anteriores.
2. Cuando una vela cierra **por debajo** de la banda inferior anterior:
   - Cerrar una posición corta existente.
   - En la siguiente vela, abra una nueva posición larga con `OrderVolume` y establezca el nivel de obtención de beneficios virtual en `close + TakeProfitPips * pipSize`.
3. Cuando una vela cierra **por encima** de la banda superior anterior:
   - Cerrar una posición larga existente.
   - En la siguiente vela, abra una nueva posición corta y establezca la toma de ganancias virtual en `close - TakeProfitPips * pipSize`.
4. Mientras una posición esté activa, ajuste el precio virtual de obtención de beneficios en cada barra. Si el precio alcanza ese nivel en una vela posterior, salga de la operación.

El tamaño del pip se deriva del valor `PriceStep`. Para pares JPY, esto convierte un paso de 0,001 en un incremento de 0,01 pips, coincidiendo con el comportamiento de MQL.

## Parámetros

| Nombre | Descripción | Predeterminado | Notas |
|------|-------------|---------|-------|
| `OrderVolume` | Volumen utilizado para cada nueva entrada al mercado. | `1` | Coincide con la entrada `Lots` del experto MQL. |
| `ChannelPeriod` | Número de velas completadas en el canal Donchian. | `25` | Refleja `ChannelPeriod` en MQL. |
| `Slippage` | Deslizamiento permitido en puntos. | `3` | Almacenado para que esté completo; las órdenes de mercado lo ignoran. |
| `TakeProfitPips` | Distancia del take-profit virtual en pips. | `15` | Se mueve con el precio mientras la posición está abierta. |
| `CandleType` | Plazo utilizado para crear el canal Donchian. | `1 hour` | Plazo predeterminado de la estrategia original. |

## Diagrama de flujo de trabajo

```
La vela se cierra
│
├─ ► Actualizar Donchian Canal (bandas anteriores)
│
├─ ► ¿Ruptura por debajo del mínimo anterior? ── ► Cerrar breve → programar larga siguiente barra
│
├─ ► ¿Ruptura por encima del máximo anterior? ─ ► Cerrar largo → programar siguiente barra corta
│
└─ ► Rastro de toma de ganancias virtual en dirección a la posición abierta
└─ ► ¿Precio alcanzado el objetivo virtual? → Cerrar posición
```

## Notas de uso

- Adjunte la estrategia a cualquier valor con velas en streaming. La configuración predeterminada coincide con la recomendación original USD/JPY H1.
- Sólo existe una posición a la vez. La estrategia evita abrir y cerrar operaciones dentro de la misma vela para replicar la lógica MQL4.
- La toma de ganancias es virtual: la salida se produce a través de una orden de mercado una vez se supera el nivel calculado. No se envían órdenes TP reales al corredor.
- Ajuste `CandleType` para que se ejecute en diferentes períodos de tiempo. Los períodos más altos requieren suficientes datos históricos para calentar el canal Donchian.

## Diferencias con la versión MQL4

- Utiliza el indicador StockSharp `DonchianChannels` en lugar de escanear manualmente los máximos y mínimos.
- Se conservan las ganancias finales y la limitación de acciones, pero la ejecución utiliza StockSharp órdenes de mercado sin depender de la gestión de tickets de MT4.
- El parámetro `Slippage` se mantiene para la paridad, aunque la ejecución del mercado en StockSharp no aplica el deslizamiento de la misma manera que en MT4.

## Archivos

- `CS/DaydreamStrategy.cs` – implementación de estrategia en C#.
- Versión de Python: aún no implementada.
