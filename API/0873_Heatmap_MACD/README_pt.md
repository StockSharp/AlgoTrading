# Heatmap MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Heatmap MACD opera quando os histogramas MACD de cinco períodos se alinham. Uma posição comprada é aberta quando todos os histogramas se tornam positivos, e uma posição vendida quando todos se tornam negativos. Opcionalmente, a posição pode ser fechada quando qualquer histograma vira contra a operação.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: Histograma MACD > 0 em todos os cinco períodos e anteriormente não todos positivos.
  - **Vendido**: Histograma MACD < 0 em todos os cinco períodos e anteriormente não todos negativos.
- **Critérios de saída**: Sinal oposto ou fechamento opcional na direção contrária.
- **Stops**: Nenhum por padrão.
- **Valores padrão**:
  - `FastLength` = 9
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `TimeFrame1` = tf(60)
  - `TimeFrame2` = tf(120)
  - `TimeFrame3` = tf(240)
  - `TimeFrame4` = tf(240)
  - `TimeFrame5` = tf(480)
  - `CloseOnOpposite` = false
- **Filtros**:
  - Categoria: Tendência
  - Direção: Comprado e Vendido
  - Indicadores: MACD
  - Stops: Não
  - Complexidade: Básico
  - Período: Multi-período
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
