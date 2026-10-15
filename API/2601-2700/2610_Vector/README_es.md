# Estrategia Vector
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia Vector es un sistema de seguimiento de tendencia multi-divisa convertido del experto "Vector" de MetaTrader 5. Opera cuatro pares forex principales — EURUSD, GBPUSD, USDCHF y USDJPY — simultáneamente. La estrategia calcula medias móviles suavizadas sobre el precio mediano de cada par y abre posiciones sincronizadas cuando la tendencia combinada apunta en la misma dirección. Un objetivo de pips dinámico basado en la volatilidad de cuatro horas y umbrales de ganancia y pérdida a nivel de cartera controlan las salidas.

## Ideas principales
- Usar medias móviles suavizadas (SMMA) construidas sobre precios medianos para medir la dirección en cada par de divisas.
- Resumir las medias rápidas y lentas de todos los instrumentos para determinar un sesgo alcista o bajista común.
- Ingresar una única orden de mercado por par cuando el sesgo global y el cruce rápido/lento local coinciden.
- Gestionar posiciones con un objetivo de pips flotante derivado del rango promedio de 50 velas de 4 horas completadas en EURUSD.
- Cerrar todas las operaciones simultáneamente si la ganancia o pérdida flotante alcanza el porcentaje configurado del saldo inicial.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| **Fast MA** | Longitud de la media móvil suavizada utilizada para la tendencia rápida en cada par. |
| **Slow MA** | Longitud de la media móvil suavizada utilizada para la tendencia lenta en cada par. |
| **MA Shift** | Número adicional de velas finalizadas requeridas antes de que se evalúen las señales, reflejando la configuración de desplazamiento en el EA original. |
| **Equity Take Profit %** | Porcentaje de ganancia flotante que activa el cierre de todas las posiciones abiertas. |
| **Equity Stop Loss %** | Porcentaje de pérdida flotante que activa una salida de emergencia para todas las operaciones. |
| **Signal Timeframe** | Marco temporal de velas usado para las medias móviles suavizadas (por defecto 15 minutos). |
| **Range Timeframe** | Marco temporal de velas usado para el promedio de volatilidad (por defecto 4 horas). |
| **Range Period** | Número de velas de marco temporal superior usadas para calcular el objetivo de pips promedio. |
| **EURUSD / GBPUSD / USDCHF / USDJPY** | Valores que corresponden a cada instrumento operado. |

Todos los parámetros soportan rangos de optimización idénticos al asesor experto original donde aplica.

## Lógica de trading
1. **Actualización de indicador** — Cada vela finalizada en un marco temporal de trading actualiza las medias móviles suavizadas rápida y lenta para el par correspondiente. Los valores solo se consideran después de que se completa el precalentamiento configurado (MA Shift).
2. **Cálculo de sesgo** — La estrategia suma las últimas medias rápidas de todos los pares y resta la suma de medias lentas. Un resultado positivo indica presión alcista, mientras que uno negativo indica presión bajista.
3. **Condiciones de entrada** — Cuando no existe posición para un par, la estrategia ingresa una orden de compra si el sesgo global es alcista y la media rápida del par está por encima de la lenta. Abre una orden de venta en el caso contrario.
4. **Salida por objetivo de pips** — La suscripción de cuatro horas de EURUSD calcula el rango de vela promedio sobre el período configurado. El objetivo de pips actual es el mayor entre este promedio y 13 pips. Los largos cierran una vez que el precio gana al menos el número objetivo de pips, y los cortos cierran después de un movimiento favorable equivalente.
5. **Protección de capital** — Siempre que la ganancia flotante supere el porcentaje de take-profit, o la pérdida flotante supere el porcentaje de stop-loss, la estrategia cierra inmediatamente todas las posiciones gestionadas.

## Notas de uso
- Adjunte la estrategia a una cartera que proporcione acceso a los cuatro instrumentos forex y establezca cada parámetro de seguridad explícitamente.
- El marco temporal de señal predeterminado es 15 minutos; asegúrese de que las velas coincidentes estén disponibles para cada par de divisas.
- Solo se mantiene una posición abierta por par en cualquier momento. El parámetro de volumen de la estrategia base se usa para cada entrada.
- Dado que las salidas dependen de la ganancia/pérdida flotante, la estrategia está pensada para operación continua en lugar de solo backtesting barra a barra.
- El objetivo de pips dinámico usa la volatilidad de EURUSD en línea con la implementación original. Ajuste el marco temporal de rango o el período si prefiere adaptar el objetivo a un entorno de mercado diferente.
