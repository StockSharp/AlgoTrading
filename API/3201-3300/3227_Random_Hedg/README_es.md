# Estrategia de Random Hedg
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Random Hedg** es un puerto de alto nivel de StockSharp del experto asesor MetaTrader "Random Hedg". El EA original abre simultáneamente una orden de compra de mercado y una de venta de mercado, luego gestiona ambas posiciones con una combinación de stop loss fijo, take profit, punto de equilibrio y lógica de trailing. La conversión mantiene ese comportamiento central mientras expone cada configuración como parámetro de estrategia para que el bot pueda ajustarse u optimizarse directamente dentro de StockSharp Designer.

## Lógica de trading
1. **Cobertura inicial** – cuando la estrategia está sin posición, envía de inmediato dos órdenes de mercado (compra y venta) usando el mismo volumen configurable. Ambas posiciones reciben un stop loss y un take profit expresados en pips.
2. **Protección de punto de equilibrio** – tras un movimiento de precio favorable por la cantidad configurada de pips, el nivel de stop se desplaza al punto de equilibrio más un desplazamiento opcional (posiciones largas) o menos el desplazamiento (posiciones cortas). Esto imita el interruptor "mover a sin pérdida" del EA.
3. **Trailing stop** – una vez que el beneficio supera la distancia de trailing, el stop sigue al precio. Para posiciones largas, el stop sigue el precio más alto menos la distancia de trailing; para cortas, sigue el precio más bajo más la distancia.
4. **Salidas protectoras** – cada posición se cierra cuando se toca su take profit o stop loss. Opcionalmente, la estrategia puede liquidar ambas posiciones si una vela cierra por debajo de la Banda de Bollinger inferior, recreando el filtro de salida del código original.
5. **Reinicio de ciclo** – una vez que ambas posiciones están cerradas, la estrategia restablece sus rastreadores internos y espera la siguiente vela para abrir un nuevo par cubierto.

## Parámetros
- `HedgeVolume` – volumen utilizado para abrir ambas posiciones de cobertura (por defecto 0.1 contratos).
- `StopLossPips` – distancia del stop loss protector (por defecto 200 pips).
- `TakeProfitPips` – distancia del take profit (por defecto 200 pips).
- `TrailingStopPips` – paso de trailing aplicado cuando una posición se vuelve rentable (por defecto 40 pips).
- `BreakEvenTriggerPips` – beneficio requerido antes de mover el stop al punto de equilibrio (por defecto 10 pips).
- `BreakEvenOffsetPips` – beneficio adicional asegurado cuando ocurre el desplazamiento al punto de equilibrio (por defecto 5 pips).
- `EnableTrailing` – activa o desactiva la gestión del trailing stop.
- `EnableBreakEven` – activa o desactiva la función de punto de equilibrio.
- `EnableExitStrategy` – activa el filtro de liquidación basado en Bandas de Bollinger.
- `BollingerPeriod` – período de las Bandas de Bollinger usadas para la salida opcional (por defecto 20 velas).
- `BollingerWidth` – multiplicador de amplitud de las Bandas de Bollinger (por defecto 2).
- `CandleType` – serie de datos de velas usada para ejecutar la lógica (por defecto marco temporal de 30 minutos).

## Notas de implementación
- La conversión usa la API de alto nivel `Strategy` con suscripciones a velas y el mecanismo `BindEx` para calcular las Bandas de Bollinger al vuelo.
- El estado interno rastrea el precio de entrada, volumen y niveles protectores dinámicos para cada posición. Esto permite que la versión C# imite los asistentes de gestión monetaria del EA original sin depender de manejadores de órdenes específicos de la plataforma.
- Los volúmenes de órdenes pendientes se rastrean por separado para que las ejecuciones puedan clasificarse como entradas o salidas incluso cuando las operaciones de compra y venta ocurren consecutivamente.
- La estrategia espera una cuenta con capacidad de cobertura porque mantiene exposición larga y corta al mismo tiempo, igual que el experto asesor fuente.
- Las funciones de trailing basado en dinero y take profit por porcentaje del código MQL se omiten intencionalmente. Dependen de datos de balance específicos del broker y rara vez se usaban en la práctica; la versión de StockSharp se centra en la gestión principal de acción del precio.

## Archivos
- `CS/RandomHedgStrategy.cs` – implementación principal en C# con comentarios en línea detallados en inglés.
- `README.md` – esta documentación (inglés).
- `README_ru.md` – traducción al ruso.
- `README_zh.md` – traducción al chino simplificado.
