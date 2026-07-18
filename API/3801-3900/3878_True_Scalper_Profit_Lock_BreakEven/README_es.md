# Estrategia de equilibrio de bloqueo de beneficios True Scalper
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia True Scalper Profit Lock es una conversión del asesor experto MetaTrader 4 **TrueScalperProfitLock.mq4**. Combina un cruce de media móvil exponencial de corto plazo con filtros de polaridad basados ​​en RSI para entradas de tiempo. La estrategia está diseñada para entornos de especulación de alta frecuencia donde las posiciones se gestionan activamente mediante un tope protector, un nivel fijo de obtención de beneficios y un bloqueo de equilibrio opcional.

## Lógica de trading

- **Filtro de tendencia:** Un EMA de 3 períodos calculado sobre la vela cerrada anterior debe negociarse por encima (para compras) o por debajo (para ventas) de un EMA de 7 períodos de la misma barra. La distancia entre los promedios debe exceder un escalón de precio para evitar condiciones de mercado planas.
- **RSI confirmación:** El EA original ofrece dos modos de validación. El método A espera a que el RSI de 2 períodos cruce el umbral configurado entre las dos velas cerradas más recientes. El método B simplemente verifica si el RSI de hace dos velas está por encima o por debajo del umbral. Ambos modos se pueden usar de forma independiente o juntos, con el Método B habilitado de forma predeterminada.
- **Dirección de la orden:** Las operaciones largas requieren que el EMA rápido esté por encima del EMA lento, mientras que el RSI indica condiciones de sobreventa (`RSI < threshold`). Las operaciones cortas reflejan la lógica y esperan lecturas de sobrecompra.

## Gestión de Puestos

- **Protección inicial:** Al ingresar, la estrategia calcula un límite de pérdidas y una toma de ganancias de distancia fija utilizando el paso del precio del valor. Ambos parámetros siguen los valores predeterminados de la versión MetaTrader (90 y 44 puntos respectivamente).
- **Bloqueo de ganancias:** Cuando está habilitado, el límite de pérdidas se mueve al punto de equilibrio más una compensación configurable una vez que el precio avanza una distancia de `BreakEvenTriggerPoints`. Esto refleja el comportamiento "ProfitLock" del EA original.
- **Temporizadores de abandono:** Dos mecanismos opcionales cierran operaciones después de un número predefinido de velas completadas (`AbandonBars`). El método A invierte la posición inmediatamente estableciendo una bandera de entrada opuesta, mientras que el método B simplemente cierra y espera nuevas señales del indicador.
- **Gestión del dinero:** La fórmula del tamaño del lote coincide con el guión original: el tamaño de la posición se deriva del saldo de la cartera, el porcentaje de riesgo, el tipo de cuenta (mini frente a estándar) y los límites de las operaciones reales. Configurar `UseMoneyManagement` en `false` vuelve al parámetro de volumen fijo.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Plazo de las velas procesadas. |
| `FixedVolume` | Volumen de orden base cuando la administración de dinero está deshabilitada. |
| `TakeProfitPoints` / `StopLossPoints` | Objetivo de beneficios y parada protectora en las subidas de precios. |
| `UseRsiMethodA` / `UseRsiMethodB` | Habilite RSI métodos de confirmación que coincidan con EA. |
| `RsiThreshold` | RSI nivel utilizado por ambos modos de confirmación. |
| `AbandonMethodA` / `AbandonMethodB` | Habilite las variantes de lógica de abandono. |
| `AbandonBars` | Número de velas completadas antes de que se active la lógica de abandono. |
| `UseMoneyManagement`, `RiskPercent`, `AccountIsMini`, `LiveTradingMode` | Controles de cálculo de volumen. |
| `UseProfitLock`, `BreakEvenTriggerPoints`, `BreakEvenOffsetPoints` | Activación y compensación del punto de equilibrio. |
| `MaxOpenTrades` | Número máximo de operaciones simultáneas (el comportamiento predeterminado es una posición abierta). |

## Notas de uso

1. La estrategia solo evalúa las velas completadas para mantener la coherencia con el experto MetaTrader, que se basa en las retrospectivas de la barra `shift`.
2. Habilite o deshabilite los métodos RSI para reproducir la configuración exacta utilizada en la plantilla original.
3. La lógica de equilibrio y abandono se basa en los máximos y mínimos de las velas para detectar caídas de precios. Cuando se ejecuta en períodos de tiempo más altos, considere la posibilidad de sobrepasos dentro de la barra.
4. La administración del dinero requiere una conexión de cartera que proporcione el `BeginValue`. Si no está disponible, la estrategia vuelve al volumen fijo.

## Archivos

- `CS/TrueScalperProfitLockBreakEvenStrategy.cs` – Implementación C# de la estrategia.
- `README_zh.md` – Documentación china.
- `README_ru.md` – Documentación rusa.
