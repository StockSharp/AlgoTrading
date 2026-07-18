# Estrategia IBS RSI CCI v4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia IBS RSI CCI v4** es un sistema de trading contrario que combina tres osciladores de momentum:

- **Internal Bar Strength (IBS)** – mide la posición relativa del cierre dentro del rango máximo-mínimo de la barra y se suaviza con una media móvil configurable.
- **Relative Strength Index (RSI)** – captura el momentum del mercado alrededor del nivel neutro de 50.
- **Commodity Channel Index (CCI)** – evalúa la desviación del precio de una línea base de media móvil.

Los tres componentes se escalan y mezclan en un oscilador compuesto. La señal compuesta está restringida por un umbral de paso configurable y filtrada a través de un envolvente de máximos/mínimos de estilo Donchian. Los cruces entre la señal compuesta y su línea media generan oportunidades de reversión.

## Lógica de trading
1. Suscribirse a velas con el marco temporal seleccionado (predeterminado: 4 horas).
2. Calcular el valor IBS para cada vela terminada y suavizarlo con el tipo de media móvil elegido.
3. Obtener valores RSI y CCI usando sus respectivas longitudes de lookback.
4. Construir el oscilador compuesto usando la ponderación original del script de MetaTrader:
   - Contribución IBS × 700
   - Desviación RSI de 50 × 9
   - Valor CCI bruto × 1
5. Aplicar un umbral de paso para evitar saltos repentinos en la señal compuesta.
6. Rastrear el máximo y mínimo rodantes de la señal compuesta y suavizar ambos bordes para formar una banda dinámica. La línea media de la banda se usa como "línea base" (equivalente al segundo buffer de indicador en la versión MQL).
7. **Gestión de posiciones**
   - Cerrar posiciones largas cuando la señal compuesta está por debajo de la línea base en la barra confirmada.
   - Cerrar posiciones cortas cuando la señal compuesta está por encima de la línea base en la barra confirmada.
   - Abrir posiciones largas cuando la barra confirmada previamente estaba por encima de la línea base y la señal más reciente cruza hacia abajo a través de la línea base (entrada contraria).
   - Abrir posiciones cortas cuando la barra confirmada previamente estaba por debajo de la línea base y la señal más reciente cruza hacia arriba a través de la línea base.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Serie de velas utilizada para cálculos de indicadores. |
| `IbsPeriod` | Longitud de lookback utilizada para suavizar el componente IBS. |
| `IbsAverageType` | Tipo de media móvil para suavizado IBS (Simple, Exponencial, Suavizado, Ponderado Lineal). |
| `RsiPeriod` | Longitud de lookback RSI. |
| `CciPeriod` | Longitud de lookback CCI. |
| `RangePeriod` | Tamaño de ventana para la banda rodante de máximos/mínimos aplicada a la señal compuesta. |
| `SmoothPeriod` | Longitud de la media móvil utilizada para suavizar los bordes de la banda de máximos/mínimos. |
| `RangeAverageType` | Tipo de media móvil para el suavizado de la banda (Simple, Exponencial, Suavizado, Ponderado Lineal). |
| `StepThreshold` | Ajuste máximo aplicado cuando la señal compuesta salta bruscamente entre barras. |
| `SignalBar` | Número de velas ya cerradas utilizadas para confirmación (predeterminado 1 replica el comportamiento original). |
| `EnableLongOpen` | Permitir abrir nuevas posiciones largas. |
| `EnableShortOpen` | Permitir abrir nuevas posiciones cortas. |
| `EnableLongClose` | Permitir cerrar posiciones largas existentes. |
| `EnableShortClose` | Permitir cerrar posiciones cortas existentes. |
| `OrderVolume` | Volumen base de la orden de mercado enviada en las entradas. |

## Notas de implementación
- La restricción de paso replica la lógica de limitación del buffer del indicador MQL. Un `StepThreshold` más alto permite saltos más grandes en el oscilador compuesto.
- Solo se admiten las cuatro familias de medias móviles más comunes para el suavizado IBS y del envolvente, porque la biblioteca estándar de StockSharp no incluye los filtros personalizados del archivo de recursos de MetaTrader.
- La estrategia usa `SignalBar` para retrasar las señales en una vela completamente cerrada, coincidiendo con el comportamiento del asesor experto original.
- Por defecto la estrategia es completamente contraria: las señales se generan en contra de la dirección del cruce más reciente. Alterne los booleanos de entrada/salida para limitar la estrategia a una sola dirección si se desea.

## Uso
1. Configure el `CandleType` para que coincida con el marco temporal de su instrumento objetivo.
2. Ajuste las longitudes de los indicadores y el umbral de paso para adaptarse a la volatilidad del instrumento.
3. Habilite o deshabilite las entradas y salidas largas/cortas según su preferencia de trading.
4. Configure el parámetro `OrderVolume` para controlar el tamaño de la orden e inicie la estrategia. `StartProtection()` está habilitado por defecto y puede personalizarse si se requieren reglas de riesgo adicionales.
5. Revise el panel de gráfico (si está disponible) para monitorear los precios de las velas, el oscilador compuesto y las operaciones registradas.

## Diferencias con la versión de MetaTrader
- Los parámetros de gestión de dinero y desviación de órdenes del EA original se reemplazan con el parámetro `OrderVolume` de StockSharp y órdenes de mercado de alto nivel.
- La conversión de StockSharp mantiene las ponderaciones originales del indicador y la lógica de reversión, pero se centra en los filtros de media móvil más utilizados.
- Los stops protectores no están preconfigurados; combine la estrategia con los módulos de riesgo de StockSharp si se requieren stops fijos o take-profits.
