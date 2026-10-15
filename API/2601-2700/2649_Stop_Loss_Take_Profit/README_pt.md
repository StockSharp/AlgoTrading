# Estratégia Stop Loss Take Profit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este port replica o consultor especialista MetaTrader «Stop Loss Take Profit». A estratégia lança uma moeda sempre que a conta está plana e abre uma ordem de mercado na direção escolhida. Cada posição recebe imediatamente ordens de stop-loss e take-profit baseadas em pips. Se o stop for atingido, a próxima operação dobra o seu tamanho (limitado pelos limites de volume do ativo). Um take-profit reinicia o volume para o valor inicial. O comportamento espelha o dimensionamento de posição estilo martingale original enquanto usa a API de alto nível do StockSharp.

## Lógica de Trading

- **Dados de Mercado**: Usa o parâmetro `CandleType` (padrão período de 1 minuto) para impulsionar os pontos de decisão.
- **Regras de Entrada**:
  - Quando `Position == 0` e nenhuma ordem de entrada está pendente, a estratégia gera um booleano pseudoaleatório.
  - `true` abre uma posição comprada com `BuyMarket(volume)`; `false` abre uma vendida com `SellMarket(volume)`.
- **Regras de Saída**:
  - Ordens de stop-loss e take-profit protetoras são colocadas assim que o fill de entrada é recebido.
  - Uma saída por stop dobra o tamanho para a próxima operação, enquanto um take-profit o reinicia.
  - Se a distância de stop ou take-profit for definida como `0`, a respectiva ordem protetora é ignorada.
- **Gestão de Dinheiro**:
  - `InitialVolume` define o tamanho base do pedido.
  - Após uma operação perdedora, o tamanho é dobrado mas cortado para `Security.MaxVolume` quando esse valor está disponível.
  - O volume é normalizado para o `VolumeStep`, `MinVolume` e `MaxVolume` do instrumento para que as ordens permaneçam válidas.
- **Manuseio de Pips**:
  - Por padrão, a estratégia infere um pip do `PriceStep` e `Decimals` do instrumento (símbolos FX de 5 dígitos mapeiam para 0.0001).
  - Defina `PipSize` para um valor positivo para substituir a detecção automática do tamanho de pip.

## Parâmetros

| Nome | Padrão | Descrição |
| ---- | ------- | ----------- |
| `CandleType` | Velas de 1 minuto | Período usado para ativar lançamentos de moeda e entradas. |
| `StopLossPips` | 1 | Distância de stop-loss expressa em pips. `0` desabilita o stop. |
| `TakeProfitPips` | 1 | Distância de take-profit expressa em pips. `0` desabilita o take-profit. |
| `InitialVolume` | 0.01 | Volume de operação inicial. Dobrado após eventos de stop-loss e reiniciado após ganhos. |
| `PipSize` | 0 (auto) | Substituição opcional do tamanho de pip em unidades de preço absolutas. |

## Notas de Uso

- Funciona tanto no lado comprado quanto vendido e é intencionalmente neutro em direção.
- As ordens protetoras são canceladas sempre que a posição é fechada para evitar ordens obsoletas.
- O gerador aleatório é semeado com `Environment.TickCount`, o que significa que cada sessão produz sequências de operações diferentes.
- Adequado para demonstrar estratificação de risco e comportamento martingale em vez de para trading em produção sem controles de risco adicionais.
