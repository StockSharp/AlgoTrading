# Estratégia de Breakeven com Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que demonstra como mover o stop loss para o ponto de equilíbrio (breakeven) e depois segui-lo conforme o preço avança.
A estratégia entra em uma posição comprada e a gerencia em duas fases:
1. Depois que o preço ganha `BreakevenPlus` pontos, o stop é movido para `BreakevenStep` pontos acima da entrada.
2. Quando o preço continua com `TrailingPlus` pontos de lucro acima do stop, o stop segue o preço a `TrailingStep` pontos de distância.

A lógica é simétrica para posições vendidas, se uma for aberta manualmente.

## Detalhes

- **Critérios de entrada**: Abre uma posição comprada na primeira vela concluída.
- **Comprado/Vendido**: Ambos (o exemplo usa comprado).
- **Critérios de saída**: O preço cruza o trailing stop.
- **Stops**: Breakeven e trailing stop.
- **Valores padrão**:
  - `BreakevenPlus` = 5
  - `BreakevenStep` = 2
  - `TrailingPlus` = 3
  - `TrailingStep` = 1
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Gestão de stops
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Breakeven, trailing
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
