# Estratégia RSI Eraser
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia RSI Eraser é um port direto do consultor especialista de MetaTrader 5 criado por Vladimir Karputov.
Usa candles horários para avaliar o índice de força relativa (RSI) e busca entradas de estilo de reversão à média quando o momentum muda em torno do nível neutro 50.
As negociações são filtradas pela faixa de alta/baixa diária anterior e a estratégia dimensiona cada posição de acordo com uma porcentagem fixa do patrimônio da conta.

## Lógica principal

- **Período primário** – Candles de 1 hora impulsionam os cálculos do indicador e os sinais de negociação.
- **Período de filtro** – Candles diários completados fornecem a máxima e mínima de ontem que condicionam as entradas.
- **Indicador** – RSI clássico com comprimento de retrospectiva configurável.
- **Direção** – Comprado quando RSI > nível neutro, vendido quando RSI < nível neutro.
- **Dimensionamento de risco** – O volume de posição é derivado da distância entre a entrada e o stop multiplicada pela porcentagem de risco escolhida.

## Critérios de entrada

1. Aguardar o candle horário fechar e calcular o RSI.
2. Verificar se pelo menos um candle diário completado está disponível.
3. **Configuração comprada**
   - Valor de RSI estritamente acima do limiar neutro (padrão 50).
   - O nível de stop proposto (entrada − distância de stop-loss) não deve estar abaixo da mínima de ontem menos o buffer diário.
   - A entrada é rejeitada se uma negociação comprada já foi aberta no mesmo dia calendário.
4. **Configuração vendida**
   - Valor de RSI estritamente abaixo do limiar neutro.
   - O nível de stop proposto (entrada + distância de stop-loss) não deve estar acima da máxima de ontem mais o buffer diário.
   - A entrada é rejeitada se uma negociação vendida já foi aberta no mesmo dia calendário.
5. Quando as condições são satisfeitas, a estratégia envia uma ordem de mercado com volume baseado em risco.
   Se há uma posição oposta, a nova ordem a fecha e inverte a direção em uma única operação.

## Critérios de saída

- O stop-loss e take-profit iniciais são calculados a partir da distância de pip configurada e do multiplicador.
- A estratégia monitora continuamente os candles completados:
  - Uma negociação comprada sai quando o preço cai até o stop ou sobe até o nível de take-profit.
  - Uma negociação vendida sai quando o preço sobe até o stop ou cai até o nível de take-profit.
- Proteção de ponto de equilíbrio: assim que o preço se move a favor em pelo menos a distância de stop original,
  o stop é elevado (ou rebaixado para vendidos) para o preço exato de entrada.
- Quando nenhuma posição está aberta, todos os níveis de risco são limpos para evitar valores desatualizados.

## Gestão de risco

- `RiskPercent` define a fração do patrimônio do portfólio a arriscar em cada negociação.
- O tamanho da posição é calculado como `risk_amount / stop_distance` com um fallback para o `Volume` base da estratégia quando as informações de patrimônio não estão disponíveis.
- O buffer diário adiciona uma margem de segurança extra em torno da faixa de ontem, prevenindo negociações que colocariam stops muito perto dos extremos de oscilação recentes.

## Valores padrão

- `RsiPeriod` = 14
- `RsiNeutralLevel` = 50
- `StopLossPips` = 50
- `TakeProfitMultiplier` = 3
- `DailyBufferPips` = 10
- `RiskPercent` = 5%
- `CandleType` = 1 hora
- `DailyCandleType` = 1 dia

## Notas de implementação

- A estratégia assina feeds de candles horários e diários usando a API de alto nível do StockSharp.
- Todos os comentários e mensagens de log são fornecidos em inglês para coincidir com as diretrizes do repositório.
- O tratamento de ponto de equilíbrio e a restrição de uma negociação por dia seguem a lógica original do MetaTrader.
