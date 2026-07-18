# Estrategia Doji Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia replica la lógica central del clásico asesor experto **Doji Trader**.
Monitorea las velas completadas en busca de patrones doji de cuerpo compacto y espera un
cierre de ruptura más allá del rango del doji para entrar al mercado en la dirección de la ruptura.

## Lógica de trading

1. Solo se procesan las velas terminadas. El marco temporal predeterminado es 1 hora, pero puede
   ajustarse mediante el parámetro `CandleType`.
2. El trading solo está permitido cuando el tiempo de cierre de la última vela cae dentro de
   la ventana de sesión configurable `[StartHour, EndHour)` medida en tiempo del exchange.
3. El algoritmo mantiene en memoria las tres velas terminadas más recientes. La vela que
   acaba de cerrar se compara con las dos velas que la precedieron (`-2` y `-3`).
4. Una vela cuenta como doji cuando la diferencia absoluta entre su apertura y cierre es
   menor que `MaximumDojiHeight * pip`, donde el valor del pip se deriva del paso de precio
   del instrumento (las cotizaciones de 3 o 5 dígitos se escalan automáticamente ×10).
5. Si la vela más nueva cierra **por encima** del máximo del doji calificado más reciente, la
   estrategia abre (o cambia a) una posición larga. Si cierra **por debajo** del mínimo del doji,
   abre una posición corta. No se coloca ninguna operación cuando el precio permanece dentro del rango del doji.
6. El tamaño de la posición se toma de la propiedad `Volume` de la estrategia. Cuando aparece una señal de reversión,
   el algoritmo envía suficiente volumen para cerrar la posición anterior y establecer
   la exposición deseada en la nueva dirección, de modo que solo quede una posición neta abierta.

## Gestión de riesgo

- Las distancias de stop-loss y take-profit se configuran en pips mediante `StopLossPips` y
  `TakeProfitPips`. Establecer un valor en cero deshabilita la orden de protección correspondiente.
- `StartProtection` se lanza una vez al inicio y usa órdenes de mercado para las salidas, de modo que el
  comportamiento refleja la implementación MQL que cerró y reabrió posiciones directamente.

## Parámetros

| Nombre | Descripción | Por defecto |
| --- | --- | --- |
| `CandleType` | Marco temporal de las velas procesadas. | Marco temporal de 1 hora |
| `StartHour` | Hora de apertura inclusiva de la ventana de trading. | 8 |
| `EndHour` | Hora de cierre exclusiva de la ventana de trading. | 17 |
| `MaximumDojiHeight` | Altura máxima del cuerpo (en pips) para que una vela sea tratada como doji. | 1 |
| `StopLossPips` | Distancia del stop de protección en pips. | 50 |
| `TakeProfitPips` | Distancia del objetivo de beneficio en pips. | 50 |

### Notas adicionales

- La estrategia asume que la cuenta de la plataforma usa posiciones netas. Si su feed proporciona
  pasos de pip fraccionarios (cotizaciones de 5 o 3 dígitos), el valor del pip se multiplica por 10 para
  coincidir con las mediciones tradicionales de pip.
- Establezca el tamaño de lote deseado en la propiedad `Volume` antes de ejecutar la estrategia.
- No se requieren indicadores adicionales; la lógica depende únicamente de los datos de velas en bruto.
- Todavía no hay un port de Python, solo la implementación en C#.
