# Estratégia de Scalping com Williams %R, MACD e SMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de scalping que utiliza Williams %R, o histograma MACD e uma média móvel simples em velas de um minuto.

## Detalhes

- **Critérios de entrada**: Williams %R cruza os níveis de ativação e o histograma MACD muda de sinal na direção da tendência.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: O histograma inverte sua direção.
- **Stops**: Não.
- **Valores padrão**:
  - `WilliamsLength` = 140
  - `MacdFast` = 24
  - `MacdSlow` = 52
  - `MacdSignal` = 9
  - `SmaLength` = 7
