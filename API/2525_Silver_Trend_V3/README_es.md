# Estrategia SilverTrend V3 (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia SilverTrend V3 es un sistema de seguimiento de momentum que se origina en el asesor experto de MetaTrader 5 "SilverTrend v3". El port a StockSharp reproduce la lógica original adaptándola a la API de estrategias de alto nivel. La idea central es detectar momentum alcista o bajista mediante el cálculo del canal SilverTrend, confirmarlo con el oscilador de perfil de mercado J_TPO y gestionar las posiciones resultantes con stops protectores, lógica de trailing y un filtro de sesión del viernes.

## Motor de señales

1. **Dirección SilverTrend**
   - Utiliza una ventana deslizante de 350 barras con un parámetro de suavizado de 9 barras para calcular soporte dinámico (`smin`) y resistencia (`smax`).
   - Cuando el cierre actual cae por debajo de `smin`, el sistema marca un régimen bajista; un cierre por encima de `smax` cambia el régimen a alcista.
   - El cálculo itera desde la barra más antigua a la más reciente para replicar la naturaleza recursiva del código MQL original.

2. **Confirmación J_TPO**
   - Implementa el oscilador J_TPO original de 14 periodos que mide cómo se agrupan los precios dentro de una distribución de corto plazo.
   - Solo permite entradas largas cuando el oscilador es positivo y entradas cortas cuando es negativo, filtrando los cambios de momentum débiles.

3. **Detección de cambio de señal**
   - Una operación se inicia solo cuando la dirección SilverTrend recién calculada difiere del valor anterior, asegurando que la estrategia reaccione a cambios de régimen genuinos en lugar de ruido.

## Gestión de operaciones

- **Entradas de mercado** – La estrategia opera con el `Volume` configurado. Si hay una posición contraria abierta, se cierra y revierte en una sola orden de mercado.
- **Stop loss inicial** – Opcional. Definido en pasos de precio relativos al precio de entrada (convertidos con el `PriceStep` del instrumento).
- **Take profit** – Opcional. También definido en pasos de precio y evaluado contra los extremos de la vela para simular el comportamiento original de modificación de órdenes.
- **Trailing stop** – Se activa una vez que el precio se mueve a favor la distancia de trailing configurada. Para posiciones largas el stop sube gradualmente, para cortas baja, coincidiendo con la lógica de MetaTrader.
- **Salida por señal opuesta** – Cuando el régimen anterior apunta en dirección contraria, cualquier posición existente se liquida al cierre de la siguiente vela.
- **Bloqueo de operaciones del viernes** – Las nuevas posiciones se omiten después de la hora especificada los viernes para evitar gaps de fin de semana, exactamente como en el EA fuente.

## Parámetros

| Nombre | Valor predeterminado | Descripción |
| --- | --- | --- |
| `TrailingStopPoints` | 50 | Distancia del trailing stop medida en pasos de precio. Poner en cero para deshabilitar el trailing. |
| `TakeProfitPoints` | 50 | Distancia del take profit en pasos de precio. Cero deshabilita el objetivo. |
| `InitialStopLossPoints` | 0 | Stop protector inicial en pasos de precio. Cero deja la posición sin stop inicial. |
| `FridayCutoffHour` | 16 | Hora de bolsa a partir de la cual no se permiten nuevas entradas el viernes. Usar `0` para permitir operar todo el día. |
| `CandleType` | Velas de 1 hora | Serie de datos que alimenta los indicadores. Se puede usar cualquier temporalidad soportada. |
| `Volume` | 1 lote | Tamaño de operación para cada posición (propiedad `Volume` de StockSharp). |

Todas las distancias se multiplican por `PriceStep` en tiempo de ejecución, lo que adapta automáticamente la estrategia al tamaño del tick del instrumento (incluyendo símbolos forex de 3/5 dígitos).

## Requisitos de datos y entorno

- Requiere al menos 360 velas completadas antes de producir señales en vivo para que los buffers de SilverTrend y J_TPO estén completamente formados.
- Diseñado para operar con un único instrumento a través de `SubscribeCandles`. El override `GetWorkingSecurities` asegura que la estrategia se suscriba solo al instrumento y temporalidad configurados.
- Usa `StartProtection()` para activar el servicio estándar de protección de posiciones de StockSharp una vez al inicio.

## Notas de uso

- El algoritmo espera instrumentos con tendencia como los pares forex principales o futuros líquidos; adaptar el marco temporal a la volatilidad del mercado.
- Debido a que el cálculo de SilverTrend es recursivo, reiniciar la estrategia con velas históricas insuficientes retrasará la formación de señales hasta que se recopilen suficientes datos.
- La implementación de la API de alto nivel utiliza los extremos de las velas para simular la gestión de órdenes (stop loss, take profit, trailing). En operativa en vivo considerar emparejar la lógica con órdenes stop/límite reales si la infraestructura lo requiere.
- El port almacena el estado interno (`_previousSignal`, `_entryPrice`, stops de trailing) exactamente una vez por vela terminada, coincidiendo con el comportamiento "una operación por barra" del EA original.

## Detalles de conversión

- Reproduce fielmente las rutinas matemáticas de `SilverTrend v3.mq5`, incluyendo el algoritmo J_TPO de arrays anidados.
- Aplica buenas prácticas de C#: los parámetros se exponen vía `StrategyParam<T>`, todos los comentarios están en inglés, y la indentación usa tabulaciones según las directrices del repositorio.
- No se incluye versión en Python en esta versión según los requisitos de la tarea.
