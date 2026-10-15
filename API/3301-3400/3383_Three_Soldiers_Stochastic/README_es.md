# Estrategia de los Tres Soldados Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el MetaTrader experto `Expert_ABC_WS_Stoch.mq5`, que combina patrones clásicos de inversión de tres velas con la confirmación del oscilador Stochastic. Una señal larga requiere la formación alcista de los "Tres Soldados Blancos" junto con una línea de señal de sobreventa Stochastic, mientras que una señal corta depende de la formación bajista de los "Tres Cuervos Negros" confirmada por una sobrecompra Stochastic. La lógica de salida monitorea los cruces de la línea de señal a través de bandas configurables para cerrar posiciones.

## Lógica de trading

1. **Detección de patrones**
   - Realice un seguimiento de las últimas tres velas completadas.
   - Identifique *Tres Soldados Blancos* cuando las tres velas sean alcistas y cada cierre sea más alto que el anterior.
   - Identifique *Tres Cuervos Negros* cuando las tres velas sean bajistas y cada cierre sea más bajo que el anterior.
2. **Confirmación del oscilador**
   - Calcule un oscilador Stochastic con períodos `%K`, `%D` y `Slowing` idénticos al experto original (47, 9, 13 por defecto).
   - Utilice la línea de señal (`%D`) como confirmación:
     - Ingrese largo si el valor de la línea de señal anterior está por debajo del umbral de sobreventa (predeterminado `30`).
     - Ingrese corto si el valor de la línea de señal anterior está por encima del umbral de sobrecompra (predeterminado `70`).
3. **Condiciones de salida**
   - Cierre una operación larga cuando la línea de señal cruce por encima de los umbrales de salida inferior o superior (por defecto `20` y `80`).
   - Cierre una operación corta cuando la línea de señal vuelva a cruzar por debajo de estos umbrales.
   - Ambas comprobaciones de salida se basan en los valores de la línea de señal anterior y anterior para detectar cruces genuinos.

## Parámetros

| Nombre | Predeterminado | Descripción |
|------|---------|-------------|
| `CandleType` | `1h` período de tiempo | Plazo de suscripción de la vela. |
| `StochKPeriod` | `47` | Período retroactivo para `%K`. |
| `StochDPeriod` | `9` | Longitud media móvil de la línea de señal. |
| `StochSlowing` | `13` | Suavizado adicional aplicado a `%K`. |
| `OversoldLevel` | `30` | Nivel de línea de señal requerido para confirmar una entrada larga. |
| `OverboughtLevel` | `70` | Nivel de línea de señal requerido para confirmar una entrada corta. |
| `ExitLowerLevel` | `20` | Límite inferior utilizado para cruces de salida largos. |
| `ExitUpperLevel` | `80` | Límite superior utilizado para cruces de salida cortos. |

Todos los parámetros numéricos admiten rangos de optimización que coinciden con la plantilla MetaTrader, por lo que el comportamiento se puede ajustar a través del Diseñador de estrategias.

## Gestión de órdenes

- La estrategia invierte posiciones cuando aparece una señal opuesta sumando el tamaño absoluto de la posición actual al `Volume` configurado.
- `StartProtection()` está habilitado para integrarse con los controles de riesgo de la plataforma, aunque no se aplican niveles explícitos de limitación de pérdidas o toma de ganancias de forma predeterminada.

## Visualización

Cuando se ejecuta dentro del Diseñador de estrategias, la estrategia dibuja:

- Velas de precio para el símbolo y el período de tiempo seleccionados.
- El oscilador Stochastic configurado.
- Intercambie marcadores para resaltar entradas y salidas.

## Notas de uso

- Confirme que el instrumento proporcione suficiente historial para que el oscilador Stochastic se caliente antes de esperar señales.
- Considere combinar la estrategia con filtros de riesgo adicionales (volatilidad, filtros de sesión, etc.) cuando la implemente en vivo.
- Los umbrales se exponen como parámetros, lo que permite una experimentación rápida con diferentes bandas de confirmación sin editar código.
