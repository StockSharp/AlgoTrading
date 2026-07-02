# Harami CCI Confirmación
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La confirmación de Harami CCI es un puerto StockSharp de alto nivel del MetaTrader 5 asesor experto `Expert_ABH_BH_CCI`. El EA original intercambia los patrones de reversión Bullish Harami y Bearish Harami de dos velas. Antes de ingresar a una operación, exige la confirmación de un oscilador del índice de canal de productos básicos (CCI) y mide el tamaño del cuerpo de la vela frente a un promedio móvil para garantizar que la vela más grande realmente domine el rango. La conversión StockSharp mantiene la misma lógica de confirmación, procesa solo velas completadas y utiliza el módulo de protección integrado de la plataforma para la seguridad de los pedidos.

## Lógica estratégica
### Detección de patrones
* **Cálculo del cuerpo promedio**: mantiene un promedio móvil de los cuerpos absolutos de las velas durante las últimas *N* barras (predeterminado 5). Esto refleja la clase auxiliar MetaTrader que suaviza el tamaño de la vela y la referencia de tendencia.
* **Harami alcista**: requiere que la vela anterior sea alcista, la vela anterior sea bajista con un cuerpo más largo que el promedio y que el cuerpo alcista permanezca dentro del rango bajista. El punto medio de la vela anterior también debe ubicarse por debajo del promedio móvil de cierres, lo que confirma una tendencia bajista.
* **Harami bajista** – condiciones reflejadas: la vela anterior debe ser bajista, la vela anterior alcista y larga, el cuerpo bajista debe estar contenido dentro del rango alcista y el punto medio debe estar por encima del promedio móvil de cierre para confirmar una tendencia alcista.

### CCI confirmación
* **Filtro de entrada**: la estrategia verifica el valor CCI de la vela completada más recientemente (cambio 1). Las operaciones largas requieren que CCI esté por debajo de `-EntryThreshold` (predeterminado 50), mientras que las operaciones cortas exigen un valor superior a `+EntryThreshold`.
* **Banda de salida**: el historial de CCI se monitorea para detectar cruces de ±`ExitBand` (80 predeterminado). Cuando el indicador sube hasta `-ExitBand`, cualquier posición corta abierta se cierra. Cuando cae por debajo de `+ExitBand`, la exposición larga existente se cierra. Esto reproduce los "votos" utilizados por el experto MetaTrader para aplanar posiciones.

### Gestión comercial
* **Reversiones**: si se confirma la configuración opuesta de Harami mientras la estrategia ya mantiene una posición, negociará suficiente volumen para cerrar la exposición existente y abrir la nueva dirección.
* **Protección**: `StartProtection()` está activado para que los usuarios puedan adjuntar configuraciones de límite de pérdidas o toma de ganancias a través de la interfaz de usuario de StockSharp si lo desean. No se aplican paradas fijas de forma predeterminada para permanecer alineado con la fuente EA, que dependía de la configuración manual de administración del dinero.

## Parámetros
* **Volumen de pedidos**: volumen base enviado con cada entrada al mercado. Se agrega volumen adicional automáticamente para cerrar la posición opuesta cuando ocurre una reversión.
* **CCI Período** – duración del oscilador del índice del canal de productos básicos.
* **Promedio corporal**: número de velas históricas utilizadas para promediar los tamaños corporales y los precios de cierre.
* **CCI Entrada**: valor mínimo absoluto CCI necesario para aceptar una señal Harami.
* **CCI Banda de salida**: magnitud de la banda que define las reglas de salida de cruce CCI.
* **Tipo de vela**: período de tiempo utilizado para las velas (predeterminado: período de 1 hora).

## Notas adicionales
* Todos los cálculos se ejecutan en velas completas proporcionadas por `SubscribeCandles`. Las señales intrabar se ignoran intencionalmente para que coincidan con el modelo de ejecución MetaTrader.
* La estrategia mantiene un breve historial deslizante de velas y valores CCI para evaluar las reglas de Harami sin recrear los buffers de indicador completos.
* En esta carpeta sólo se proporciona la implementación de C#; No existe una versión de Python para esta conversión.
