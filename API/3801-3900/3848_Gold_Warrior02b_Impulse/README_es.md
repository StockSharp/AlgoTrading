# Estrategia GoldWarrior02b
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Puerto StockSharp completo del asesor experto MetaTrader 4 *GoldWarrior02b* (carpeta `MQL/7694`).
Combina un índice de canales de productos básicos (CCI), un medidor de impulso personalizado y un detector de oscilación en ZigZag hecho a mano.
y evalúa las señales sólo unos segundos antes de cada límite de 15 minutos. El objetivo de esta traducción es
para imitar la lógica de alto nivel del robot original respetando al mismo tiempo el modelo de ejecución de posición neta de StockSharp.

## Características clave

- **Filtro de impulso**: reemplaza el indicador personalizado `DayImpuls` promediando la distancia de apertura/cierre de la vela
normalizado por el paso del precio del instrumento.
- **Estructura en zigzag**: reconstruye los máximos y mínimos recientes para determinar si el mercado tiene una tendencia alcista o bajista.
- **Puerta de tiempo**: se permiten entradas solo cuando la vela actual se cierra durante los últimos 15 segundos de los minutos 14, 29, 44 o 59.
- **Controles de riesgo**: incluye stop-loss, take-profit, trailing stop (opcional) y un objetivo de ganancias medido para toda la cuenta.
en unidades monetarias. Los valores predeterminados reflejan las entradas MetaTrader (stop de 1000 puntos, toma de ganancias de 150 puntos, seguimiento deshabilitado).
- **Exposición neta**: StockSharp mantiene una única posición neta por valor, por lo que la cobertura multinivel y la escala de lotes
de la implementación MQL no se reproducen. En cambio, la estrategia se centra en un volumen de entrada única.

## Lógica de trading

### Preparación de señal

1. Suscríbase a velas definidas por `CandleType` (período de tiempo de 5 minutos de forma predeterminada).
2. Calcule CCI y el promedio de impulso utilizando el `ImpulsePeriod` compartido (21 barras por defecto).
3. Actualice la dirección del swing del ZigZag una vez que la desviación supere los `ZigZagDeviation` puntos y la profundidad/retroceso
se cumplen las restricciones.
4. Almacene los valores anteriores de los indicadores para replicar los valores "actuales" (`cci0`, `imp`) y "anteriores" (`cci1`, `nimp`)
buffers utilizados en el asesor experto.

### Reglas de entrada

Una configuración se evalúa sólo si no hay ninguna posición abierta actualmente, han pasado al menos 15 segundos desde la última salida y
`AllowEntryTime` devuelve `true` (fin del bloque de 15 minutos).

**Largo:**
- El último movimiento del ZigZag apunta hacia abajo (nuevo mínimo más bajo que el anterior).
- Cualquiera
  - El CCI actual aumenta en comparación con la barra anterior, el CCI anterior está por debajo de -50, el CCI actual permanece por debajo de -30,
el impulso se vuelve positivo y el impulso anterior era negativo; o
  - El CCI actual está por debajo de -200, el CCI anterior era aún más bajo, el impulso permanece por debajo de `ImpulseBuyThreshold`
y es más fuerte que el impulso anterior.

**Corto:**
- El último movimiento del ZigZag apunta hacia arriba (nuevo máximo más alto que el anterior).
- Cualquiera
  - el CCI actual disminuye en comparación con la barra anterior, el CCI anterior está por encima de 50, el CCI actual se mantiene por encima de 30,
el impulso se vuelve negativo y el impulso anterior era positivo; o
  - El CCI actual está por encima de 200, el CCI anterior era más alto, el impulso se mantiene por encima de `ImpulseSellThreshold`
y es más débil que el impulso anterior.

Si el valor del impulso anterior se encuentra entre `ImpulseSellThreshold` y `ImpulseBuyThreshold`, la señal se ignora.

### Gestión de salidas

- **Stop-loss**: se activa cuando el precio se mueve `StopLossPoints` más allá del precio de entrada (1000 puntos de forma predeterminada).
- **Take-profit** – cierra la posición después de viajar `TakeProfitPoints` (150 puntos).
- **Parada de seguimiento** – opcional; cuando está habilitado, se activa después de que el precio se mueve `TrailingStopPoints + TrailingStepPoints`
a favor de la posición y luego sigue el precio en `TrailingStopPoints`.
- **Objetivo de ganancias**: convierte el PnL abierto en la moneda de la cuenta usando `PriceStep` y `StepPrice` y
cierra la posición una vez que excede `ProfitTarget` (predeterminado 300).

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `BaseVolume` | Tamaño comercial para las entradas. | `0.1` |
| `StopLossPoints` | Distancia de parada en puntos. | `1000` |
| `TakeProfitPoints` | Distancia de toma de ganancias en puntos. | `150` |
| `TrailingStopPoints` | Distancia del trailing stop en puntos (0 desactiva el trailing). | `0` |
| `TrailingStepPoints` | Distancia adicional antes de que se active el seguimiento. | `0` |
| `ImpulsePeriod` | Período tanto para CCI como para cálculos de impulso. | `21` |
| `ZigZagDepth` | Barras mínimas entre nuevos swings en ZigZag. | `12` |
| `ZigZagDeviation` | Movimiento de precio mínimo (en puntos) para confirmar una oscilación. | `5` |
| `ZigZagBackstep` | Barras mínimas antes de aceptar un nuevo swing. | `3` |
| `ProfitTarget` | Umbral de beneficio no realizado (moneda de la cuenta). | `300` |
| `ImpulseSellThreshold` | Valor de impulso mínimo requerido para pantalones cortos. | `-30` |
| `ImpulseBuyThreshold` | Valor de impulso máximo permitido para posiciones largas. | `30` |
| `CandleType` | Plazo de trabajo. | `5 minute time frame` |

## Diferencias vs. Asesor Experto Original

- La versión MetaTrader utiliza `GlobalVariableSet` para limitar las órdenes y almacenar recuentos de tickets para las cuadrículas de cobertura.
Este puerto conserva la aceleración basada en el tiempo, pero no la escalera de promedio/cobertura porque StockSharp cuenta
están netos.
- La gestión de órdenes se maneja a través de órdenes de mercado (`BuyMarket`, `SellMarket`) para mantenerse dentro de la guía de alto nivel API.
- El cálculo del impulso se simplifica; el `DayImpuls` original expone dos buffers (`imp`, `nimp`). Aquí ambos buffers
se aproximan a las lecturas del promedio móvil actual y anterior.

## Consejos de uso

- Configure `CandleType` para que coincida con el período de tiempo utilizado durante la optimización (el EA original funciona en M5).
- Asegúrese de que el instrumento proporcione metadatos `PriceStep` y `StepPrice` para convertir distancias de puntos correctamente.
- Realice una prueba retrospectiva con deslizamiento/latencia realista para confirmar que la puerta de entrada (últimos segundos antes del cuarto de hora) se comporta como se esperaba.

## Descargo de responsabilidad

Esta estrategia se proporciona con fines educativos. Pruebe minuciosamente con datos históricos y futuros antes
arriesgar capital real.
