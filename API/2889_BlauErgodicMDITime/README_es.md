# Estrategia Blau Ergodic MDI Time
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General

La **Estrategia Blau Ergodic MDI Time** es una conversión directa del experto MetaTrader `Exp_BlauErgodicMDI_Tm.mq5` a StockSharp. Opera sobre velas de marco temporal superior y reproduce los tres modos de señal del algoritmo original: **Breakdown**, **Twist** y **CloudTwist**. La estrategia se basa en un proceso de suavizado de media móvil exponencial (EMA) de múltiples etapas aplicado a un precio de vela seleccionado. Todos los cálculos se realizan dentro de la estrategia sin indicadores adicionales para que la lógica coincida con el experto MetaTrader mientras permanece compatible con la API de alto nivel de StockSharp.

El pipeline de suavizado sigue la lógica del oscilador Blau Ergodic MDI:

1. Suavizar el precio elegido con una EMA (longitud `BaseLength`).
2. Restar el valor suavizado del precio bruto para obtener una serie de diferencias.
3. Aplicar tres EMA consecutivas a la diferencia (longitudes `FirstSmoothingLength`, `SecondSmoothingLength`, `ThirdSmoothingLength`).
4. Escalar las salidas intermedia (`histogram`) y final (`signal`) por el paso de precio del instrumento. Estos valores impulsan las señales de trading.

## Modos de Señal

### Breakdown

* Usa el histograma dos barras atrás (controlado por `SignalBar`).
* Cuando el valor anterior del histograma es positivo y la barra seleccionada se mueve a territorio no positivo, la estrategia prepara una entrada en largo y opcionalmente cierra posiciones cortas.
* Cuando el valor anterior del histograma es negativo y la barra seleccionada sube a territorio no negativo, la estrategia prepara una entrada corta y opcionalmente cierra posiciones largas.

### Twist

* Compara la pendiente del histograma sobre dos barras históricas.
* Si el histograma acelera hacia arriba (barra `SignalBar + 1` < barra `SignalBar + 2`) y la barra seleccionada más reciente está por encima de la anterior, se genera una señal de entrada en largo. Las posiciones cortas pueden cerrarse en el mismo bloque.
* Si el histograma acelera hacia abajo (barra `SignalBar + 1` > barra `SignalBar + 2`) y la barra seleccionada más reciente está por debajo de la anterior, la estrategia prepara una entrada corta y puede cerrar posiciones largas.

### CloudTwist

* Usa tanto el histograma como la línea suavizada adicional.
* Cuando el histograma anterior permanece por encima de la línea de señal pero la barra seleccionada cae por debajo, se prepara una entrada en largo y pueden cerrarse posiciones cortas.
* Cuando el histograma anterior está por debajo de la línea de señal pero la barra seleccionada cruza por encima, la estrategia prepara una entrada corta y puede salir de posiciones largas.

## Filtro de Ventana de Tiempo

El experto original restringe el trading a una sesión configurable. La versión StockSharp replica las mismas reglas mediante los parámetros `UseTimeFilter`, `StartHour`, `StartMinute`, `EndHour` y `EndMinute`. La lógica de sesión soporta ventanas que cruzan la medianoche, idéntica a la implementación MetaTrader:

* Si la hora de inicio es anterior a la hora de fin, la sesión se mantiene dentro de un día.
* Si la hora de inicio es igual a la hora de fin, los minutos definen un intervalo más corto durante esa hora.
* Si la hora de inicio es posterior a la hora de fin, la sesión se extiende sobre la medianoche.

Cuando el trading está deshabilitado por el filtro de sesión, la estrategia cierra a mercado cualquier posición abierta y bloquea nuevas entradas hasta que la sesión se vuelva a abrir.

## Gestión de Riesgo

Los parámetros `StopLossPoints` y `TakeProfitPoints` reflejan las distancias de stop-loss y take-profit del experto. Las distancias se expresan en pasos de precio. La estrategia recalcula los precios protectores cada vez que se abre una nueva posición. Cada vela finalizada comprueba si el rango de la barra tocó algún nivel protector y cierra inmediatamente la posición si se activa.

## Entradas de Precio

El parámetro `PriceMode` expone la misma lista de fuentes de precio que el indicador original:

| Modo | Descripción |
| ---- | ----------- |
| Close | Precio de cierre. |
| Open | Precio de apertura. |
| High | Precio máximo. |
| Low | Precio mínimo. |
| Median | (High + Low) / 2. |
| Typical | (High + Low + Close) / 3. |
| Weighted | (High + Low + 2 × Close) / 4. |
| Simple | (Open + Close) / 2. |
| Quarter | (Open + High + Low + Close) / 4. |
| TrendFollow0 | High en velas alcistas, Low en bajistas, Close en neutras. |
| TrendFollow1 | Promedio de Close con el extremo de la vela en la dirección de la tendencia. |
| Demark | Precio Demark (ponderado por la dirección de la vela). |

## Parámetros

| Parámetro | Predeterminado | Descripción |
| --------- | -------------- | ----------- |
| `Mode` | Twist | Selecciona la evaluación de señal Breakdown, Twist o CloudTwist. |
| `PriceMode` | Close | Fuente de precio usada para el oscilador. |
| `BaseLength` | 20 | Longitud de EMA aplicada al precio bruto. |
| `FirstSmoothingLength` | 5 | Longitud de EMA del primer suavizado de diferencias. |
| `SecondSmoothingLength` | 3 | Longitud de EMA del segundo suavizado de diferencias. |
| `ThirdSmoothingLength` | 8 | Longitud de EMA del tercer suavizado de diferencias. |
| `SignalBar` | 1 | Número de barras completadas atrás usadas para verificaciones de señal (1 coincide con el predeterminado de MetaTrader). |
| `AllowLongEntry` / `AllowShortEntry` | true | Habilitar o deshabilitar entradas en largo/corto. |
| `AllowLongExit` / `AllowShortExit` | true | Habilitar o deshabilitar salidas para el lado correspondiente. |
| `UseTimeFilter` | true | Activa el filtro de sesión de trading. |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | 0/0/23/59 | Límites de la sesión. |
| `StopLossPoints` | 1000 | Distancia de stop-loss en pasos de precio (0 deshabilita). |
| `TakeProfitPoints` | 2000 | Distancia de take-profit en pasos de precio (0 deshabilita). |
| `CandleType` | Marco temporal 4h | Suscripción de velas usada para cálculos. |
| `Volume` | 0.1 | Volumen de la orden, coincidiendo con el input `MM` del experto. |

## Resumen de Reglas de Trading

1. Suscribirse a las velas del marco temporal configurado.
2. En cada vela finalizada, actualizar el pipeline EMA de cuatro etapas y almacenar los valores del histograma y la señal en buffers deslizantes.
3. Esperar hasta que se alcance la profundidad mínima de historial (coincidiendo con el cálculo original de `min_rates_total`).
4. Evaluar el modo seleccionado usando la barra `SignalBar` y valores más antiguos para establecer indicadores de apertura/cierre.
5. Cerrar posiciones primero si se activa el indicador de salida correspondiente o si el filtro de tiempo bloquea el trading.
6. Abrir nuevas operaciones en largo o corto solo cuando se establezca el indicador respectivo, el filtro de tiempo permita el trading y la posición actual no apunte ya en la misma dirección. Al revertir, la estrategia dimensiona automáticamente la orden para cubrir la exposición existente más el volumen configurado.
7. Mantener stops y objetivos protectores usando extremos de vela para detectar activaciones.

## Notas de Uso

* La estrategia usa tabulaciones para indentación, consistente con las directrices del proyecto.
* Llama a `StartProtection()` una vez durante el inicio para mantener las funciones de seguridad de StockSharp alineadas con los cambios de posición.
* Los valores del indicador se almacenan solo para el número mínimo de barras requeridas por las señales. No se crean grandes colecciones, siguiendo las instrucciones del repositorio.
* Para experimentar con otros métodos de suavizado de la versión MetaTrader, ajustar las longitudes de EMA en consecuencia. El pipeline basado en EMA proporciona la aproximación más cercana soportada por la API de alto nivel de StockSharp.

## Ejecución de la Estrategia

1. Agregar la clase de estrategia a su solución StockSharp y compilar el proyecto.
2. Configurar los parámetros (instrumento, marco temporal de velas, modo, sesión y configuración de riesgo).
3. Adjuntar la estrategia a un conector que proporcione los datos de mercado requeridos.
4. Iniciar la estrategia; se suscribirá automáticamente a las velas configuradas y gestionará las órdenes según las reglas anteriores.
