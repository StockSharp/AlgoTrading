# Estrategia de Apertura de Tiempo para Dos Sesiones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Apertura de Tiempo para Dos Sesiones** automatiza un plan de trading programado por tiempo que puede gestionar dos sesiones independientes durante el día de trading. Cada sesión puede configurarse con su propia dirección, parámetros de riesgo y ventana opcional de cierre forzado. La conversión sigue la lógica original de MetaTrader pero se apoya en las API de alto nivel de StockSharp, velas y objetos de parámetros para configuración y optimización.

## Lógica de trading
1. **Ventanas de cierre de sesión.** Para cada intervalo se puede definir una ventana de cierre opcional. Cuando el tiempo de la vela cae dentro de la ventana (tiempo de inicio más la duración global), la estrategia cierra forzosamente el intervalo correspondiente y limpia su estado.
2. **Mantenimiento del trailing stop.** Si el trailing stop y el paso son positivos, la lógica de trailing monitorea las velas completadas. Una vez que el precio se mueve a favor de la posición al menos `(TrailingStop + TrailingStep)`, el stop avanza en `TrailingStop`. Las actualizaciones requieren la distancia del paso para evitar recálculos ruidosos.
3. **Verificaciones de stop loss y take profit.** Cada intervalo tiene distancias independientes de stop loss y take profit medidas en pips. En cada vela completada, los precios altos/bajos se comparan con estos niveles, cerrando el intervalo inmediatamente cuando se supera un nivel.
4. **Filtro por día de semana.** El trading procede solo en los días de semana habilitados. Si la vela actual pertenece a un día deshabilitado, no se abren nuevas operaciones.
5. **Ventanas de apertura.** Cada intervalo tiene una ventana de apertura con tiempos de inicio y fin. El valor de duración global extiende la ventana por el lado de finalización. Cuando una ventana está activa y el intervalo no tiene posición abierta, la estrategia abre una orden de mercado en la dirección configurada.
6. **Sincronización de posición.** Los intervalos activos contribuyen a una posición neta objetivo. La estrategia llama a `BuyMarket` o `SellMarket` para que la posición neta coincida con la suma de exposiciones de intervalos. Cada intervalo mantiene su propio precio de entrada, niveles de stop/take y estado de trailing stop.

## Referencia de parámetros
- **Close Window #1 / Close Window #2** – habilitar o deshabilitar las ventanas de cierre forzado dedicadas para cada intervalo.
- **Close Start #1 / Close Start #2** – hora local del día en que comienza la ventana de cierre para cada intervalo.
- **Trailing Stop / Trailing Step** – distancias en pips usadas por la lógica de trailing. Ambas deben ser mayores que cero para activar el trailing.
- **Trade Monday … Trade Friday** – filtros por día de semana. Al menos un día debe permanecer habilitado para permitir el trading.
- **Open Start #1 / Open End #1 / Open Start #2 / Open End #2** – límites de ventana de apertura para cada intervalo. La duración global extiende la ventana más allá del tiempo de fin.
- **Window Duration** – intervalo de tiempo extra añadido a las ventanas de apertura y cierre.
- **Direction #1 / Direction #2** – indicadores de dirección de trade (`true` para largo, `false` para corto) para cada intervalo.
- **Trade Volume** – volumen de orden de mercado para cada intervalo. La estrategia asume volumen idéntico para ambos intervalos como en el asesor experto original.
- **Stop Loss #1 / Take Profit #1 / Stop Loss #2 / Take Profit #2** – distancias en pips para los niveles de stop loss y take profit por intervalo. Un valor de cero deshabilita el nivel correspondiente.
- **Candle Type** – serie de velas utilizada para impulsar la estrategia. Todos los cálculos, incluidas ventanas de tiempo y verificaciones de riesgo, se ejecutan cuando estas velas finalizan.

## Detalles de gestión de riesgo
- Las distancias en pips se convierten a unidades de precio usando el paso de precio del instrumento. Si el instrumento usa tres o cinco decimales, el paso se multiplica por diez para replicar la definición de pip de MetaTrader.
- La lógica de trailing es compartida por ambos intervalos, mientras que los valores de stop loss y take profit permanecen independientes.
- Cuando se activa el nivel de stop o trailing, el intervalo restablece su estado para poder reabrir dentro de la misma ventana si el tiempo lo permite.

## Limitaciones y notas
- StockSharp opera con un modelo de posición neta. Si el intervalo #1 y #2 están configurados con direcciones opuestas, la posición neta resultante se aplanará en lugar de mantener dos operaciones cubiertas abiertas simultáneamente. Use un portafolio con capacidad de cobertura si se requiere cobertura real.
- Las decisiones se basan en la serie de velas seleccionada. Usar un marco temporal grande puede retrasar las reacciones comparado con la implementación basada en ticks de MetaTrader.
- La estrategia espera que los relojes del exchange y del terminal estén sincronizados porque las comparaciones de hora del día se basan en hora local.

## Consejos de uso
- Configure el tipo de vela para que coincida con la granularidad temporal usada para el horario (p. ej., un minuto para control granular).
- Combine el filtro de día y las ventanas de cierre para evitar llevar posiciones durante sesiones indeseables.
- Optimice los parámetros a través de los objetos `StrategyParam` integrados; los campos clave ya tienen `SetCanOptimize` habilitado.
