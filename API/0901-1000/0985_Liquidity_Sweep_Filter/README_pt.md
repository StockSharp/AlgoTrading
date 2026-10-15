# Estratégia de Filtro de Varredura de Liquidez
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia seguidora de tendência usa Bandas de Bollinger para detectar a direção do mercado e monitora o volume para possíveis varreduras de liquidez. Uma posição é aberta quando a tendência muda para alta ou baixa dependendo do modo de negociação selecionado.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A tendência se torna altista e o modo permite operações compradas.
  - **Vendido**: A tendência se torna baixista e o modo permite operações vendidas.
- **Comprado/Vendido**: Configurável através do modo de negociação.
- **Critérios de saída**:
  - **Comprado**: A tendência se torna baixista ou o modo proíbe comprado.
  - **Vendido**: A tendência se torna altista ou o modo proíbe vendido.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Length` = 12.
  - `Multiplier` = 2.0.
  - `Major Sweep Threshold` = 50.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

