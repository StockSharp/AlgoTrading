# Estrategia de Captura de Mercado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia de Captura de Mercado reproduce la lógica del experto original de MetaTrader 5. El algoritmo construye una cuadrícula dinámica alrededor de un precio central en movimiento y abre operaciones estilo cobertura cada vez que el precio oscila alrededor de este centro. Las posiciones se distribuyen por encima y por debajo del centro con objetivos de beneficio fijos, mientras que los hitos de equidad de la cuenta controlan cuándo liquidar las operaciones con pérdidas más grandes.

## Reglas de trading
- **Línea central** – la estrategia almacena un nivel central interno que comienza en el cierre de la primera vela procesada. Cuando el mercado se mueve más allá del espaciado de cuadrícula configurado, el centro se desplaza paso a paso para seguir el precio.
- **Corto inicial** – se puede abrir una posición corta opcional inmediatamente después del inicio para coincidir con el comportamiento del script MQL.
- **Entradas largas** – se permite una operación larga cuando el último cierre está por encima del centro y la vela anterior operó por debajo de él. Una comprobación de proximidad garantiza que ninguna otra operación larga esté activa cerca del mismo nivel.
- **Entradas cortas** – se permite una operación corta cuando el último cierre está por debajo del centro y la vela anterior operó por encima de él. El mismo filtro de proximidad evita apilar cortos idénticos.
- **Take profit** – cada operación almacena un nivel objetivo que está a un múltiplo fijo del paso de precio del instrumento desde el precio de entrada. Los máximos de las velas (para largos) o mínimos (para cortos) que alcanzan el objetivo desencadenan una salida de mercado.
- **Gestión de equidad** – la estrategia monitorea la equidad del portafolio. Después de una ganancia porcentual configurable cierra una serie de las operaciones con peor desempeño para asegurar ganancias. Otro umbral porcentual define cuándo reducir el riesgo durante la caída liquidando operaciones perdedoras. Cada vez que un umbral se activa la línea base de equidad se recalcula.

## Parámetros
- `Enable Long` / `Enable Short` – permitir o bloquear operaciones en cada dirección.
- `Grid Steps` – espaciado entre niveles de cuadrícula medido en pasos de precio.
- `Take Profit Steps` – distancia de take profit medida en pasos de precio.
- `Open Initial Short` – habilitar la primera orden corta colocada justo después del inicio.
- `Use Equity Target` – activar la regla de crecimiento de equidad para recortar operaciones perdedoras.
- `Track Drawdown` – activar la regla de reducción para recortar operaciones perdedoras durante la caída.
- `Equity Gain %` / `Equity Loss %` – porcentajes de cambio de equidad que activan las reglas anteriores.
- `Loss Trades Up` / `Loss Trades Down` – número máximo de operaciones perdedoras cerradas cuando se activa cada regla.
- `Candle Type` – marco temporal o tipo de vela personalizado utilizado para el proceso de decisión.
- `Volume` (propiedad de estrategia) – tamaño de operación para cada orden de mercado.

## Notas
- La estrategia mantiene un registro interno de operaciones abiertas para imitar el estilo de cobertura del experto original mientras trabaja con el modelo de posición neta de StockSharp.
- Los parámetros de distancia se multiplican por el paso de precio del valor; asegúrese de que el instrumento seleccionado exponga un valor `PriceStep` válido.
- La lógica opera solo en velas finalizadas. Seleccione un tipo de vela que coincida con el horizonte de trading previsto, desde cuadrículas de muy corto plazo hasta cuadrículas de swing más amplias.
