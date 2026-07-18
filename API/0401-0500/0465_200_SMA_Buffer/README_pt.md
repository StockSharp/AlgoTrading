# Estratégia de Buffer de 200 SMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Buffer de 200 SMA opera com base na distância do preço em relação a uma média móvel simples de longo prazo. Compra quando o fechamento sobe uma certa porcentagem acima da SMA e sai quando o preço cai uma porcentagem definida abaixo dela. A abordagem visa capturar o momentum de longo prazo permitindo um buffer ao redor da média móvel.

## Detalhes

- **Critérios de entrada**:
  - Preço de fechamento > SMA * (1 + Entry %).
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - Preço de fechamento < SMA * (1 - Exit %).
- **Stops**: Nenhum.
- **Valores padrão**:
  - `SmaLength` = 200
  - `EntryPercent` = 5
  - `ExitPercent` = 3
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Long
  - Indicadores: SMA
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
