# Estratégia X Trader V3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera cruzamentos entre duas médias móveis do preço mediano. A primeira média móvel é mais longa e deslocada, enquanto a segunda é curta. Uma posição comprada é aberta quando a primeira média móvel cruza abaixo da segunda e permanece abaixo por duas barras após ter estado acima duas barras antes. Uma posição vendida é aberta no cruzamento oposto. As posições podem ser fechadas em sinais reversos. O trading é limitado a uma janela de horário intradiário específica. Stops protetores opcionais estão disponíveis.

## Detalhes

- **Critérios de entrada**:
  - SMA do preço mediano(`Ma1Period`) cruza abaixo da SMA do preço mediano(`Ma2Period`) e permanece abaixo por duas barras ⇒ comprar quando `AllowBuy` é verdadeiro.
  - SMA do preço mediano(`Ma1Period`) cruza acima da SMA do preço mediano(`Ma2Period`) e permanece acima por duas barras ⇒ vender quando `AllowSell` é verdadeiro.
  - Tempo do candle entre `StartTime` e `EndTime`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Cruzamento oposto quando `CloseOnReverseSignal` é verdadeiro.
- **Stops**:
  - Take profit e stop loss opcionais em ticks via `TakeProfitTicks` e `StopLossTicks`.
- **Valores padrão**:
  - `Ma1Period` = 16
  - `Ma2Period` = 1
  - `TakeProfitTicks` = 150
  - `StopLossTicks` = 100
- **Filtros**:
  - Categoria: Cruzamento
  - Direção: Ambos
  - Indicadores: SMA
  - Stops: Opcional
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
