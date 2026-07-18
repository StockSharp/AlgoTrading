# Estrategia Color XMUV con Filtro de Tiempo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el asesor experto de MetaTrader **Exp_ColorXMUV_Tm** a StockSharp. Recrea la línea suavizada Color XMUV original y el filtro de ventana de tiempo mientras usa la API de trading de alto nivel de StockSharp. La estrategia sigue el color de la línea suavizada: una transición a verde azulado (subiendo) activa la gestión de largos mientras una transición a magenta (bajando) impulsa la gestión de cortos.

## Lógica central
- Para cada candle finalizado la estrategia construye un precio compuesto similar a la versión MQL (`(H + Close)/2` en barras alcistas, `(L + Close)/2` en barras bajistas, o `Close` para barras doji).
- El precio compuesto pasa por el método de suavizado solicitado. Los métodos comunes (SMA, EMA, SMMA/RMA, LWMA y Jurik) están implementados con indicadores StockSharp. Las opciones exóticas como T3 o VIDYA recurren a una EMA porque StockSharp no expone equivalentes directos. El parámetro phase se mantiene para compatibilidad de configuración incluso cuando el indicador subyacente lo ignora.
- El "color" de Color XMUV se reconstruye comparando el último valor suavizado con el anterior. Las pendientes ascendentes se asignan a color alcista, las pendientes descendentes a color bajista y los valores sin cambio a color neutral.
- `SignalBar` define cuántas barras completamente terminadas mirar atrás al evaluar una señal (por ejemplo, el valor predeterminado de 1 significa que la lógica espera confirmación en la barra anterior a la más reciente).
- Un flip alcista (color anterior no alcista, color actual alcista) cierra cualquier posición corta y opcionalmente abre o agrega a una posición larga. Un flip bajista realiza las acciones simétricas para operaciones cortas.
- El filtro de tiempo imita el EA original: fuera de la ventana de trading la estrategia cierra inmediatamente las posiciones existentes e ignora nuevas entradas. El filtro soporta sesiones nocturnas (hora de inicio posterior a la hora de fin).
- `StopLossPoints` y `TakeProfitPoints` se traducen en distancias absolutas usando el paso de precio del instrumento y se registran con `StartProtection` para que StockSharp gestione las salidas del lado del servidor donde sea posible.

## Gestión del riesgo y las posiciones
- Las órdenes se dimensionan con el parámetro `OrderVolume`. Al invertir la dirección la estrategia agrega el valor absoluto de la posición actual para que la reversión cierre la operación anterior y abra una nueva en una sola transacción.
- El stop-loss y take-profit opcionales se convierten de valores de puntos a distancias de precio absolutas. Ponga cualquier parámetro en cero para deshabilitar la respectiva capa de protección.
- Las salidas de posiciones activadas por el flip de color respetan los interruptores `EnableBuyExits` y `EnableSellExits`, permitiendo control independiente de la gestión de largos y cortos.

## Parámetros
- **Candle Type** – Serie de candles usada para cálculos (predeterminado candles de 4 horas).
- **Order Volume** – Tamaño base de la orden de mercado.
- **Enable Long Entries / Enable Short Entries** – Permitir apertura de posiciones en flips alcistas/bajistas.
- **Close Longs / Close Shorts** – Habilitar salidas automáticas en transiciones de color opuesto.
- **Use Time Filter** – Restringir trading a la sesión configurada.
- **Start Hour / Start Minute / End Hour / End Minute** – Límites de la sesión de trading. Cuando el inicio es posterior al fin, la sesión se extiende a través de la medianoche.
- **Smoothing Method** – Algoritmo de media móvil para la línea Color XMUV. Las opciones sin implementación nativa en StockSharp son reemplazadas por EMA y están documentadas arriba.
- **Length** – Longitud de suavizado (debe ser positivo).
- **Phase** – Parámetro de phase auxiliar retenido para compatibilidad de configuración.
- **Signal Bar** – Número de barras completadas para retrasar la verificación de señal. Poner en cero para actuar sobre la barra cerrada más reciente.
- **Stop Loss (pts) / Take Profit (pts)** – Offsets expresados en puntos de precio; cero deshabilita la respectiva capa.

## Notas
- El expert MQL se basaba en bibliotecas de suavizado externas. Cuando dichos modos de suavizado no están disponibles en StockSharp (ParMA, VIDYA, T3) la implementación sustituye una EMA. Documente estas alternativas cuando comparta la estrategia con usuarios.
- La estrategia almacena solo el historial mínimo de color requerido por `SignalBar`, cumpliendo con la directriz del repositorio que desaconseja construir cachés de datos personalizados.
