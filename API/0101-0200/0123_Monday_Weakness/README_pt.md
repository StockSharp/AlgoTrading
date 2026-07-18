# Estratégia de Fraqueza de Segunda-feira
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A Fraqueza de Segunda-feira observa que as ações frequentemente abrem mais baixas após o fim de semana, enquanto os traders digerem notícias e reposicionam carteiras.
A pressão baixista de curto prazo pode surgir no início da semana antes que os mercados se estabilizem.

Os testes indicam um retorno anual médio de aproximadamente 106%. Funciona melhor no mercado de ações.

A estratégia vende a descoberto na abertura de segunda-feira e cobre ao fechamento, buscando lucrar com essa fraqueza inicial.

Os stops são mantidos estreitos para evitar perdas caso o mercado contrarie a tendência e suba.

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

