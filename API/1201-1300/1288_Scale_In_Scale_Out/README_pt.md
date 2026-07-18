# Estratégia de Acumulação e Redução Gradual de Posição
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia constrói gradualmente uma posição investindo uma porcentagem fixa do capital disponível em cada barra. Quando o valor da posição atinge um nível de lucro configurável, ela vende uma parte da posição e opcionalmente reserva parte do lucro realizado.

## Detalhes

- **Critérios de entrada**: Comprar sempre que houver capital disponível.
- **Critérios de saída**: Vender quando o percentual de lucro ultrapassa o limiar.
- **Comprado/Vendido**: Somente comprado.
- **Valores padrão**:
  - `Buy Scaling Size %` = 2
  - `Take Profit Level %` = 50
  - `Take Profit Size %` = 1
  - `Retain Profit Portion %` = 50
  - `Minimum Position Value` = 200000
  - `Minimum Buy Value` = 100
- **Filtros**:
  - Categoria: Outro
  - Direção: Comprado
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
