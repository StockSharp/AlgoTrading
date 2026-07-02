# Estrategia IFS Fractals
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
IFS Fractals es un puerto del script MetaTrader 5 `IFS_Fractals`. El experto original genera un mapa de bits del sistema de funciones iteradas (IFS) de la "palabra fractal" aplicando repetidamente 28 transformaciones afines a una nube de puntos. La versión StockSharp convierte el mismo proceso caótico en un oscilador direccional: la coordenada X de los puntos generados se escala, se suaviza con un promedio móvil exponencial (EMA) y se interpreta como un indicador de impulso que impulsa entradas largas y cortas.

## Lógica estratégica
### Sistema de funciones iteradas
* **Transformaciones afines**: cada vela terminada desencadena un lote de iteraciones (configurables). Durante cada iteración, se selecciona una de las 28 transformaciones de acuerdo con los pesos de probabilidad originales (todos iguales a 35). La transformación actualiza el punto actual `(x, y)` utilizando los coeficientes transferidos palabra por palabra del código MQL5.
* **Tabla de probabilidad**: la estrategia precalcula una matriz de probabilidad acumulada una vez al inicio, lo que permite una selección rápida de la siguiente transformación utilizando un único sorteo aleatorio dentro de la masa de probabilidad total.

### Construcción de señal
* **Normalización**: la coordenada X se divide por el mismo factor de escala (`50` por defecto) que el script utilizó al proyectar el fractal en el mapa de bits. Esto mantiene la señal en un rango numérico estable independientemente del precio del instrumento.
* **EMA suavizado**: la serie normalizada alimenta un EMA cuyo período es configurable. El EMA actúa como un filtro de paso bajo que extrae la deriva dominante de las iteraciones caóticas.
* **Lógica de entrada**: cuando EMA supera el umbral de entrada positivo, la estrategia abre o invierte en una posición larga. Simétricamente, cuando el EMA cae por debajo del umbral negativo, se abre o se invierte en corto.
* **Lógica de salida**: las posiciones largas abiertas salen una vez que EMA vuelve a caer al umbral de salida o por debajo de él, mientras que las posiciones cortas salen cuando el EMA vuelve a subir por encima del umbral de salida negativo. Esto crea una banda de histéresis que evita cambios rápidos alrededor de cero.

### Gestión de riesgos
* **Protección de posición**: se pueden habilitar distancias opcionales de stop-loss y take-profit a través de `StartProtection`. Un valor de `0` deshabilita el nivel respectivo, coincidiendo con el comportamiento del script fuente que operaba sin órdenes de protección.
* **Control de volumen**: las entradas utilizan un parámetro de volumen de mercado fijo. Cualquier exposición opuesta existente se cierra antes de abrir una nueva operación para mantener una posición direccional única.

## Parámetros
* **Volumen** – volumen de mercado para nuevas entradas.
* **Tipo de vela**: período de tiempo que impulsa las iteraciones fractales (predeterminado: velas de 5 minutos).
* **Iteraciones**: número de iteraciones de IFS procesadas después de cada vela terminada.
* **Escala**: divisor aplicado a la coordenada X antes de introducirla en EMA.
* **Umbral de entrada**: valor absoluto EMA requerido para abrir una posición (positivo para largos, negativo reflejado para cortos).
* **Umbral de salida**: valor EMA que activa las salidas cuando la señal vuelve a cero.
* **EMA Período** – período de suavizado de la media móvil exponencial aplicada a la señal fractal normalizada.
* **Take Profit** – distancia absoluta de obtención de beneficios; configúrelo en `0` para deshabilitarlo.
* **Stop Loss** – distancia absoluta de stop-loss; configúrelo en `0` para deshabilitarlo.

## Notas adicionales
* Cada ejecución produce una secuencia comercial diferente a menos que se inyecte una semilla aleatoria determinista modificando la fuente; esto refleja la aleatoriedad del script de representación de mapa de bits original.
* La estrategia no requiere ningún indicador derivado del mercado. Todos los datos se generan internamente a partir de los coeficientes IFS, por lo que las velas suscritas simplemente proporcionan tiempo para las iteraciones.
* No se incluye ninguna implementación de Python en este paquete. Solo la estrategia C# está disponible en `CS/`.
