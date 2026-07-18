# Estratégia de Fade no Horário de Almoço
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A estratégia de Fade no Horário de Almoço visa reversões que se desenvolvem durante o período lento do meio-dia.
Após a sessão da manhã, as tendências costumam pausar ou recuar quando o volume cai no horário do almoço.

Os testes indicam um retorno anual médio de aproximadamente 127%. Funciona melhor no mercado de ações.

A estratégia vai contra o movimento da manhã por volta do meio-dia, entrando na direção contrária à tendência predominante e cobrindo antes que o volume retorne.

Um stop percentual gerencia o risco caso a tendência retome em vez de esmorecer.

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

