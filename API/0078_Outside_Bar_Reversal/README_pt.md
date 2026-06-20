# Estratégia de Reversão com Barra Externa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma barra externa ocorre quando o intervalo de uma vela excede o da vela anterior, criando um breve aumento de volatilidade. Esta estratégia opera contra o movimento se a barra externa fechar na direção oposta à tendência anterior, esperando um retorno ao equilíbrio.

Os testes indicam um retorno anual médio de aproximadamente 121%. Funciona melhor no mercado de criptomoedas.

Quando uma barra externa se forma, o algoritmo determina se a vela é de alta ou de baixa. Uma barra externa de alta após uma queda abre uma posição comprada com stop abaixo da mínima da barra. Uma barra externa de baixa após uma alta aciona uma posição vendida com stop acima de sua máxima. As operações são encerradas se o preço posteriormente romper esse extremo.

A configuração busca reversões rápidas após um impulso exaustivo e é melhor utilizada quando os mercados estão agitados em vez de seguirem uma tendência forte.

## Detalhes

- **Critérios de entrada**: Barra externa fechando em sentido oposto ao movimento anterior.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Preço rompendo a máxima/mínima da barra externa ou stop-loss.
- **Stops**: Sim, colocados além do padrão.
- **Valores padrão**:
  - `CandleType` = 5 minute
  - `StopLossPercent` = 1
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Candlestick
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

