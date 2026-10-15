# Estrategia Open Oscillator Cloud MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el asesor experto de MetaTrader **Exp_Open_Oscillator_Cloud_MMRec** a la API de alto nivel de StockSharp. El sistema opera el cruce del indicador Open Oscillator Cloud, que compara el precio de apertura actual con las aperturas de las barras más altas y más bajas dentro de una ventana deslizante y suaviza el resultado con una media móvil configurable.

## Lógica de la estrategia

### Construcción del indicador
- Se construye una ventana de retroceso (`Oscillator Period`, por defecto 20 barras) de velas terminadas del marco temporal seleccionado.
- Se encuentra la barra con el máximo más alto y se almacena su precio de apertura, y se encuentra la barra con el mínimo más bajo y se almacena su precio de apertura.
- Se calculan dos valores brutos para la vela actual:
  - **Banda superior** = apertura actual − precio de apertura en el máximo más alto.
  - **Banda inferior** = precio de apertura en el mínimo más bajo − apertura actual.
- Ambas series se suavizan con la media móvil elegida (`Smoothing Method`, `Smoothing Length`). Los tipos admitidos son medias móviles Simple, Exponencial, Suavizada y Ponderada.
- Se almacena el historial suavizado y se retrasa la señal por `Signal Bar` velas completamente cerradas (por defecto 1) para imitar la lógica original del EA que actúa sobre la barra anterior.

### Criterios de entrada
- **Entrada largo**: la banda superior de la barra anterior estaba por encima de la banda inferior y el último valor retrasado cruza hacia abajo (`upper ≤ lower`). Puede desactivarse mediante `Enable Long Entries`.
- **Entrada corto**: la banda superior de la barra anterior estaba por debajo de la banda inferior y el último valor retrasado cruza hacia arriba (`upper ≥ lower`). Puede desactivarse mediante `Enable Short Entries`.

### Criterios de salida
- **Salida largo**: la banda superior de la barra anterior estaba por debajo de la banda inferior, señalando un régimen bajista. Controlado por `Enable Long Exits`.
- **Salida corto**: la banda superior de la barra anterior estaba por encima de la banda inferior, señalando un régimen alcista. Controlado por `Enable Short Exits`.
- **Gestión de riesgos**: si `Stop Loss Points` o `Take Profit Points` son mayores que cero, la estrategia cierra automáticamente la posición una vez que el precio alcanza esas distancias (medidas en pasos de precio del instrumento) desde la entrada.

### Gestión de órdenes
- Solo se utilizan órdenes de mercado. Antes de abrir una nueva posición, el lado opuesto se aplana para permanecer alineado con el comportamiento de posición única del robot MetaTrader.
- El parámetro `Trade Volume` establece el tamaño base de posición para cada entrada.

## Parámetros
- `Candle Type` – marco temporal de las velas usadas para el oscilador (por defecto 1 hora).
- `Oscillator Period` – número de velas en la ventana deslizante (por defecto 20).
- `Smoothing Method` – media móvil aplicada a las brechas de apertura (Simple, Exponential, Smoothed, Weighted).
- `Smoothing Length` – longitud de la media móvil de suavizado (por defecto 10).
- `Signal Bar` – número de barras completamente cerradas para retrasar la evaluación de señales (por defecto 1).
- `Enable Long Entries` / `Enable Short Entries` – permite o bloquea la apertura de operaciones en cada dirección.
- `Enable Long Exits` / `Enable Short Exits` – permite o bloquea las salidas automáticas para la dirección respectiva.
- `Trade Volume` – tamaño de cada orden de mercado (por defecto 1 contrato/lote).
- `Stop Loss Points` – distancia del stop protector en pasos de precio (0 desactiva el stop, por defecto 1000).
- `Take Profit Points` – distancia del objetivo de beneficio en pasos de precio (0 desactiva el objetivo, por defecto 2000).

## Notas de implementación
- Los métodos de suavizado coinciden con las opciones más comunes del EA original. Los modos exóticos como JJMA, T3, VIDYA o AMA no se portan porque StockSharp ya ofrece ricas alternativas para optimización y robustez.
- Las señales se evalúan solo en eventos `CandleStates.Finished` para evitar actuar sobre datos incompletos.
- La estrategia mantiene un historial interno de valores suavizados en lugar de consultar buffers de indicadores, lo que se alinea con el flujo de trabajo de alto nivel recomendado por StockSharp.
- Los niveles de protección se borran automáticamente cuando la posición queda plana para evitar que stops obsoletos reactiven operaciones.

## Comportamiento por defecto
- Seguimiento de tendencia en ambas direcciones con confirmación retrasada para reducir el ruido.
- Utiliza gestión de dinero fija (constante `Trade Volume`) respetando las distancias de stop loss y take profit, similar a la versión MetaTrader.
- Adecuada como plantilla para experimentar con diferentes tipos de suavizado o combinar el oscilador con filtros adicionales.
