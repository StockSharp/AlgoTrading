# Estratégia Heiken-Ashi Suavizado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Velas Heiken-Ashi suavizadas com EMA destacam a aceleração nos movimentos de preço. Uma posição comprada é aberta quando uma vela de alta suavizada tem um corpo maior do que a anterior. A posição é fechada quando o corpo de baixa se expande.

## Detalhes

- **Critérios de entrada**: vela Heiken-Ashi suavizada de alta com corpo maior que a anterior
- **Comprado/Vendido**: Comprado
- **Critérios de saída**: o corpo de baixa se expande
- **Stops**: Não
- **Valores padrão**:
  - `EmaLength` = 40
- **Filtros**:
  - Categoria: Padrão
  - Direção: Comprado
  - Indicadores: EMA, Heikin-Ashi
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
