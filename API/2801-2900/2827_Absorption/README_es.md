# Estrategia de Absorción
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia replica el asesor experto Absorption para MetaTrader. Busca velas "envolventes" que absorben el rango de la barra anterior y forman un extremo dentro de un corto período de búsqueda. Cuando aparece dicha barra de absorción, el algoritmo coloca órdenes stop a ambos lados del mercado y gestiona la posición resultante con una combinación de objetivos fijos, lógica de breakeven y un trailing stop.

## Lógica de Trading

1. **Detección de patrones**
   - Se inspeccionan las últimas dos velas completadas.
   - Se trata una vela como *barra de absorción* cuando su máximo está por encima del máximo de la vela anterior y su mínimo está por debajo del mínimo de la vela anterior.
   - Se valida la barra comprobando si su máximo o mínimo es el valor más extremo dentro de las últimas `MaxSearch` velas.
   - Se da prioridad a la vela más antigua (dos barras atrás). Si ambas barras satisfacen la condición de absorción, se usa la barra más antigua; de lo contrario, la barra más reciente puede activar la configuración.
2. **Colocación de órdenes**
   - Se coloca una orden de compra stop en el máximo de la barra más el `Indent` configurado.
   - Se coloca una orden de venta stop en el mínimo de la barra menos el mismo `Indent`.
   - Ambas órdenes usan el volumen de estrategia común.
   - Cada orden pendiente almacena su propio nivel de stop protector y objetivo de take profit opcional. Las órdenes expiran automáticamente después de `OrderExpirationHours` si permanecen sin ejecutar.
3. **Gestión de posiciones**
   - Cuando se ejecuta un lado, la orden pendiente opuesta se cancela.
   - El stop inicial se sitúa en el lado opuesto de la vela de absorción menos/más el indent.
   - Un take profit fijo opcional cierra la operación una vez alcanzada la distancia configurada en pasos de precio.
   - El módulo de breakeven mueve el stop-loss a `Entrada + Breakeven` (largo) o `Entrada - Breakeven` (corto) después de que el precio avanza `BreakevenProfit` pasos.
   - El trailing stop mantiene el stop-loss a distancia `TrailingStop` del mejor precio, actualizándose solo cuando el precio se mueve al menos `TrailingStep` pasos en la dirección rentable.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Tipo de datos de vela para suscribirse (por defecto: marco temporal de 1 hora). |
| `MaxSearch` | Número de velas recientes usadas para confirmar extremos altos/bajos. |
| `TakeProfitBuy` | Distancia en pasos de precio para la orden de take profit largo. `0` deshabilita el objetivo. |
| `TakeProfitSell` | Distancia en pasos de precio para la orden de take profit corto. `0` deshabilita el objetivo. |
| `TrailingStop` | Distancia del trailing stop en pasos de precio. `0` deshabilita el trailing. |
| `TrailingStep` | Movimiento mínimo adelante requerido antes de avanzar el trailing stop. Debe ser positivo cuando el trailing está habilitado. |
| `Indent` | Desplazamiento en pasos de precio que se añade por encima/debajo de la barra de absorción para definir los niveles de entrada stop. |
| `OrderExpirationHours` | Tiempo de vida de las órdenes pendientes. Después de este período las órdenes se cancelan si no se activan. |
| `Breakeven` | Desplazamiento aplicado al stop-loss cuando se activa la regla de breakeven. `0` deshabilita el breakeven. |
| `BreakevenProfit` | Umbral de beneficio (en pasos de precio) que debe alcanzarse antes de mover el stop-loss a breakeven. |

Todas las entradas basadas en distancia se expresan como múltiplos del paso de precio del instrumento. El volumen de estrategia predeterminado se establece en `0.1`.

## Gestión de Riesgo

La estrategia usa solo órdenes de mercado para las salidas. Las reglas de stop-loss, take-profit, breakeven y trailing monitorean los máximos y mínimos de las velas para detectar tocamientos de nivel dentro de la barra. Una vez que se envía una orden de salida, no se generan solicitudes de salida adicionales hasta que la posición actual esté plana.
