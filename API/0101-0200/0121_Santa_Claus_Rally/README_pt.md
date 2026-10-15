# Estratégia Santa Claus Rally
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
O Santa Claus Rally descreve a tendência das ações de subir na última semana de dezembro até os primeiros dois dias de negociação de janeiro.
O otimismo das festas e o posicionamento de fim de ano podem impulsionar essa breve explosão de força.

Os testes indicam um retorno anual médio de aproximadamente 100%. Funciona melhor no mercado forex.

A estratégia compra no início do período e sai após o segundo dia de negociação do novo ano, visando capturar o impulso sazonal.

Os stops são mantidos pequenos para evitar grandes perdas caso o mercado não suba durante a janela.

## Detalhes

- **Critérios de entrada**: gatilhos de efeito calendário
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Sazonalidade
  - Direção: Ambos
  - Indicadores: Sazonalidade
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

