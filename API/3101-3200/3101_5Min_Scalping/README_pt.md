# Estratégia de Scalping de 5 Minutos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Portagem do expert advisor MT4 **"5MIN SCALPING" (MQL ID 22828)** para a API de alto nível do StockSharp. A estratégia procura configurações de rompimento rápido no período principal e as confirma com momentum de período superior e direção do MACD mensal antes de entrar no mercado.

- **Categoria:** Scalping de rompimento / Momentum
- **Plataforma original:** MetaTrader 4
- **Requisitos de dados:** Feed de ticks ou velas para todos os períodos configurados (padrão 5 minutos, 30 minutos, 1 mês)

## Lógica de trading

1. **Filtro de tendência.** Duas médias móveis lineares ponderadas (LWMA) com comprimentos configuráveis (padrão 6 e 85) definem a tendência predominante. Posições compradas requerem que a LWMA rápida permaneça acima da LWMA lenta, posições vendidas requerem a relação oposta.
2. **Filtro de estrutura multi-barra.** O trio interno LWMA (comprimentos 8, 13, 21) é avaliado nas últimas 20 velas concluídas. O algoritmo imita a função `scalper()` da versão MQL:
   - Configuração altista: cada barra dentro do ciclo deve satisfazer `LWMA8 > LWMA13 > LWMA21`, a mínima da vela recua para a pilha de médias móveis, e o fechamento atual rompe acima da máxima mais alta das 5 velas anteriores.
   - Configuração baixista: lógica espelhada usando máximas penetrando a pilha LWMA e o fechamento atual rompendo abaixo da mínima mais baixa das 5 velas anteriores.
3. **Guarda de sobreposição.** Uma condição de sobreposição menor (`Low[2] < High[1]` para compras, `Low[1] < High[2]` para vendas) previne entradas em picos isolados.
4. **Confirmação de momentum.** Um indicador `Momentum` de período superior (padrão velas de 30 minutos, comprimento 14) deve mostrar que pelo menos um dos últimos três valores se desvia da linha de base de 100 mais do que os limites configurados (0.3 por padrão).
5. **Alinhamento do MACD macro.** Um histograma mensal `MACD(12, 26, 9)` é calculado via `MovingAverageConvergenceDivergenceSignal`. Trades comprados requerem que a linha MACD esteja acima da linha de sinal, trades vendidos requerem o oposto.
6. **Agregação de posição.** Entrar na direção oposta fecha a exposição existente primeiro e imediatamente abre o novo trade com o volume configurado.

## Gestão do risco

- **Alvos estáticos.** Níveis opcionais de take-profit e stop-loss em pips (convertidos internamente usando o `PriceStep` do instrumento).
- **Módulo de break-even.** Quando ativado, o stop é movido para a entrada ± offset assim que o preço percorre uma quantidade configurável de pips.
- **Trailing stop.** Trailing stop opcional que segue a posição a uma distância fixa em pips assim que o mercado avança.
- **Saídas manuais.** Todas as saídas são tratadas dentro da estratégia sem colocar ordens de proteção, refletindo o comportamento original do EA.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `CandleType` | Período de 5 minutos | Período principal onde os rompimentos são detectados. |
| `MomentumCandleType` | Período de 30 minutos | Tipo de vela usado para o filtro de momentum de período superior. |
| `MacroMacdCandleType` | Período de 1 mês | Tipo de vela usado para confirmação MACD de longo prazo. |
| `FastMaLength` | 6 | Comprimento do filtro de tendência LWMA rápido. |
| `SlowMaLength` | 85 | Comprimento do filtro de tendência LWMA lento. |
| `MomentumLength` | 14 | Período de lookback para o indicador de momentum. |
| `MomentumBuyThreshold` | 0.3 | Desvio mínimo |Momentum-100| necessário para confirmar trades comprados. |
| `MomentumSellThreshold` | 0.3 | Desvio mínimo |Momentum-100| necessário para confirmar trades vendidos. |
| `TakeProfitPips` | 50 | Distância do take-profit em pips. Definir como 0 para desativar. |
| `StopLossPips` | 20 | Distância do stop-loss em pips. Definir como 0 para desativar. |
| `TrailingStopPips` | 40 | Distância do trailing stop em pips. Efetivo apenas quando `EnableTrailing` é verdadeiro. |
| `EnableTrailing` | true | Ativa ou desativa a lógica do trailing stop. |
| `EnableBreakEven` | true | Habilita o gerenciamento automático de break-even. |
| `BreakEvenTriggerPips` | 30 | Lucro em pips necessário antes de o stop ser movido para o break-even. |
| `BreakEvenOffsetPips` | 30 | Buffer extra (em pips) adicionado ao mover o stop para o break-even. |
| `TradeVolume` | 1 | Volume de ordem usado para entradas. |

## Uso

1. Adicionar a estratégia ao projeto StockSharp e vinculá-la ao instrumento desejado.
2. Garantir que os dados históricos para todos os tipos de velas configurados estejam disponíveis antes de iniciar a estratégia.
3. Configurar o volume, períodos e limites de acordo com a volatilidade do instrumento negociado.
4. Iniciar a estratégia. Ela assinará todas as séries de velas necessárias, desenhará indicadores no gráfico (quando disponível) e gerenciará entradas/saídas automaticamente.

## Diferenças em relação ao EA original

- Os módulos de trailing baseados em dinheiro (`Take_Profit_In_Money`, `TRAIL_PROFIT_IN_MONEY2`) e o stop de equidade da versão MQL não foram portados. O risco é gerenciado através de distâncias em pips.
- O escalonamento de lote estilo martingale (`Lots * MathPow(LotExponent, CountTrades())`) não está implementado. Ajustar `TradeVolume` manualmente se necessário dimensionamento de posição dinâmico.
- Alertas de e-mail/notificação do código original são omitidos. Usar a infraestrutura de notificações do StockSharp se necessário.
- A estratégia depende do `PriceStep` do instrumento para converter distâncias em pips. Validar que os metadados do instrumento estejam corretamente preenchidos no ambiente de conexão.
