# Estratégia de Rompimento Diário com Sombra Diária
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera rompimentos diários usando as duas últimas velas diárias completas. Fecha qualquer posição aberta no início de cada novo dia.

## Detalhes

- **Critérios de entrada**:
  - Comprado: O dia anterior fecha acima do máximo do corpo da vela anterior e abre abaixo desse nível.
  - Vendido: O dia anterior fecha abaixo do mínimo do corpo da vela anterior e abre acima desse nível.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - A posição é fechada no início de um novo dia.
- **Stops**: Não.
- **Valores padrão**:
  - `CandleType` = 1 Day
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Baixo
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
