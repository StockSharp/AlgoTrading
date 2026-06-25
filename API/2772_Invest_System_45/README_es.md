# Estrategia Invest System 4.5 (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Invest System 4.5 es un asesor experto de MetaTrader 5 que ha sido portado a la API de estrategia de alto nivel de StockSharp. La estrategia opera el par EUR/USD siguiendo la dirección de la vela de 4 horas completada anterior. Se permite una sola operación durante los primeros minutos de la nueva sesión de 4 horas y el dimensionamiento de posición se adapta al rendimiento realizado y al crecimiento de la cuenta.

El código se basa exclusivamente en la API de alto nivel: se utilizan suscripciones automáticas de velas para monitorear tanto el sesgo direccional de 4 horas como la ventana de entrada de marco temporal inferior, mientras que el helper `StartProtection` integrado aplica niveles estáticos de take-profit y stop-loss expresados en pips.

## Lógica de trading
1. **Sesgo direccional** – al cierre de cada vela de 4 horas terminada, la estrategia almacena si la vela cerró alcista o bajista. Una vela alcista habilita solo entradas largas para la próxima sesión, mientras que una vela bajista habilita solo cortos. Si la vela cierra exactamente en su apertura, se mantiene la dirección anterior.
2. **Timing de entrada** – cuando comienza una nueva vela de 4 horas, se abre una ventana de entrada. La ventana permanece válida por un número configurable de minutos (15 por defecto). La estrategia observa velas de marco temporal inferior (1 minuto por defecto) y puede enviar como máximo una orden de mercado si se satisfacen todos los filtros mientras la ventana está activa.
3. **Posición única** – la estrategia nunca pirámide. Si ya hay una posición abierta, no se procesan nuevas señales hasta la próxima sesión de 4 horas. Una vez enviada una orden, la ventana de entrada se cierra inmediatamente para replicar el comportamiento de MetaTrader.
4. **Seguimiento de ganancias y pérdidas** – cuando una posición se cierra completamente, se captura el PnL realizado para impulsar la lógica adaptativa de lotes descrita a continuación.

## Reglas de dimensionamiento de posición
El asesor experto original usa dos capas de gestión de dinero:
- **Hitos de capital**: el saldo inicial de la cuenta se almacena en la primera actualización. Cuando el capital supera 2×, 3× … 6× el saldo inicial, el tamaño del lote base aumenta proporcionalmente. La Etapa 1 comienza en `BaseLot`, la etapa 2 lo duplica, la etapa 3 lo triplica, y así sucesivamente. Los tamaños de lote secundarios (`Lot2`, `Lot3`, `Lot4`) se derivan usando los multiplicadores originales (×2, ×7 y ×14 respectivamente).
- **Escalada Plan B**: se mantiene un único valor de volumen global entre operaciones.
  - Después de una operación perdedora con el lote base, el volumen se eleva al segundo lote (`Lot3`).
  - Si ocurre otra pérdida mientras se opera con el segundo lote, se activa el "Plan B". El Plan B reasigna las opciones de lote internas de modo que el lote base se convierte en `Lot2` y el lote agresivo en `Lot4`. El volumen actual no cambia inmediatamente, pero cualquier pérdida posterior empuja la estrategia al lote agresivo. El Plan B se cancela automáticamente cuando la cuenta alcanza un nuevo máximo de capital.
  - Una operación rentable siempre restablece el volumen actual al lote base para la etapa activa.
Estas reglas reproducen fielmente la escalada de lotes en cascada de la versión MetaTrader sin iterar manualmente a través de órdenes o usar colecciones.

## Gestión de riesgos
- `StartProtection` configura tanto el stop-loss como el take-profit en unidades de precio absoluto derivadas del tamaño del pip. Los stops y objetivos se registran solo una vez cuando se inicia la estrategia, tal como el EA original adjunta los valores a cada orden.
- Solo se usan órdenes de mercado. La propia estrategia no realiza posiciones de cobertura, escalado ni salidas parciales; las salidas ocurren a través de las órdenes de protección configuradas.

## Parámetros de la estrategia
| Parámetro | Descripción | Por defecto | Rango de optimización |
|-----------|-------------|-------------|----------------------|
| `StopLossPips` | Distancia del stop-loss en pips. Use `0` para deshabilitar el stop. | 240 | 120 – 360, paso 20 |
| `TakeProfitPips` | Distancia del take-profit en pips. Use `0` para deshabilitar el objetivo. | 40 | 20 – 80, paso 10 |
| `EntryWindowMinutes` | Duración de la ventana de entrada después de que se abre cada nueva vela de 4 horas. | 15 | 5 – 30, paso 5 |
| `SignalCandleType` | Serie de velas usada para monitorear la ventana de entrada (1 minuto por defecto). | Marco temporal de 1 minuto | – |
| `TrendCandleType` | Vela de marco temporal superior usada para construir el sesgo direccional (4 horas por defecto). | Marco temporal de 4 horas | – |
| `BaseLot` | Tamaño de lote inicial para la etapa 1. Los demás tamaños de lote se derivan automáticamente. | 0.1 | 0.05 – 0.3, paso 0.05 |

## Estructura de archivos
```
2772_Invest_System_45/
├── CS/
│   └── InvestSystem45Strategy.cs
├── README.md
├── README_ru.md
└── README_zh.md
```

## Notas
- La estrategia espera que el instrumento adjunto proporcione tanto la serie de velas de 4 horas como la serie de marco temporal más rápido. Estas suscripciones se crean automáticamente dentro de `OnStarted`.
- El tamaño del pip se determina a partir de `Security.PriceStep` y se ajusta para cotizaciones fraccionarias (3 o 5 decimales) para coincidir con el tratamiento de MetaTrader de los valores de pip.
- Debido a que el robot original usa umbrales de saldo de cuenta, la implementación de StockSharp lee `Portfolio.CurrentValue` en cada actualización de vela de entrada. Al ejecutar en simulación, asegúrese de que el modelo de portafolio actualice el capital actual para que el escalado de lotes permanezca consistente.
- La traducción a Python se omite intencionalmente según lo solicitado.
