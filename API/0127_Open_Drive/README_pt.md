# Estratégia Open Drive
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
O Open Drive refere-se a um forte movimento direcional logo na abertura, frequentemente após um catalisador de notícias overnight.
Os traders buscam alto volume e momentum sustentado nos primeiros minutos.

Os testes indicam um retorno anual médio de aproximadamente 118%. Funciona melhor no mercado de ações.

A estratégia acompanha esse momentum, entrando comprado ou vendido dentro do intervalo de abertura e arrastando um stop conforme o preço se estende.

As posições são fechadas rapidamente se o impulso estacionar, mantendo as perdas pequenas durante aberturas agitadas.

## Detalhes

- **Critérios de entrada**: sinal de indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Intradiário
  - Direção: Ambos
  - Indicadores: Price Action
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

