# Estrategia de Cobertura de Superposición Multicurrencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Conversión del asesor experto de MetaTrader 4 **"Multicurrency hedge example EA (overlay hedge)"** a la API de alto nivel de StockSharp.

## Descripción general
- Trabaja con un universo de símbolos forex suministrados por el usuario y monitorea todos los pares únicos.
- Calcula la correlación de Pearson deslizante y los ratios ATR para determinar qué símbolos se mueven juntos y cómo dimensionar ambas piernas.
- Construye superposiciones de precios sintéticas para detectar cuándo el instrumento principal se desvía de su socio correlacionado más allá de un umbral configurable.
- Abre bloques cubiertos (compra/venta, compra/compra, venta/compra, venta/venta) dependiendo del signo de correlación y la dirección de superposición.
- Cierra el bloque completo una vez que se alcanza un objetivo de take-profit mutuo en puntos o en moneda de la cartera.

## Flujo de trabajo
1. Suscribirse a velas completadas para cada instrumento del universo y almacenar los últimos valores de high/low/close.
2. Suscribirse a cotizaciones Level1 de cada instrumento para aplicar filtros de spread antes de enviar coberturas.
3. Una vez al día (por defecto a las 01:00 hora del servidor) reconstruir la lista de pares negociables:
   - Mantener sólo los pares donde la correlación absoluta está por encima del umbral configurado.
   - Calcular el ratio ATR para escalar el volumen de la pierna principal.
4. Para cada vela completada verificar la distancia de superposición:
   - Correlación positiva ⇒ comprar principal / vender secundaria cuando la desviación está por debajo de `-OverlayThreshold` puntos, vender principal / comprar secundaria cuando está por encima de `+OverlayThreshold` puntos.
   - Correlación negativa ⇒ comprar ambas piernas por debajo del umbral negativo, vender ambas piernas por encima del umbral positivo.
5. Rastrear bloques de cobertura abiertos y cerrarlos cuando el beneficio agregado alcance cualquiera de las condiciones de take-profit.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `Universe` | Colección de objetos `Security` para escanear. Necesita al menos dos entradas. | vacío |
| `CandleType` | Tipo de datos de velas usado para los cálculos. | Marco temporal de 1 minuto |
| `RangeLength` | Número de barras usadas para calcular envolventes de precios. | 400 |
| `CorrelationLookback` | Barras usadas para la correlación de Pearson. | 500 |
| `AtrLookback` | Barras usadas para el dimensionamiento del ratio ATR. | 200 |
| `CorrelationThreshold` | Correlación absoluta mínima para mantener un par (0–1). | 0.90 |
| `OverlayThreshold` | Distancia de superposición en puntos medida usando el paso del instrumento principal. | 100 |
| `TakeProfitByPoints` / `TakeProfitPoints` | Habilita y configura el take-profit mutuo basado en puntos. | true / 10 |
| `TakeProfitByCurrency` / `TakeProfitCurrency` | Habilita y configura el take-profit mutuo basado en moneda. | false / 10 |
| `MaxOpenPairs` | Máximo de bloques de cobertura abiertos simultáneamente. | 10 |
| `BaseVolume` | Volumen de la pierna secundaria (volumen de la pierna principal = `BaseVolume * ATR ratio`). | 1 |
| `RecalculationHour` | Hora del día en que se recalculan las correlaciones. | 1 |
| `MaxSpread` | Spread bid-ask máximo permitido por pierna (en puntos). | 10 |

## Requisitos de datos
- Velas históricas y en vivo para cada instrumento en `Universe` con el `CandleType` especificado.
- Actualizaciones de cotizaciones Level1 para cada instrumento para validar spreads.
- Información del portafolio para el registro de órdenes.

## Notas de uso
- La estrategia no auto-popula el universo; pasa los símbolos forex deseados antes de iniciar.
- Para imitar la lógica de dimensionamiento de MetaTrader, mantén `BaseVolume` igual al tamaño de lote de la pierna secundaria. El volumen de la pierna principal se escala automáticamente por el ratio ATR.
- Si los datos de spread no están disponibles, la estrategia omitirá nuevas entradas hasta que llegue la primera instantánea del libro de órdenes.
- La lógica de cierre estima el beneficio mutuo combinando el movimiento con signo de cada pierna usando el paso de precio del instrumento y el precio del paso.

## Diferencias con el EA original
- Usa suscripciones de StockSharp (`SubscribeCandles`, `SubscribeLevel1`) en lugar de polling basado en temporizador.
- La lógica de take-profit se implementa con información promediada del paso de precio en lugar de beneficio/comisión de operación bruta.
- Requiere un parámetro de universo explícito, permitiendo que la estrategia se ejecute en cualquier subconjunto de instrumentos admitidos por StockSharp.
- La ejecución de órdenes se realiza a través de órdenes de mercado de StockSharp con comentarios por cobertura para trazabilidad.
