# Estratégia de Número Retroativo de Barras
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia mantém uma posição comprada apenas durante as barras mais recentes contadas regressivamente a partir do tempo atual. Demonstra como restringir o trading a uma janela histórica móvel.

## Detalhes

- **Critérios de entrada**: O tempo da vela está dentro das últimas *N* barras a partir do horário de início.
- **Critérios de saída**: O tempo da vela cai fora desta janela.
- **Comprado/Vendido**: Somente comprado.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Bar count` = 50
  - `Candle type` = velas de 1 minuto
- **Filtros**:
  - Categoria: Baseado em tempo
  - Direção: Comprado
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Simples
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
