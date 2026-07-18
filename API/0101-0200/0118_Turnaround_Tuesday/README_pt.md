# Estratégia Turnaround Tuesday
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Turnaround Tuesday refere-se à tendência de mercados que caíram na segunda-feira de se recuperarem no dia seguinte.
O efeito é frequentemente atribuído a traders que reagem de forma exagerada após o fim de semana e depois invertem o curso.

Os testes indicam um retorno anual médio de aproximadamente 91%. Funciona melhor no mercado de ações.

Esta estratégia compra na abertura de terça-feira quando a segunda-feira foi de baixa, mantendo a posição apenas durante a sessão ou até que um objetivo de lucro modesto seja alcançado.

Os stops são apertados para proteger contra fraqueza continuada caso o rebote não se desenvolva.

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

