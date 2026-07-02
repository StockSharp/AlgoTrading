# Estrategia de ruptura MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el comportamiento de los expertos MetaTrader "M.A break mt5 buy" y "M.A break mt5 sell" al combinar ambas direcciones de ruptura en una única implementación StockSharp. Observa una serie de velas configurables, analiza varios promedios móviles exponenciales y confirma una vela de fuerte impulso antes de abrir operaciones. Las posiciones se gestionan mediante paradas protectoras fijas y objetivos medidos en pips.

## Lógica comercial

1. **Confirmación de tendencia.** Dos EMA pares (rápido versus lento) deben estar alineados en la dirección comercial en la vela completa. Para posiciones largas, ambos promedios rápidos deben estar por encima de sus contrapartes lentos; para los cortos las relaciones se invierten. La vela anterior abierta también debe estar en el lado correcto de un filtro EMA dedicado.
2. **Medición de rango silencioso.** Un número configurable de velas anteriores (excluyendo la vela de impulso más reciente) define el período "tranquilo". Su rango más alto se compara con un umbral mínimo de pips.
3. **Detección de impulsos.** La última vela terminada debe expandirse al menos `ImpulseStrength` veces el rango de silencio. Se pueden imponer límites de tamaño de velas en pips para ignorar movimientos inusualmente pequeños o grandes.
4. **Plantilla de vela.** La vela de impulso debe presentar una estructura de mecha específica:
   - Operaciones largas: cuerpo alcista, mecha superior que no excede `BullUpperWickPercent` del rango de la vela y mecha inferior al menos `BullLowerWickPercent` del rango.
   - Operaciones cortas: cuerpo bajista, mecha superior de al menos `BearUpperWickPercent` y mecha inferior no mayor que `BearLowerWickPercent` del rango.
5. **Condición de retroceso.** El impulso mínimo (para largos) o máximo (para cortos) debe probar un EMA adicional para garantizar que la ruptura surgió de un retroceso.
6. **Control de posición.** Sólo se permite una posición neta. La estrategia cierra el lado opuesto antes de entrar en una nueva operación y nunca abre una posición contra el filtro de tendencia.
7. **Gestión de salida.** Los niveles de stop-loss y take-profit se calculan en pips a partir del precio de entrada. Cada vela terminada comprueba si el precio ha alcanzado los niveles de protección y sale en consecuencia.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| **Tipo de vela** | Serie de velas primarias utilizada para todos los cálculos. |
| **MA rápida 1 / MA lenta 1** | Períodos del primer par EMA que define la tendencia principal. |
| **MA 2 rápido/MA 2 lento** | Períodos del par secundario EMA utilizados como filtro de tendencia adicional. |
| **Abrir filtro MA** | EMA período que filtra el precio de apertura de la vela anterior. |
| **Retroceso MA** | EMA período cuyo valor debe ser tocado por la mecha de impulso. |
| **Bares tranquilos** | Número de velas históricas utilizadas para medir el rango tranquilo del mercado. |
| **Rango silencioso (pips)** | Se requiere un rango mínimo de pips en las velas tranquilas antes de considerar una ruptura. |
| **Multiplicador de impulso** | Relación mínima entre el tamaño de la vela de impulso y el rango de silencio. |
| **Tamaño mínimo/máximo de vela (pips)** | Límites opcionales para el rango de velas de impulso. Zero desactiva el límite respectivo. |
| ** % de mecha superior de toro / % de mecha inferior de toro ** | Filtros de forma para la vela de impulso alcista, expresados como porcentajes del rango de la vela. |
| **Oso % de mecha superior/Oso % de mecha inferior** | Filtros de forma para la vela de impulso bajista. |
| **Volumen** | Tamaño del pedido en lotes utilizados tanto para entradas largas como cortas. |
| **Stop-Loss (pips)** | Distancia al tope de protección medida a partir del precio de entrada. Cero desactiva la parada. |
| **Take-Profit (pips)** | Distancia al objetivo de ganancias. Cero desactiva el objetivo. |
| **Habilitar largo/Habilitar corto** | Alternar operaciones de ruptura en cada dirección de forma independiente. |

## Notas de uso

- Configure la serie de velas para que coincida con el período de tiempo utilizado por el experto original (por ejemplo, M5 o H1). El valor predeterminado es un período de tiempo de 5 minutos.
- La estrategia almacena sólo el historial reciente necesario para el cálculo del rango silencioso, evitando el uso innecesario de memoria.
- Los precios de entrada se aproximan mediante el cierre de la vela de impulso, que coincide con el comportamiento original MetaTrader de colocar órdenes de mercado al comienzo de la siguiente barra.
- Los niveles de stop-loss y take-profit se evalúan en cada vela completa. Si se alcanzan ambos niveles dentro de la misma barra, la parada tiene prioridad, reflejando el manejo conservador utilizado por los expertos en fuentes.
- Habilitar solo una dirección reproduce los asesores expertos originales de "compra" o "venta", mientras que dejar ambas opciones activas permite operaciones de ruptura simétricas.

## Detalles de conversión

- Ambos archivos MQ5 originales fueron codificados en UTF-16 y creados a partir de bloques generados por el motor FXD. Cada bloque se ha traducido a lógica C# explícita.
- Las comparaciones y plantillas de velas EMA siguen los mismos cambios que la versión MetaTrader (`Shift = 1`), lo que significa que la estrategia siempre evalúa velas completamente cerradas.
- La lógica de parada virtual y las etiquetas de los gráficos de los scripts MQ5 se omitieron intencionalmente porque no influyen en la colocación de pedidos.

## Pruebas

Compile la solución a través de `AlgoTrading.sln` o ejecute la estrategia dentro del probador de estrategias StockSharp. Ajustar el escalón del precio del instrumento si los metadatos de seguridad carecen de esta información; la implementación recurre a `0.0001` para emular valores de pips de FX comunes.
