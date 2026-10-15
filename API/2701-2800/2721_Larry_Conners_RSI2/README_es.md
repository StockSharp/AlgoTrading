# Estrategia Larry Connors RSI-2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un port fiel al sistema clásico Larry Connors RSI-2 para StockSharp. La estrategia combina un oscilador RSI rápido de 2 períodos con filtros de media móvil en el marco temporal horario para capturar configuraciones de reversión a la media a corto plazo mientras se mantiene alineada con la tendencia del marco temporal superior. Los niveles opcionales de stop-loss y take-profit, expresados en pips, replican las reglas originales de gestión monetaria de MetaTrader.

## Descripción General

- **Tipo**: Reversión a la media con filtro de tendencia.
- **Mercado**: Diseñada para pares Forex en el gráfico H1.
- **Dirección**: Opera tanto largo como corto, pero solo en la dirección del filtro de SMA lenta.
- **Indicadores principales**: SMA de 5 períodos (momento de salida), SMA de 200 períodos (filtro de tendencia), RSI de 2 períodos (disparador de señal).

## Reglas de Trading

### Entradas Largo
- El valor RSI cae por debajo de `RSI Long Entry` (por defecto 6).
- El precio de cierre de la vela completada se mantiene por encima de `Slow SMA` (por defecto 200 períodos).
- No hay posición abierta.

### Entradas Corto
- El valor RSI sube por encima de `RSI Short Entry` (por defecto 95).
- El precio de cierre está por debajo de `Slow SMA`.
- No hay posición abierta.

### Condiciones de Salida
- **Posiciones largo** se cierran cuando el cierre supera la `Fast SMA` (por defecto 5). Los niveles opcionales de stop-loss y take-profit medidos en pips también pueden cerrar la operación si están activados.
- **Posiciones corto** se cierran cuando el cierre cae por debajo de la `Fast SMA`. Los niveles opcionales de stop-loss y take-profit en pips se aplican simétricamente.

### Gestión de Riesgo
- `Use Stop Loss` activa una distancia fija de stop en pips relativa al precio de entrada.
- `Use Take Profit` habilita un objetivo de beneficio simétrico en pips.
- Las distancias en pips se convierten a precios absolutos mediante el `PriceStep` del instrumento y la precisión decimal, replicando la lógica MT5 para cotizaciones de 4/5 dígitos.

## Valores Predeterminados

| Parámetro | Por defecto | Descripción |
|-----------|-------------|-------------|
| `Trade Volume` | 1 | Volumen de orden base para cada entrada. |
| `Fast SMA Period` | 5 | Media de temporización de salida. |
| `Slow SMA Period` | 200 | Filtro de dirección de tendencia. |
| `RSI Period` | 2 | Período de observación del oscilador RSI. |
| `RSI Long Entry` | 6 | Umbral de sobreventa para operaciones largo. |
| `RSI Short Entry` | 95 | Umbral de sobrecompra para operaciones corto. |
| `Use Stop Loss` | true | Activar/desactivar stop protector. |
| `Stop Loss (pips)` | 30 | Distancia del stop-loss en pips. |
| `Use Take Profit` | true | Activar/desactivar objetivo fijo de beneficio. |
| `Take Profit (pips)` | 60 | Distancia del objetivo de beneficio en pips. |
| `Candle Type` | 1 hora | Marco temporal de las velas de trabajo. |

Todos los parámetros ajustables exponen `.SetCanOptimize(true)` permitiendo la optimización por lotes dentro de Designer/Tester.

## Notas de Ejecución

- Las señales se evalúan en velas cerradas para coincidir con la implementación original de MetaTrader.
- Los niveles protectores se rastrean internamente, cerrando toda la posición con órdenes de mercado cuando se superan.
- La estrategia restablece el estado interno (`pipSize`, anclas de entrada) en cada reinicio para garantizar backtests reproducibles.
- Añada la estrategia a un proyecto junto con datos Forex fiables para replicar los resultados de rendimiento publicados.

## Uso Sugerido

1. Conecte un feed de datos Forex que suministre velas de 1 hora.
2. Añada la estrategia a Designer o ejecútela programáticamente a través de StockSharp API.
3. Ajuste los parámetros de riesgo basados en pips para que coincidan con las especificaciones del contrato del broker si es necesario.
4. Opcionalmente optimice los umbrales RSI o las longitudes de medias móviles para adaptar el modelo a otros símbolos.

Al preservar la lógica exacta de RSI y medias móviles, este port permite a los usuarios de MT5 evaluar la metodología Larry Connors RSI-2 dentro del ecosistema StockSharp.
