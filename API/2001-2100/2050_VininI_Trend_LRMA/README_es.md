# Estrategia VininI Trend LRMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sigue el extremo `L` de una regresión lineal móvil de cierres completos. Padrón: `CandleType = 4 horas`, `Period = 13`, `UpLevel = 10`, `DnLevel = -10`, `Mode = Breakdown`. Espera indicadores formados y permiso para operar.

En **Breakdown**, los umbrales corresponden al oscilador relativo `O = 100 × (Close − L) / L`, en porcentaje. Compra con `previousO <= UpLevel && O > UpLevel`; vende con `previousO >= DnLevel && O < DnLevel`. No son niveles absolutos de precio LRMA.

En **Twist**, compra cuando la pendiente del extremo pasa de dirección negativa a positiva y vende en el cambio inverso. Un tramo plano conserva la dirección anterior no nula.

Compra con `Position <= 0` y vende con `Position >= 0`. Primero cierra la posición contraria ejecutada y después abre `Volume` en la nueva dirección mediante órdenes a mercado. No aumenta posiciones del mismo lado. Sin stop-loss ni objetivo de beneficios.
