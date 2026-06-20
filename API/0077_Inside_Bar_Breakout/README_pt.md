# Estratégia de Rompimento de Barra Interna
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma barra interna se forma quando o intervalo de uma vela está completamente contido dentro da máxima e da mínima da barra anterior. Ela sinaliza indecisão de curto prazo que pode levar a um rompimento assim que o preço superar o padrão. Esta estratégia aguarda esse rompimento e então opera na direção da expansão.

Os testes indicam um retorno anual médio de aproximadamente 118%. Funciona melhor no mercado de ações.

Cada nova vela é comparada com a anterior. Se uma barra interna aparecer, o sistema marca sua máxima e mínima e observa um fechamento fora desses níveis. Um rompimento de alta abre uma posição comprada com stop abaixo da mínima do padrão, enquanto um rompimento de baixa aciona uma posição vendida com stop acima da máxima do padrão.

Caso o preço não rompa imediatamente, a estratégia gerencia as posições existentes saindo se a próxima vela se mover contra a operação além dos extremos da barra anterior.

## Detalhes

- **Critérios de entrada**: Rompimento da máxima ou mínima de uma barra interna.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Preço cruzando o extremo da vela anterior ou stop-loss.
- **Stops**: Sim, colocados além do padrão.
- **Valores padrão**:
  - `CandleType` = 5 minute
  - `StopLossPercent` = 1
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Candlestick
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

