# Estrategia TRAYLERv
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia TRAYLERv** es una conversión directa del MetaTrader 4 asesores expertos *TRAYLERv*. El código original actuaba como un administrador comercial automatizado en lugar de un generador de señales; ajustó continuamente las órdenes de protección para las posiciones existentes utilizando fractales de Bill Williams y permitió a los operadores limpiar las órdenes pendientes pendientes. Este puerto StockSharp conserva el mismo comportamiento al tiempo que aprovecha el API de alto nivel para la gestión de pedidos y las suscripciones de velas.

La estrategia **no** abre posiciones por sí sola. Espera que las operaciones se creen manualmente o mediante otra estrategia y luego asume el trabajo de mantener paradas y tomas de ganancias de acuerdo con la lógica siguiente. Todos los comentarios y nombres de configuración siguen el EA heredado para que los usuarios experimentados puedan mapear el comportamiento rápidamente.

## Lógica de trading
1. Suscríbase a la serie de velas configuradas (velas de un minuto por defecto) y registre cada barra terminada. Los máximos y mínimos fractales se detectan una vez que hay cinco velas disponibles, reproduciendo la definición fractal estándar MT4.
2. Cada vez que se cierra una nueva vela durante un minuto par, la estrategia verifica la posición neta actual:
   - **Posiciones largas**: busque el fractal descendente más reciente dentro de `StopFractalDepth` barras (predeterminado 7). Si lo encuentra, coloque o mueva un stop de venta por debajo del mínimo fractal menos el diferencial actual y un colchón de dos puntos. Si no existe ningún fractal válido, utilice el mínimo de la vela tres barras hacia atrás menos dos puntos. Cuando una posición larga sea rentable y la toma de ganancias esté habilitada, busque el último fractal alcista dentro de las barras `TakeProfitFractalDepth` (predeterminado 21) y coloque un límite de venta ligeramente por debajo de ese nivel para que coincida con la implementación de MetaTrader.
   - **Posiciones cortas**: refleja la lógica utilizando fractales ascendentes para el stop de compra dinámico y fractales bajistas para el objetivo de toma de ganancias. Se agregan buffers por encima de los máximos fractales para evitar paradas prematuras.
3. Cuando `DeleteAllPendingOrders` está habilitado, la estrategia cancela todas las órdenes pendientes activas que puede ver. Alternativamente, `DeleteOwnPendingOrders` elimina solo las órdenes pendientes que pertenecen al símbolo actual. Ambas opciones replican los interruptores de limpieza manual del EA original.
4. Si no hay ninguna posición abierta, todas las órdenes de protección registradas por la estrategia se cancelan para mantener ordenado el libro de órdenes.

## Gestión del riesgo
- Las órdenes de protección se crean con contrapartes de órdenes de mercado (`SellStop`, `BuyStop`, `SellLimit`, `BuyLimit`). El volumen de la orden de protección siempre coincide con el tamaño neto absoluto de la posición.
- Las paradas dinámicas y las tomas de ganancias son opcionales. Al deshabilitar el parámetro de toma de ganancias se elimina cualquier orden límite existente, pero se deja intacta la lógica de seguimiento.
- La información sobre diferenciales se toma del mejor par oferta/demanda cuando esté disponible. Si no se puede medir ningún diferencial, el código vuelve al incremento mínimo de precio del instrumento para evitar realizar órdenes directamente sobre el precio actual.
- Todos los niveles de precios están normalizados al tamaño del tick del instrumento para que las órdenes resultantes cumplan con los requisitos del intercambio.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `OrderVolume` | Volumen predeterminado sugerido para entradas manuales. Se conserva por compatibilidad con el EA original y no se utiliza internamente. | `0.1` |
| `DeleteAllPendingOrders` | Cuando `true`, cancele todas las órdenes pendientes activas en la conexión después de cada vela. | `false` |
| `DeleteOwnPendingOrders` | Cuando `true`, cancele solo las órdenes pendientes para el símbolo actual. | `false` |
| `UseTakeProfit` | Permite el cálculo de obtención de beneficios basado en fractales. Cuando está deshabilitada, se elimina cualquier orden de obtención de beneficios existente. | `true` |
| `EnableSound` | Bandera heredada conservada de MT4; proporcionado para que esté completo pero no utilizado en StockSharp. | `true` |
| `ShowCommentary` | Cambio heredado equivalente al comentario en el gráfico MT4. Está disponible para pantallas de configuración pero no tiene efecto en el puerto. | `true` |
| `StopFractalDepth` | Número de barras inspeccionadas para encontrar un fractal para el trailing stop. | `7` |
| `TakeProfitFractalDepth` | Número de barras inspeccionadas para encontrar un fractal para la toma de ganancias. | `21` |
| `CandleType` | Tipo de datos utilizado para la serie de velas principal. El valor predeterminado es un período de tiempo de 1 minuto. | `1 minute` período de tiempo |

## Notas de implementación
- La estrategia utiliza el flujo de trabajo de alto nivel `SubscribeCandles().Bind(...)` y procesa solo velas terminadas, reflejando el ciclo basado en ticks MT4 y evitando actualizaciones prematuras.
- La detección de fractales se implementa manualmente utilizando una lista continua de instantáneas de velas. Esto reproduce el comportamiento del indicador MT4 `iFractals` sin depender de indicadores StockSharp adicionales.
- Los precios de los pedidos se redondean al tick válido más cercano y los volúmenes respetan las restricciones `VolumeStep`, `MinVolume` y `MaxVolume` para garantizar la compatibilidad del intercambio.
- No se incluye ninguna traducción de Python. El directorio `PY` está ausente intencionalmente, ya que cumple con los requisitos de las pautas de conversión.
