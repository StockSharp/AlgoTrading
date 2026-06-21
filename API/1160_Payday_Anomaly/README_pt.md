# Estratégia de Anomalia do Dia de Pagamento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia abre uma posição comprada nos dias de pagamento selecionados (1, 2, 16 e 31 de cada mês) e fecha a posição no dia seguinte.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: abrir uma posição comprada nos dias selecionados do mês.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - fechar a posição comprada quando o dia não estiver selecionado.
- **Stops**: Não.
- **Valores padrão**:
  - `Trade1st` = true.
  - `Trade2nd` = true.
  - `Trade16th` = true.
  - `Trade31st` = true.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filtros**:
  - Categoria: Sazonalidade
  - Direção: Comprado
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
