# Estratégia de Pivôs de Máximas e Mínimas de Swing [LV]
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Opera em torno de máximas e mínimas de swing confirmadas. Quando um pivô mínimo aparece, a estratégia coloca uma ordem limitada de compra no preço da barra do pivô e define alvos fixos de stop e take-profit. Pivôs máximos acionam configurações de venda. Um filtro de média móvel opcional pode restringir as operações à direção da tendência.

## Detalhes

- **Entradas**:
  - Comprimento do pivô.
  - Distância de stop-loss em ticks.
  - Distância de take-profit em ticks.
  - Segundo take-profit e opção de entrada dupla.
  - Tipo e comprimento do filtro de média móvel.
- **Comprado/Vendido**: Ambos.
- **Saída**: Stop fixo e até dois alvos de lucro.
- **Filtros**:
  - Categoria: Reconhecimento de padrões
  - Direção: Ambos
  - Indicadores: Média móvel
  - Stops: Fixo
  - Complexidade: Alto
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
