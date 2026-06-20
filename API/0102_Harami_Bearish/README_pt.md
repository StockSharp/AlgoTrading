# Estratégia Harami de Baixa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
O Harami de Baixa é o inverso da versão de alta e aparece após uma subida.
Uma vela pequena se forma completamente dentro da barra de alta anterior, sugerindo que o impulso de alta está perdendo força.

Os testes indicam um retorno anual médio de aproximadamente 43%. Funciona melhor no mercado de ações.

A estratégia vende a descoberto quando essa vela interior fecha, apostando em uma reversão à medida que os compradores perdem convicção.

Um stop percentual acima do máximo do padrão limita o risco e a operação é encerrada se o preço romper para novos máximos.

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

