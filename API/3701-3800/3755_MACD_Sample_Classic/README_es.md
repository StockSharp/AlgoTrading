# MACD Ejemplo de estrategia clásica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia reproduce el asesor experto MetaTrader 4 "MACD Muestra" utilizando el API de alto nivel de StockSharp. Opera en ambas direcciones en un solo instrumento y refleja la lógica original: realiza operaciones cuando la línea MACD cruza su línea de señal en el lado correcto de cero mientras una tendencia EMA confirma la dirección. Las órdenes de protección se convierten al administrador de riesgos integrado de StockSharp con paradas finales opcionales.

## Lógica de trading

1. Espere al menos 100 velas terminadas para que MACD y EMA contengan suficiente historial.
2. Calcula un MACD estándar (12, 26, 9) junto con su línea de señal y una media móvil exponencial de 26 períodos que actúa como filtro direccional.
3. **Entrada larga**: permitida solo cuando no existe ninguna posición. El MACD debe estar por debajo de cero pero cruzando por encima de la línea de señal, el valor anterior de MACD estaba por debajo de su señal, el valor absoluto de MACD excede el umbral configurable `MacdOpenLevel` (en puntos de precio) y la tendencia EMA está aumentando.
4. **Entrada corta** – la configuración simétrica: MACD por encima del cruce por cero por debajo de su señal, el MACD anterior estaba por encima de la señal, el valor actual excede el umbral `MacdOpenLevel` y la tendencia EMA está cayendo.
5. **Salida larga**: cuando MACD vuelve a cruzar por debajo de la señal en el lado positivo de cero y el valor está por encima de `MacdCloseLevel`. La posición también puede cerrarse antes mediante el trailing stop o take-profit gestionado por `StartProtection`.
6. **Salida corta** – cuando MACD vuelve a cruzar la señal en el lado negativo y el valor absoluto de MACD excede `MacdCloseLevel`, o por los módulos de protección.

La estrategia nunca ocupa más de una posición a la vez. Cada entrada utiliza órdenes de mercado dimensionadas por la propiedad `Volume`. La lógica de protección se basa en el controlador de riesgos de StockSharp, por lo que las distancias de obtención de beneficios y los trailingstops permanecen sincronizados con el tamaño del tick del instrumento.

## Parámetros

| Nombre | Descripción | Predeterminado | Notas |
| --- | --- | --- | --- |
| `FastEmaPeriod` | Período EMA rápida utilizado por MACD. | 12 | Rango optimizable 6…18.
| `SlowEmaPeriod` | Período lento de EMA utilizado por MACD. | 26 | Rango optimizable 20…32.
| `SignalPeriod` | Periodo de señal EMA dentro de MACD. | 9 | Rango optimizable 5…13.
| `TrendMaPeriod` | EMA longitud para el filtro direccional. | 26 | Rango optimizable 20…40.
| `MacdOpenLevel` | Umbral de entrada expresado en MACD puntos (escalones de precio). | 3 | Equivalente a `MACDOpenLevel` en código MT4.
| `MacdCloseLevel` | Umbral de salida expresado en MACD puntos. | 2 | Equivalente a `MACDCloseLevel`.
| `TakeProfitPoints` | Obtenga ganancias en puntos de precio (multiplicados por el tamaño del tick del instrumento). | 50 | Establezca en 0 para deshabilitar la toma de ganancias.
| `TrailingStopPoints` | Trailing stop en puntos de precio. | 30 | Establezca en 0 para desactivar el trailing stop.
| `CandleType` | Serie de velas utilizadas para actualizaciones de indicadores. | marco de tiempo de 5 minutos | Admite cualquier tipo de vela StockSharp.

## Notas de implementación

- Los indicadores MACD y EMA están vinculados a la suscripción de vela a través de `BindEx`/`Bind`, lo que permite a StockSharp alimentar valores listos para usar sin almacenamiento en caché manual.
- Las posiciones se abren solo cuando la plataforma informa `IsFormedAndOnlineAndAllowTrading()`, lo que impide realizar transacciones mientras los datos históricos aún se están cargando o la conexión está fuera de línea.
- Todos los umbrales que se refieren a "puntos" se escalan automáticamente según el paso del precio del instrumento, imitando la constante `Point` de MetaTrader.
- `StartProtection` convierte la toma de ganancias fija y el trailing stop de MetaTrader en órdenes de protección del lado del mercado. Habilite o deshabilite cada módulo cambiando el parámetro correspondiente.
- El registro extenso (`LogInfo`) documenta cada decisión comercial, lo que simplifica la comparación con el asesor experto original durante la validación de la migración.

## Consejos de uso

- El EA original apunta a las principales divisas en marcos de tiempo intradiarios. Comience con símbolos similares y ajuste los parámetros si el instrumento utiliza un tamaño de marca diferente.
- Al probar símbolos con valores de tick exóticos, verifique que `Security.PriceStep` esté configurado; de lo contrario, se utilizará el valor predeterminado 1.0.
- Combínelo con las funciones de protección de cartera de StockSharp si necesita administración de dinero a nivel de cuenta más allá de las paradas por posición.

## Etiquetas

- Seguimiento de tendencias
- Impulso
- MACD cruce
- Intradiario (5 minutos predeterminado)
- Trailing stop + toma de ganancias
