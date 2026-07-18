# Estrategia Expotest
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia Expotest es una conversión directa a StockSharp del asesor experto original `Expotest.mq5`. Opera un único instrumento usando el indicador Parabolic SAR y una regla de gestión monetaria simple inspirada en el martingale. La estrategia abre solo una posición a la vez y depende de niveles de stop-loss y take-profit predefinidos para las salidas.

## Lógica de trading
- **Indicador**: Parabolic SAR calculado en la serie de velas seleccionada. Tanto el factor de aceleración (`SarStep`) como la aceleración máxima (`SarMaximum`) son configurables.
- **Condiciones de entrada**: Cuando no hay posición abierta, la estrategia verifica la última vela cerrada.
  - Si el valor del Parabolic SAR está por debajo o igual al precio de cierre, se inicia una posición larga.
  - Si el valor del Parabolic SAR está por encima o igual al precio de cierre, se inicia una posición corta.
- **Condiciones de salida**: Los niveles de stop-loss y take-profit se colocan a una distancia fija del precio de entrada, medida en pasos de precio. Durante cada nueva vela, la estrategia monitorea si el rango de la vela toca cualquiera de los niveles y cierra la posición en consecuencia. El tipo de salida (ganancia o pérdida) se recuerda para futuras decisiones de dimensionamiento de posición.

## Dimensionamiento de posición
- **Volumen base**: Definido por el parámetro `FixedVolume` cuando es mayor que cero. De lo contrario, la estrategia estima el tamaño a partir de los valores `RiskPercent` y `StopLossPoints` usando el capital actual del portafolio. Si ningún método devuelve un tamaño válido, se usa el `Strategy.Volume` predeterminado.
- **Paso martingale**: Después de una operación perdedora, el siguiente tamaño de posición se duplica en comparación con el volumen de la posición perdedora. Una salida rentable restablece el multiplicador y la siguiente orden usa el volumen base nuevamente.

## Parámetros configurables
- `CandleType` – Tipo de datos para la agregación de velas (marco temporal u otro formato de vela).
- `SarStep` – Factor de aceleración inicial para el Parabolic SAR.
- `SarMaximum` – Factor de aceleración máximo para el Parabolic SAR.
- `StopLossPoints` – Distancia del stop-loss desde la entrada expresada en pasos de precio.
- `TakeProfitPoints` – Distancia del take-profit desde la entrada expresada en pasos de precio.
- `RiskPercent` – Porcentaje del capital del portafolio a arriesgar por operación cuando el dimensionamiento dinámico está habilitado.
- `FixedVolume` – Volumen de orden explícito. Establecer en `0` para habilitar el dimensionamiento basado en riesgo.

## Notas adicionales
- La estrategia procesa solo velas terminadas para mantenerse cerca de la implementación MQL original basada en ticks mientras permanece compatible con las suscripciones de StockSharp.
- Los niveles de protección se rastrean internamente en lugar de órdenes stop/limit separadas, lo que mantiene la lógica transparente y fácil de backtestear.
- La implementación Python se omite intencionalmente según lo solicitado.
