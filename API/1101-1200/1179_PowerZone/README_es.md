# Estrategia PowerZone
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia identifica bloques de órdenes "PowerZone" creados por una vela bajista seguida de velas alcistas consecutivas (y viceversa). Una ruptura por encima de la zona alcista activa una operación larga, mientras que una ruptura por debajo de la zona bajista abre una corta. Los objetivos y stops se basan en el rango de la zona.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Vela bajista hace `Periods+1` barras seguida de `Periods` velas alcistas y el precio rompe por encima del máximo de la zona.
  - **Corto**: Vela alcista hace `Periods+1` barras seguida de `Periods` velas bajistas y el precio rompe por debajo del mínimo de la zona.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: Take profit y stop loss como múltiplos del rango de la zona.
- **Indicadores**: Ninguno.
- **Valores predeterminados**:
  - `Periods` = 5
  - `Threshold` = 0
  - `UseWicks` = false
  - `Take Profit Factor` = 1.5
  - `Stop Loss Factor` = 1
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Moderado

## Cálculo y ejecución

C# y Python usan velas terminadas. En la barra de decisión `t`, el ancla es `t - Periods - 1`; las siguientes `Periods` barras hasta `t - 1` deben cerrar todas estrictamente por encima de la apertura para zona alcista, o por debajo para bajista. El ancla tiene color estrictamente opuesto; los dojis no califican. La vela actual no confirma la zona, que nunca se conoce antes de terminar la serie.

Exija `100 * abs(Close[t - 1] - Close del ancla) / abs(Close del ancla) >= Threshold`; un cierre del ancla cero es inválido. La zona es su cuerpo `[min(Open, Close), max(Open, Close)]` o `[Low, High]` con `UseWicks=true`. Los extremos de confirmación no amplían la zona; la anchura debe ser positiva. Mantenga por separado las últimas zonas alcista y bajista hasta reemplazarlas, con una sola entrada aceptada por zona.

Largo requiere `Close actual > máximo de zona alcista`; corto, `Close < mínimo de zona bajista`. Igualdad mantiene la posición. Una zona nueva puede entrar en su primera vela de decisión aunque el precio ya esté fuera. Entre con `Volume` desde cero o invierta completamente con `Volume + abs(Position)`, sin aumentar el mismo lado. Si ambas direcciones califican desde cero, largo tiene prioridad. Órdenes pendientes bloquean otras órdenes. No hay EMA, espera obligatoria ni otros indicadores.

Al aceptar la entrada, congele `anchura * StopLossFactor` y `anchura * TakeProfitFactor` alrededor de la ejecución real. Aumentos parciales del mismo lado usan base ponderada por volumen; reducir conserva la base e invertir la reinicia. Zonas posteriores no mueven las distancias. La protección local comprueba bid ejecutable para largos, ask para cortos y cierres de velas; cierra toda la cantidad restante a mercado, con prioridad sobre entradas en esa vela. Tocar máximos/mínimos no supone ejecuciones intrabar; saltos pueden alterar el precio ejecutado.

Valores compartidos: parámetros API `TakeProfitFactor=1.5`, `StopLossFactor=1`, `CandleType` de cinco minutos. El parámetro publicado es `UseWicks`; la propiedad C# es `IsUseWicks`. `Periods` y factores positivos; `Threshold` es un porcentaje no negativo. Estas definiciones precisan las reglas de umbral, límites, consumo único y base de ejecución antes no especificadas.
