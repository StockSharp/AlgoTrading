# Estrategia Exp i-KlPrice Vol Directo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
La **Estrategia Exp i-KlPrice Vol Directo** es una adaptación de StockSharp del asesor experto de MetaTrader 5
`Exp_i-KlPrice_Vol_Direct`. El sistema original multiplica un oscilador KlPrice personalizado por el volumen, lo suaviza con
varias etapas de media móvil y reacciona a los cambios en la pendiente de la línea resultante. El port mantiene la cadena de
procesamiento de múltiples etapas, expone los mismos parámetros configurables y ejecuta operaciones a través de la API de alto
nivel de StockSharp en velas completadas.

Ideas clave preservadas de la versión MQL5:
- **Suavizado de dos etapas de precio y rango** – los datos de precio se filtran por una media móvil configurable, el rango
  máximo-mínimo se suaviza por separado y los resultados forman bandas dinámicas adaptativas.
- **Ponderación de volumen** – la salida del oscilador se multiplica por el flujo de volumen seleccionado (tick o real) antes
  de un filtro Jurik final, amplificando los movimientos que ocurren en mayor actividad.
- **Mapa de color direccional** – la estrategia monitorea el signo de la pendiente del oscilador suavizado. Un cambio de
  color bajista a alcista abre largos y cierra cortos; el cambio opuesto abre cortos y cierra largos.
- **Retraso de señal** – `SignalBar` permite al usuario requerir velas cerradas adicionales antes de actuar, coincidiendo con
  la lógica de "barra confirmada" del código fuente.

## Pipeline de Procesamiento
1. **Selección de Precio Aplicado** – elegir entre las mismas doce fórmulas de precio aplicado que el indicador MQL (Close,
   Open, Median, Demark, TrendFollow, etc.).
2. **Suavizado Primario** – aplicar `PriceMethod` sobre `PriceLength` barras con `PricePhase` opcional (efectivo para filtros
   basados en Jurik).
3. **Suavizado de Rango** – repetir el mismo procedimiento para el rango de la vela (`High - Low`) usando `RangeMethod`,
   `RangeLength` y `RangePhase`.
4. **Construcción del Oscilador** – calcular `(Price - (PriceMA - RangeMA)) / (2 * RangeMA) * 100 - 50`, idéntico a la
   fórmula MQL, y multiplicar por el flujo de volumen seleccionado (`VolumeSource`).
5. **Filtro Jurik Final** – el oscilador ponderado por volumen y el flujo de volumen bruto pasan por medias móviles Jurik
   con período `ResultLength`.
6. **Detección de Color** – comparar el valor más reciente del oscilador suavizado con el anterior. Los valores crecientes
   colorean la barra de alcista (`0`), los decrecientes de bajista (`1`), los iguales heredan el color anterior.

## Lógica de Trading
### Lado Largo
- **Entrada**: cuando el color en la barra de señal (`SignalBar`) es alcista (`0`) y el color inmediatamente anterior es bajista
  (`1`), abrir una posición larga si `AllowLongEntries = true` y la posición neta actual no es positiva.
- **Salida**: si el color de la barra de señal es alcista y `AllowShortExits = true`, cerrar cualquier posición corta abierta.

### Lado Corto
- **Entrada**: cuando el color de la barra de señal se vuelve bajista (`1`) después de ser alcista (`0`), abrir una posición
  corta si `AllowShortEntries = true` y la posición neta actual no es negativa.
- **Salida**: si el color de la barra de señal es bajista y `AllowLongExits = true`, cerrar la exposición larga existente.

## Referencia de Parámetros
| Parámetro | Descripción | Valor predeterminado |
|-----------|-------------|----------------------|
| `CandleType` | Marco temporal de las velas analizadas. | `H4` |
| `VolumeSource` | Flujo de volumen para la ponderación (`Tick` o `Real`). | `Tick` |
| `PriceMethod` / `PriceLength` / `PricePhase` | Algoritmo de suavizado primario, período y fase Jurik para el precio aplicado. | `Sma`, `100`, `15` |
| `RangeMethod` / `RangeLength` / `RangePhase` | Algoritmo de suavizado, período y fase para el rango de la vela. | `Jjma`, `20`, `100` |
| `ResultLength` | Período Jurik para el oscilador ponderado por volumen y el flujo de volumen. | `20` |
| `PriceMode` | Fórmula de precio aplicado (Close, Open, Median, Demark, TrendFollow0/1, etc.). | `Close` |
| `HighLevel2`, `HighLevel1`, `LowLevel1`, `LowLevel2` | Multiplicadores de nivel para diagnóstico visual; no alteran señales. | `0`, `0`, `0`, `0` |
| `SignalBar` | Número de velas cerradas a saltar antes de evaluar el cambio de color. | `1` |
| `AllowLongEntries` / `AllowShortEntries` | Indicadores de permiso para abrir operaciones largas/cortas. | `true` |
| `AllowLongExits` / `AllowShortExits` | Indicadores de permiso para cerrar posiciones existentes en color opuesto. | `true` |
| `StopLossPoints` / `TakeProfitPoints` | Desplazamientos de protección en puntos de precio pasados a `StartProtection`. | `1000`, `2000` |

## Gestión de Riesgos
- Los niveles de stop-loss y take-profit se traducen en desplazamientos `UnitTypes.Point` y son gestionados por
  `StartProtection`. Establecer cualquier valor en `0` para deshabilitar la protección respectiva.
- El tamaño de posición está completamente controlado por `Strategy.Volume`.
- Los colores se evalúan solo cuando la estrategia está formada, en línea y el trading está permitido.

## Limitaciones y Diferencias vs. MQL5
- Las aproximaciones de suavizado más exóticas pueden desviarse ligeramente de la salida de MT5.
- Los datos de volumen de StockSharp exponen solo el volumen total.
- Los modos de gestión de dinero del EA original no están portados.
- Las órdenes se envían inmediatamente después del cierre de la vela de señal.
