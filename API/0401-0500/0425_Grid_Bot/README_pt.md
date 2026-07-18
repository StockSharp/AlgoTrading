# Estratégia Grid Bot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O grid bot divide um intervalo de preços predefinido em níveis iguais e opera as oscilações entre eles. Quando o preço deriva para a metade inferior da grade, a estratégia acumula posições compradas, vendendo-as quando o preço retorna à metade superior. Esta abordagem prospera em mercados laterais com limites claros.

Nenhum viés direcional é assumido; o bot simplesmente reage à proximidade das linhas da grade.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: o preço toca um nível na metade inferior sem posição comprada
  - **Vendido**: o preço toca um nível na metade superior sem posição vendida
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - O sinal de entrada oposto fecha a posição existente
- **Stops**: Nenhum
- **Valores padrão**:
  - `UpperLimit` = 48000
  - `LowerLimit` = 45000
  - `GridCount` = 10
- **Filtros**:
  - Categoria: Range trading
  - Direção: Ambos
  - Indicadores: Price levels
  - Stops: Não
  - Complexidade: Baixo
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
