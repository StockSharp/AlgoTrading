# Estrategia automática RXD v1.67
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Auto RXD v1.67 es una estrategia basada en reglas que emula al asesor experto MetaTrader del mismo nombre. El enfoque utiliza tres perceptrones lineales: un supervisor que decide si buscar señales alcistas o bajistas, además de un perceptrón dedicado para cada dirección. Cada perceptrón opera con promedios móviles ponderados lineales (LWMA) calculados a partir del cierre de la vela y las entradas de "precio ponderado" de Robbie Ruan (alto + mínimo + 2 × cierre). El puerto StockSharp se ejecuta solo en velas completadas y utiliza el flujo de datos de alto nivel `BindEx` para mantener los cálculos del indicador sincronizados con el ciclo comercial.

## Datos e indicadores del mercado
- **Velas**: el período de tiempo predeterminado es velas de 30 minutos. El plazo se puede cambiar a través del parámetro `CandleType`.
- **Rango verdadero promedio (ATR)**: proporciona distancias de toma de ganancias y de parada de pérdidas adaptables cuando `UseAtrTargets` está habilitado. El período ATR está controlado por `AtrPeriod`.
- **Índice de fuerza relativa (RSI)**: filtro opcional que aplica operaciones largas por encima del nivel neutral 50 y ventas cortas por debajo de 50 cuando `UseRsiFilter` es verdadero.
- **Índice de canales de productos básicos (CCI)**: filtro de tendencias opcional que requiere lecturas superiores a +100 para posiciones largas y inferiores a -100 para posiciones cortas cuando `UseCciFilter` está activo.
- **Divergencia de convergencia de media móvil (MACD)**: confirmación de impulso opcional. Las entradas largas requieren la línea MACD encima de la línea de señal, mientras que las cortas necesitan la línea MACD debajo de la línea de señal cuando `UseMacdFilter` es verdadero.
- **Índice direccional promedio (ADX)**: filtro de intensidad opcional que verifica que ADX esté por encima del umbral configurado y que +DI versus -DI se alinee con la dirección deseada cuando `UseAdxFilter` está habilitado.

## Lógica de trading
1. **Preparación de datos de Perceptron**: para cada vela, la estrategia actualiza los buffers con los últimos precios de cierre y ponderados. Los buffers alimentan instantáneas LWMA, generando cuatro características retrasadas separadas por los valores `Step` configurados para perceptrones cortos, largos y supervisores.
2. **Decisión del supervisor**: el perceptrón supervisor evalúa los deltas rezagados utilizando los parámetros de peso `SupervisorX1…X4` y `SupervisorThreshold`. Una puntuación positiva desbloquea el perceptrón largo; una puntuación negativa desbloquea el perceptrón corto. Si la puntuación del supervisor es cero o no está disponible (no hay suficientes datos), se omite la vela.
3. **Especialistas direccionales**: el perceptrón correspondiente (largo o corto) valida su propia puntuación utilizando el mismo conjunto de funciones LWMA y pesos específicos de dirección (`LongX*` o `ShortX*`). Un valor positivo desencadena la siguiente etapa de validación.
4. **Filtros de indicador**: cuando `UseIndicatorFilters` es falso, la estrategia opera únicamente con la señal del perceptrón. Cuando es verdadero, cada filtro habilitado (RSI, CCI, MACD, ADX) debe coincidir con la dirección propuesta. Los datos faltantes del indicador o las condiciones fallidas cancelan la señal.
5. **Ejecución de órdenes**: la estrategia garantiza que no haya órdenes activas, aplana cualquier exposición opuesta e ingresa utilizando órdenes de mercado de tamaño `OrderVolume`. Los precios de entrada se establecen por defecto en la mejor cotización cuando esté disponible; de ​​lo contrario, la vela se cierra.

## Gestión del riesgo
- **Órdenes de protección**: después de completar una entrada, la estrategia calcula inmediatamente las distancias de obtención de ganancias y parada de pérdidas hasta `CalculateProtectiveDistances`. Cuando `UseAtrTargets` es verdadero, las distancias escalan ATR según los multiplicadores configurados (`AtrTakeProfitFactor`, `AtrStopLossFactor`) y según las magnitudes TP/SL originales MQL basadas en puntos. Si la segmentación ATR está deshabilitada, las distancias de puntos fijos se convierten en incrementos de precios.
- **Gestión de órdenes**: el asistente `SetProtectiveOrders` traduce distancias brutas en conteos de pasos de precio y registra órdenes de stop-loss y take-profit en relación con el precio de entrada. La estrategia evita pedidos duplicados marcando `HasActiveOrders()` antes de enviar nuevas operaciones.
- **Iniciar protección**: `StartProtection()` se llama una vez en `OnStarted`, lo que permite el manejo de protección integrado del marco siempre que la posición sea distinta de cero.

## Parámetros
La implementación de StockSharp expone el conjunto completo de parámetros MQL agrupados para optimización y claridad de la interfaz de usuario. Los parámetros clave incluyen:

### Comercio
- `OrderVolume` – Tamaño del lote para nuevas posiciones.
- `CandleType`: tipo de datos de vela utilizado para el enlace.

### Riesgo
- `UseAtrTargets`: alterna entre distancias de protección basadas en ATR y de punto fijo.
- `AtrPeriod`, `AtrTakeProfitFactor`, `AtrStopLossFactor` – ATR configuración para objetivos adaptables.
- `LongTakeProfitPoints`, `LongStopLossPoints`, `ShortTakeProfitPoints`, `ShortStopLossPoints`: referencias TP/SL basadas en puntos reutilizadas tanto por ATR como por modos fijos.

### Filtros de indicador
- `UseIndicatorFilters` – Interruptor maestro para todos los filtros.
- `UseAdxFilter`, `AdxPeriod`, `AdxThreshold` – ADX configuración de confirmación.
- `UseMacdFilter`, `MacdFast`, `MacdSlow`, `MacdSignal` – MACD configuración de confirmación.
- `UseRsiFilter`, `RsiPeriod` – RSI configuración de confirmación.
- `UseCciFilter`, `CciPeriod` – CCI configuración de confirmación.

### Especialistas en perceptrones
- `ShortMaPeriod`, `ShortStep`, `ShortX1…ShortX4`, `ShortThreshold` – Configuración corta del perceptrón.
- `LongMaPeriod`, `LongStep`, `LongX1…LongX4`, `LongThreshold` – Configuración de perceptrón largo.
- `SupervisorMaPeriod`, `SupervisorStep`, `SupervisorX1…SupervisorX4`, `SupervisorThreshold` – Configuración del perceptrón supervisor.

Todos los parámetros numéricos reflejan los valores predeterminados de MQL, lo que permite un comportamiento similar entre el asesor experto original y este puerto StockSharp al tiempo que expone la configuración a través del sistema `StrategyParam` para campañas de optimización.
