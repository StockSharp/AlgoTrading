# Estrategia de Desaceleración WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Desaceleración WPR utiliza el oscilador Williams %R para detectar reversiones cuando el momentum se estanca cerca de niveles extremos. Se produce una desaceleración cuando el valor actual de Williams %R difiere del valor anterior en menos de un punto. Cuando dicha desaceleración aparece por encima del umbral superior, la estrategia cierra posiciones cortas y opcionalmente abre una posición larga. Una desaceleración por debajo del umbral inferior cierra posiciones largas y opcionalmente abre una posición corta.

## Reglas de Entrada y Salida
- **Entrada larga**: Williams %R está por encima de `LevelMax` y se cumple la condición de desaceleración. Las posiciones cortas pueden cerrarse si está permitido.
- **Entrada corta**: Williams %R está por debajo de `LevelMin` y se cumple la condición de desaceleración. Las posiciones largas pueden cerrarse si está permitido.
- **Salida larga**: Desencadenada por una señal de entrada corta cuando `BuyPosClose` está habilitado.
- **Salida corta**: Desencadenada por una señal de entrada larga cuando `SellPosClose` está habilitado.

## Parámetros
- `WprPeriod` – período para calcular Williams %R.
- `LevelMax` – nivel de señal superior (predeterminado -20) que marca la zona de sobrecompra.
- `LevelMin` – nivel de señal inferior (predeterminado -80) que marca la zona de sobreventa.
- `SeekSlowdown` – habilita la detección de desaceleración entre valores consecutivos de Williams %R.
- `BuyPosOpen` – permitir apertura de posiciones largas.
- `SellPosOpen` – permitir apertura de posiciones cortas.
- `BuyPosClose` – permitir cierre de posiciones largas en señales de venta.
- `SellPosClose` – permitir cierre de posiciones cortas en señales de compra.
- `CandleType` – tipo de vela utilizado para los cálculos del indicador (predeterminado velas de 6 horas).

## Notas
La estrategia se centra únicamente en la lógica de desaceleración de Williams %R del experto MQL5 original. Las alertas, la gestión del dinero y otras características auxiliares se omiten por claridad. La funcionalidad de stop-loss y take-profit puede añadirse manualmente si es necesario.
