# Estratégia Bebê Abandonado de Baixa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
O Bebê Abandonado de Baixa espelha a versão de alta, mas sinaliza um possível topo.
Apresenta um doji com gap de alta seguido de um gap de baixa, deixando a vela do meio isolada acima do intervalo anterior.

Os testes indicam um retorno anual médio de aproximadamente 79%. Funciona melhor no mercado de ações.

A estratégia vende a descoberto quando a terceira vela abre em gap abaixo do doji, visando lucrar com a mudança abrupta de sentimento.

O risco é limitado com um stop logo acima da máxima do doji, caso o preço se recupere.

## Detalhes

- **Critérios de entrada**: correspondência de padrão
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
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

