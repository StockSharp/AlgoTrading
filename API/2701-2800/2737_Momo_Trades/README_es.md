# Estrategia Momo Trades
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Conversión del asesor experto original de MetaTrader "Momo_trades" que opera rupturas de momentum filtradas por una media móvil y la estructura del MACD.

## Lógica de la estrategia
- Trabaja con velas completadas del marco temporal configurado y procesa solo una posición neta a la vez.
- Usa una media móvil simple con un desplazamiento de barra configurable para medir cuánto cerró el precio alejado del promedio. Las operaciones largas requieren que el cierre desplazado esté por encima de la SMA en más del umbral de desplazamiento de precio; los cortos requieren lo contrario.
- Evalúa un patrón de momentum MACD en cascada que refleja las reglas MQL: varios valores pasados de la línea principal MACD deben aumentar a través de cero para largos o disminuir a través de cero para cortos. Esto evita operaciones mientras el momentum se debilita.
- Abre una orden de mercado con el volumen de la estrategia una vez que tanto el filtro de distancia SMA como el patrón MACD se alinean para la misma dirección.

## Gestión de riesgo
- El stop-loss, take-profit, trailing stop, paso de trailing, break-even y los inputs de desplazamiento de precio se definen en pips y se convierten automáticamente a unidades de precio usando el paso del instrumento.
- Cuando se proporcionan valores de take-profit y trailing, el stop solo se arrastra después de que el precio avance por la distancia de trailing más el paso de trailing, reproduciendo el comportamiento MQL.
- Cuando no hay take-profit configurado pero sí una distancia de break-even, el stop se mueve al precio de entrada una vez que se alcanza el disparador de break-even.
- Todos los niveles de stop y take se recalculan en cada vela completada y se cierran mediante órdenes de mercado cuando son cruzados por los extremos de la vela.

## Gestión de sesión
- La bandera `CloseEndDay` coincide con el asesor experto original y cierra cualquier posición activa a las 23:00 hora de la plataforma (21:00 los viernes). Después del corte, la estrategia omite nuevas entradas hasta el día siguiente.

## Parámetros
- **SMA Period / MA Bar Shift** – longitud de la media móvil y el índice de barra usado para obtener valores de SMA y precio.
- **MACD Fast / Slow / Signal / Bar Shift** – configuración de MACD y el desplazamiento aplicado a los valores almacenados para las verificaciones de patrón.
- **Stop Loss / Take Profit / Trailing Stop / Trailing Step / Breakeven / Price Shift** – distancias en pips que controlan la salida, el trailing y los filtros de SMA.
- **Close End Of Day** – cierra posiciones después del fin de sesión configurado.
- **Candle Type** – marco temporal usado para velas y cálculos de indicadores.
