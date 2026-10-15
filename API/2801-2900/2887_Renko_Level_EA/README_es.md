# Estrategia Renko Level EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Convertida del asesor experto de MetaTrader **Renko Level EA.mq5**.
- Emula el indicador original manteniendo un nivel Renko superior e inferior derivado del parámetro `BrickSize`.
- Evalúa velas finalizadas proporcionadas por `CandleType` (predeterminado: marco temporal de 1 minuto) y reacciona cuando la cuadrícula Renko se desplaza.
- No usa stops ni objetivos fijos; cada salida ocurre a través de una señal opuesta.

## Lógica de trading
1. En la primera vela finalizada el precio de cierre se redondea a la cuadrícula Renko para inicializar los niveles superior e inferior.
2. Para cada vela subsiguiente:
   - Si el cierre permanece entre los límites actuales, la cuadrícula permanece sin cambios.
   - Un cierre por encima del nivel superior eleva el bloque Renko hacia arriba al siguiente valor de cuadrícula.
   - Un cierre por debajo del nivel inferior empuja el bloque hacia abajo.
3. Un cambio en el nivel Renko superior se interpreta como un rompimiento direccional.
   - Nivel superior creciente → señal alcista (a menos que `ReverseSignals` esté habilitado).
   - Nivel superior decreciente → señal bajista.
4. Las señales pueden opcionalmente invertirse (`ReverseSignals`) o piramidarse (`AllowIncrease`) para coincidir con el comportamiento del EA original.

## Gestión de órdenes
- Antes de entrar largo, cualquier posición corta se cierra; lo opuesto ocurre antes de entrar corto.
- Cuando `AllowIncrease = false`, la estrategia abre un nuevo trade solo si no existe ninguna posición en esa dirección.
- Cuando `AllowIncrease = true`, se permiten órdenes adicionales de tamaño `OrderVolume` incluso si una posición ya está abierta.
- No hay stop-loss ni take-profit dedicados; los reversales de posición sirven como mecanismo de salida.
- `StartProtection()` se invoca una vez para mantener las salvaguardas de riesgo alineadas con el framework base.

## Parámetros
| Nombre | Descripción | Predeterminado | Optimizable |
| --- | --- | --- | --- |
| `BrickSize` | Tamaño del bloque Renko medido como múltiplos de `Security.PriceStep`. Define cuánto debe moverse el precio para desplazar la cuadrícula. | `30` | Sí (10 → 100 paso 10) |
| `OrderVolume` | Volumen enviado con cada orden de mercado. | `1` | No |
| `ReverseSignals` | Invierte las acciones alcistas y bajistas. Refleja la entrada *Reverse* del EA. | `false` | No |
| `AllowIncrease` | Permite añadir a una posición existente en lugar de esperar una posición plana. Refleja el indicador *Increase* del EA. | `false` | No |
| `CandleType` | Fuente de velas usada para los cálculos. Predeterminado a velas de marco temporal de 1 minuto, pero se puede suministrar cualquier serie soportada. | `TimeFrameCandleMessage(1m)` | No |

## Notas prácticas
- `BrickSize` se adapta automáticamente al instrumento negociado porque multiplica el `PriceStep` definido por la bolsa.
- La decisión se basa puramente en precios de cierre; los movimientos intrabarra importan solo cuando forman el cierre final.
- Combinar `ReverseSignals` y `AllowIncrease` permite probar variantes contratendencia y de piramidación del EA.
- Funciona en cualquier mercado donde la lógica de rompimiento estilo Renko es relevante, incluyendo forex, futuros e instrumentos cripto.

## Clasificación
- **Régimen**: Seguimiento de tendencia (rompimiento Renko).
- **Dirección**: Largo/Corto.
- **Complejidad**: Moderado (seguimiento de niveles personalizado, ajuste mínimo).
- **Stops**: Ninguno; salidas en señales inversas.
- **Marco temporal**: Configurable mediante `CandleType`.
- **Indicadores**: Proyección de nivel Renko personalizada.
