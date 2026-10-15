# Estratégia de Custo Médio em Dólares Otimizado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia acumula uma posição investindo uma quantia fixa de capital em intervalos regulares entre datas de início e fim definidas pelo usuário. Cada compra ocorre ao preço de fechamento do período selecionado independentemente do preço, implementando uma abordagem clássica de custo médio em dólares.

## Detalhes

- **Critérios de entrada**:
  - A cada intervalo (diário, semanal ou mensal) entre as datas de início e fim, a
    estratégia compra ao preço de fechamento pelo valor configurado.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - As posições são mantidas; nenhuma lógica de saída automática está incluída.
- **Stops**: Nenhum.
- **Valores padrão**:
  - Valor por período = 100.
  - Intervalo = Semanal.
  - Data de início = 2018-01-01, Data de fim = 2020-01-28.
- **Filtros**:
  - Categoria: Acumulação.
  - Direção: Comprado.
  - Indicadores: Nenhum.
  - Stops: Não.
  - Complexidade: Baixo.
  - Período: Qualquer.
  - Sazonalidade: Não.
  - Redes neurais: Não.
  - Divergência: Não.
  - Nível de risco: Baixo.
