# Estrategia de Zonas RSI de Bollinger Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un sistema de ruptura multi-banda de Bollinger Bands convertido del asesor experto MetaTrader «Bollinger Bands RSI». La estrategia deriva tres envolventes de Bollinger con períodos idénticos pero diferentes desviaciones para crear bandas «amarilla», «azul» y «roja». Las órdenes se activan cuando el precio revisita zonas configurables alrededor de estas bandas, con confirmación opcional de filtros RSI y Estocástico.

## Lógica de la estrategia
- La banda principal (amarilla) utiliza el multiplicador de desviación configurado.
- La banda azul reduce a la mitad la desviación, creando una envolvente más estrecha.
- La banda roja duplica la desviación, produciendo una envolvente exterior amplia.
- Los valores de RSI y Estocástico se evalúan en la vela terminada anterior (`Bar Shift`) para coincidir con el comportamiento original del EA.
- `Only One Position` controla si las nuevas órdenes están permitidas solo cuando la posición neta está plana o si se permiten operaciones de escala adicionales una vez que el precio regresa a la línea media de Bollinger.

## Reglas de entrada
### Entradas largas
1. El precio en la vela actual cae hasta o por debajo de la zona de entrada larga seleccionada (`Entry Mode`):
   - Punto medio entre amarilla y azul, azul y roja, o una de las bandas individuales.
2. Confirmaciones opcionales:
   - Filtro RSI: RSI ≤ `100 - RSI Lower`.
   - Filtro Estocástico: %K < `100 - Stochastic Lower`.
3. Requisitos de posición:
   - Si `Only One Position` está habilitado, la posición neta debe estar plana.
   - De lo contrario, las órdenes largas adicionales están bloqueadas hasta que la vela cierre por encima de la banda media (amarilla), emulando la lógica de bloqueo del EA.

### Entradas cortas
1. El precio en la vela actual sube hasta o por encima de la zona de entrada corta seleccionada (refleja las opciones largas).
2. Confirmaciones opcionales:
   - Filtro RSI: RSI ≥ `RSI Lower`.
   - Filtro Estocástico: %K > `Stochastic Lower`.
3. Los requisitos de posición reflejan la lógica larga (posición plana para el modo de una sola operación o estado desbloqueado cuando la vela cierra de vuelta por debajo de la banda media).

## Reglas de salida
- El modo de cierre está determinado por `Closure Mode`:
  - `Middle Line`: salir de largos cuando el precio alcanza la banda media de Bollinger; salir de cortos cuando el precio la toca desde arriba.
  - `Between Yellow and Blue` / `Between Blue and Red`: salir en los mismos puntos medios usados para las entradas; por defecto en los puntos medios entre azul y rojo cuando el modo de entrada difiere.
  - `Yellow Line`, `Blue Line`, `Red Line`: salir en toques directos de las bandas superiores/inferiores correspondientes.
- Los indicadores de bloqueo para el modo de escala se restablecen automáticamente cuando la vela cierra en el lado opuesto de la banda media, recreando el comportamiento del EA.

## Gestión de riesgos
- Los parámetros `Stop Loss` y `Take Profit` se expresan en pips y se convierten en distancias de precio absolutas a través de `Pip Value` cuando se inicializa `StartProtection`.
- Los stops y objetivos son opcionales; deje la distancia en cero para deshabilitar la protección respectiva.
- El volumen de la operación está definido por `Order Volume` y se aplica a cada orden de mercado.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `Entry Mode` | Elige la zona de Bollinger que activa las entradas. | Entre amarilla y azul |
| `Closure Mode` | Define la banda o punto medio de toma de ganancias. | Entre azul y roja |
| `Bands Period` | Longitud de período compartida por todas las bandas de Bollinger. | 140 |
| `Deviation` | Multiplicador de desviación estándar para la banda amarilla (azul es la mitad, roja es el doble). | 2.0 |
| `Use RSI Filter` | Activa la lógica de confirmación RSI. | false |
| `RSI Period` | Período de promediado RSI. | 8 |
| `RSI Lower` | Umbral de sobrecompra; la sobreventa usa `100 - valor`. | 70 |
| `Use Stochastic Filter` | Activa la lógica de confirmación %K. | true |
| `Stochastic Period` | Período de retroceso principal %K (suavizado fijo en 3/3 SMA). | 20 |
| `Stochastic Lower` | Umbral de sobrecompra; la sobreventa usa `100 - valor`. | 95 |
| `Bar Shift` | Número de barras terminadas para mirar hacia atrás en busca de valores de indicadores. | 1 |
| `Only One Position` | Si está habilitado, abre nuevas operaciones solo cuando no hay ninguna posición activa. | true |
| `Order Volume` | Volumen enviado con cada orden de mercado. | 1 |
| `Pip Value` | Valor de precio absoluto de un pip para la conversión de stop/objetivo. | 0.0001 |
| `Stop Loss` | Distancia de stop protector en pips (0 deshabilita). | 200 |
| `Take Profit` | Distancia de objetivo protector en pips (0 deshabilita). | 200 |
| `Candle Type` | Tipo de datos usado para los cálculos (por defecto velas de 1 minuto). | Marco temporal de 1m |

## Notas
- La estrategia procesa solo velas completadas, por lo que `Bar Shift` debe permanecer ≥ 1 para evitar referenciar barras sin terminar.
- Los filtros RSI y Estocástico usan la línea %K; la línea %D se calcula pero no se usa, reflejando la implementación original del EA.
- La conversión mantiene comentarios y nombres de señales en inglés y sigue las pautas de la API de alto nivel de StockSharp (pipeline de indicadores basado en Bind, sin acceso manual a buffers).
