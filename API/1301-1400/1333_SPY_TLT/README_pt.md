# Estratégia SPY TLT
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia compra o ativo principal quando o preço do TLT cruza acima da sua SMA e sai quando o TLT fecha abaixo da SMA. A operação é permitida apenas dentro da janela de tempo especificada.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: TLT fecha acima da sua SMA dentro da janela de tempo.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - TLT fecha abaixo da sua SMA.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Start Time` = 2014-01-01
  - `End Time` = 2099-01-01
  - `TLT Symbol` = TLT
  - `SMA Length` = 20
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Somente comprado
  - Indicadores: SMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
