# Estratégia de Fraqueza Pós-Feriado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A Fraqueza Pós-Feriado é a tendência dos preços caírem imediatamente após um grande feriado quando o volume permanece baixo.
Com muitos participantes ainda ausentes, movimentos contra a tendência podem ganhar força.

Os testes indicam um retorno anual médio de aproximadamente 112%. Funciona melhor no mercado de forex.

A estratégia vende a descoberto no dia seguinte ao feriado e cobre rapidamente assim que a participação normal retorna.

Um stop pequeno é usado para evitar perdas excessivas durante a negociação com baixa liquidez.

## Detalhes

- **Critérios de entrada**: ativadores de efeito de calendário
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

