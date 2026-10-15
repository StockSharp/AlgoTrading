# Estrategia de Conteo de Señales con Array
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia reproduce la lógica del asesor experto de MetaTrader 4 `Signal-COunt-with array.mq4`.
Monitorea los extremos del canal Donchian para un conjunto configurable de offsets de precio y cuenta con qué frecuencia
la salida del indicador cambia, queda vacía o vuelve a un valor de señal. La implementación mantiene
el enfoque diagnóstico del script original: no se ejecutan operaciones. En su lugar, la estrategia imprime
estadísticas detalladas siempre que se registra un nuevo máximo/mínimo o cuando el registro por vela está habilitado.

## Concepto

- Reemplazar la búsqueda `iCustom` original de `super_signals_v2_alert` con un canal Donchian que
  proporciona el máximo más alto y el mínimo más bajo durante `ChannelPeriod` velas.
- Evaluar una cuadrícula de offsets (`GapStart`, `GapStep`, `GapCount`) que emulan las múltiples configuraciones de indicadores
  probadas por el script MQL.
- Para cada offset rastrear seis contadores que reflejan los arrays originales, incluyendo transiciones hacia y
  fuera del valor centinela (`2147483647` para lecturas superiores vacías y `-2147483646` para lecturas inferiores vacías).
- Generar una tabla de texto con los contadores acumulados para que el usuario pueda inspeccionar con qué frecuencia cada buffer
  produce una señal nueva, vuelve a estar vacío o sale del estado cero por defecto.

## Parámetros

| Parámetro | Por defecto | Descripción |
|-----------|-------------|-------------|
| `CandleType` | Marco temporal de 5 minutos | Serie de velas usada para los cálculos de Donchian. |
| `ChannelPeriod` | 24 | Número de velas usadas para determinar los máximos y mínimos de Donchian. |
| `GapStart` | 0 | Primer offset (en múltiplos del paso de precio) aplicado a los valores de señal virtuales. |
| `GapStep` | 1 | Tamaño del paso (en pasos de precio) entre offsets consecutivos. |
| `GapCount` | 8 | Número de offsets a evaluar (coincide con el bucle 0..7 original). |
| `LogOnEachCandle` | false | Cuando está habilitado, fuerza el registro después de cada vela terminada. |

## Contadores

Cada offset mantiene dos filas: el índice `0` representa el buffer superior de Donchian (señal alcista) y
el índice `1` representa el buffer inferior (señal bajista). Se recopilan las siguientes estadísticas:

- **Changed** – se incrementa cada vez que el valor bruto del indicador difiere de la observación anterior.
- **Empty** – cuenta cuántas veces el buffer devolvió el centinela positivo (`2147483647`).
- **NegEmpty** – cuenta las ocurrencias del centinela negativo (`-2147483646`), principalmente para el buffer inferior.
- **Zero** – rastreat ransiciones del estado cero predeterminado a cualquier valor distinto de cero.
- **NewFromEmpty** – se incrementa cuando una señal basada en precio real reemplaza el valor centinela.
- **BackToEmpty** – se incrementa cuando el buffer vuelve a su centinela después de mantener un valor no centinela.

Estos contadores se corresponden uno a uno con los arrays mantenidos en el Expert Advisor original
(`GetInd_iCustom_changed`, `GetInd_iCustom_maxInt`, `GetInd_iCustom_minInt`, etc.).

## Registro

La estrategia imprime diagnósticos a través de `AddInfoLog` en dos situaciones:

1. Siempre que la banda superior de Donchian sube o la banda inferior baja, indicando un nuevo extremo.
2. Cada vela terminada cuando `LogOnEachCandle` está establecido en `true`.

Cada entrada de registro comienza con el tiempo de la vela y luego lista los contadores para cada offset, facilitando
la comparación del comportamiento entre diferentes configuraciones de indicadores virtuales.

## Notas de uso

- Adjuntar la estrategia a cualquier instrumento; solo depende de velas históricas y no envía órdenes.
- Ajustar `ChannelPeriod` para que coincida con la volatilidad del instrumento que se está estudiando. Un período más largo
  imita una detección de swing más amplia similar al indicador MT4.
- Aumentar `GapCount` si necesitas observar más offsets. Los arrays se redimensionan automáticamente al inicio.
- Combinar los diagnósticos con dibujos en el gráfico (velas más canal Donchian) para alinear visualmente las
  estadísticas impresas con la estructura del mercado.
