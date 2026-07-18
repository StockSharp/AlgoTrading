# Estratégia de Convergência de Preço
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia estima a probabilidade de o preço subir ou cair comparando a soma dos valores OHLC4 de velas de alta e de baixa. Uma posição comprada é aberta quando a probabilidade de subida supera 50%, e uma posição vendida quando a probabilidade de queda supera 50%.

Os testes indicam um retorno anual médio de aproximadamente 37%. Tem melhor desempenho no mercado de criptomoedas.

A estratégia pode operar sobre todo o histórico ou em uma janela deslizante definida pelo parâmetro `Range`. O valor OHLC4 de cada vela é usado para ponderar as contribuições dos movimentos de alta e baixa.

## Detalhes

- **Critérios de entrada**: Uma probabilidade de subida acima de 50% aciona uma entrada comprada; uma probabilidade de queda acima de 50% aciona uma entrada vendida.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `FullHistory` = true
  - `Range` = 200
  - `CandleType` = 1 minute
- **Filtros**:
  - Categoria: Estatístico
  - Direção: Ambos
  - Indicadores: Personalizado
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
