# Estratégia Original do Universo 3.0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o consultor especialista **Universum_3_0** MQL4 original usando o StockSharp API de alto nível.
Ele combina um modelo simples de entrada de limite DeMarker com uma regra de dimensionamento de posição semelhante a martingale que se adapta
tamanho do lote após perder negociações.

## Lógica de negociação

- **Indicador**: oscilador DeMarker clássico com período configurável.
- **Geração de Sinal**:
  - Abra uma posição longa quando `DeMarker > 0.5` estiver no fechamento de uma vela concluída.
  - Abra uma posição curta quando `DeMarker < 0.5` estiver no fechamento de uma vela concluída.
  - Apenas uma posição pode estar ativa por vez; novos sinais são ignorados enquanto uma negociação está aberta.
- **Gerenciamento de saídas**:
  - Os níveis protetores de stop-loss e take-profit são anexados usando compensações de preços absolutos medidas em pontos.
  - As posições são fechadas automaticamente por estes níveis de proteção; a estratégia não muda imediatamente.
- **Gerenciamento de dinheiro**:
  - Após uma negociação lucrativa, o volume é redefinido para o lote base.
  - Após uma negociação perdida, o volume é multiplicado por `(TakeProfitPoints + StopLossPoints) / (TakeProfitPoints - SpreadPoints)`.
  - O valor do spread é obtido das cotações ao vivo do Nível 1 e convertido em "pontos" usando a precisão do símbolo.
  - São contabilizadas perdas consecutivas; atingir o limite interrompe a estratégia de emular a proteção contra perdas original.
  - A configuração `FastOptimize = true` desativa a regra de dimensionamento adaptável e sempre usa o lote base, o que acelera as otimizações.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `CandleType` | Período usado para cálculos do DeMarker. | Período de 1 minuto |
| `DemarkerPeriod` | Período de retrospectiva do oscilador DeMarker. | `10` |
| `TakeProfitPoints` | Distância de take-profit expressa em pontos (convertida internamente em preço absoluto). | `50` |
| `StopLossPoints` | Distância de stop-loss expressa em pontos. | `50` |
| `BaseVolume` | Volume de negociação inicial utilizado após cada negociação lucrativa. | `1` |
| `LossesLimit` | Número máximo de perdas consecutivas antes da estratégia parar. | `1,000,000` |
| `FastOptimize` | Quando `true` desativa o dimensionamento adaptativo para passes rápidos de otimização. | `true` |

## Notas de implementação

- Os dados do Nível 1 são necessários para estimar o spread atual e replicar o multiplicador do lote original.
- A normalização do volume respeita o volume mínimo, o volume máximo e o tamanho do passo do instrumento.
- As compensações de stop-loss e take-profit adaptam-se automaticamente a instrumentos de 3/5 dígitos ajustando o tamanho do ponto.
- A visualização do gráfico traça velas, o indicador DeMarker e negociações executadas para facilitar a validação.

## Dicas de uso

1. Forneça dados de oferta/venda de nível 1, além de velas, para garantir que o multiplicador baseado em spread funcione corretamente.
2. Use `FastOptimize = true` durante pesquisas grosseiras de parâmetros e, em seguida, desative-o para backtests precisos e negociações ao vivo.
3. Monitore o contador de perdas consecutivas ao usar multiplicadores agressivos para evitar exceder os limites do corretor.
4. Ajuste `TakeProfitPoints` e `StopLossPoints` para corresponder ao símbolo original ou ao seu perfil de risco antes de negociar ao vivo.
