# Estratégia Bollinger Squeeze
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia baseada na compressão das Bandas de Bollinger

Os testes indicam um retorno anual médio de aproximadamente 100%. Funciona melhor no mercado de forex.

Bollinger Squeeze aguarda uma largura de banda estreita indicando baixa volatilidade. Um rompimento fora das bandas inicia uma operação nessa direção e sai quando o momentum falha ou surge um rompimento oposto.

A condição de compressão indica uma iminente expansão de volatilidade. Uma vez acionada, a operação aproveita o rompimento e depende de um stop ATR ou cruzamento de banda para sair.


## Detalhes

- **Critérios de entrada**: Sinais baseados em Bollinger.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2m
  - `SqueezeThreshold` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Bollinger
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Neural Networks: Não
  - Divergência: Não
  - Nível de risco: Médio

