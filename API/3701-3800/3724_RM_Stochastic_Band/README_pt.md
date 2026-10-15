# Estratégia de banda RM Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **RM Stochastic Band Strategy** é uma versão StockSharp de alto nível do MetaTrader consultor especialista *EA RM Stochastic Band* de Ronny Maheza. A estratégia observa três osciladores estocásticos calculados em diferentes intervalos de tempo (base, médio e alto) e abre negociações somente quando todos os três confirmam condições de sobrevenda ou sobrecompra. Na entrada, os níveis de saída são derivados do Average True Range (ATR) medido no período de tempo mais alto, replicando os níveis de stop-loss e take-profit baseados em ATR no Expert Advisor original. Filtros de execução adicionais incluem um valor mínimo configurável do portfólio como proxy de margem e um controle de spread que adapta sua tolerância dependendo do spread observado.

## Lógica principal

1. **Confirmação estocástica de vários períodos de tempo**
   - O prazo de execução primário (padrão M1) gera o sinal de negociação.
   - Os prazos de confirmação (padrão M5 e M15) devem estar de acordo com a direção do sinal.
   - Uma negociação é aberta apenas se os valores %K estocásticos em todos os três períodos de tempo estiverem simultaneamente abaixo do nível de sobrevenda (configuração longa) ou acima do nível de sobrecompra (configuração curta).

2. **Saídas baseadas em volatilidade com ATR**
   - ATR é calculado no período mais alto (padrão M15).
   - Stop loss = `entry price ± ATR * StopLossMultiplier`.
   - Lucro = `entry price ± ATR * TakeProfitMultiplier`.
   - Os preços são monitorados nas velas do período base; se uma vela tocar qualquer um dos níveis, a posição será fechada no mercado.

3. **Filtros de execução e segurança**
   - Os pedidos são ignorados quando o spread observado (BestAsk - BestBid) excede o limite adaptativo. Se o spread for superior ao limite padrão, o limite mais flexível da conta em centavos será aplicado, refletindo a lógica EA de origem.
   - A negociação é bloqueada enquanto o valor da carteira estiver abaixo de `MinMargin`.
   - Apenas uma posição pode ser aberta por vez e nenhuma nova negociação será iniciada se existirem ordens ativas.

## Indicadores e Assinaturas

| Indicador | Prazo | Objetivo |
|-----------|-----------|---------|
| Stochastic Oscilador | Período base (padrão 1 minuto) | Gera sinal primário (apenas %K é usado). |
| Stochastic Oscilador | Período intermediário (padrão 5 minutos) | Confirma a direção do sinal primário. |
| Stochastic Oscilador | Prazo alto (padrão 15 minutos) | Fornece confirmação de longo prazo. |
| Faixa Verdadeira Média | Prazo alto (padrão 15 minutos) | Define distâncias de stop-loss e take-profit ajustadas à volatilidade. |

Os dados de nível 1 são assinados para capturar o melhor lance e solicitar avaliação de spread.

## Regras de entrada

- **Configuração longa**: todos os três valores estocásticos de %K estão abaixo de `OversoldLevel`. Quando acionada, a estratégia compra no volume de mercado `OrderVolume` e armazena níveis de saída baseados em ATR.
- **Configuração curta**: todos os três valores estocásticos de %K estão acima de `OverboughtLevel`. Uma venda no mercado é executada com o mesmo manuseio de volume.

## Regras de saída

- **Stop-loss**: Para posições longas, saia quando a mínima da vela tocar `entry - ATR * StopLossMultiplier`. Para posições vendidas, saia quando a máxima da vela atingir `entry + ATR * StopLossMultiplier`.
- **Take-profit**: Para posições longas, saia quando a máxima da vela tocar `entry + ATR * TakeProfitMultiplier`. Para posições curtas, saia quando a mínima da vela atingir `entry - ATR * TakeProfitMultiplier`.
- Após uma saída, a parada interna e os espaços reservados de destino são apagados para que o próximo sinal possa recalcular novos níveis.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `OrderVolume` | Volume de cada ordem de mercado. | 0,1 |
| `StochasticLength` | %K período de retrospectiva. | 5 |
| `StochasticSmoothing` | Suavização aplicada a %K. | 3 |
| `StochasticSignalLength` | %D comprimento. | 3 |
| `AtrPeriod` | Período ATR no período máximo. | 14 |
| `StopLossMultiplier` | Multiplicador ATR para o stop loss. | 1,5 |
| `TakeProfitMultiplier` | Multiplicador de ATR para o lucro. | 3,0 |
| `MinMargin` | Valor mínimo do portfólio exigido para negociação. | 100 |
| `MaxSpreadStandard` | Limite de spread para contas padrão. | 3 |
| `MaxSpreadCent` | Limite de spread usado quando o spread atual já excede o limite padrão. | 10 |
| `OversoldLevel` | Limite de sobrevenda para %K estocástico. | 20 |
| `OverboughtLevel` | Limite de sobrecompra para %K estocástico. | 80 |
| `BaseCandleType` | Período primário (velas padrão de 1 minuto). | 1 minuto |
| `MidCandleType` | Prazo de confirmação (velas padrão de 5 minutos). | 5 minutos |
| `HighCandleType` | Confirmação + período de ATR (velas padrão de 15 minutos). | 15 minutos |

Todos os parâmetros suportam intervalos de otimização idênticos às entradas MetaTrader quando apropriado.

## Notas de implementação

- A estratégia usa `SubscribeCandles(...).BindEx(...)` para obter valores de indicadores estritamente por meio do API de alto nível, conforme exigido pelas diretrizes do projeto.
- O spread é calculado a partir de atualizações ao vivo de nível 1; sem dados de compra/venda, a negociação permanece desativada, garantindo uma operação segura em feeds de dados que não fornecem cotações.
- As posições são gerenciadas exclusivamente por meio de ordens de mercado, refletindo o EA original que dependia de entradas de mercado com níveis de stop-loss e take-profit pré-calculados.
- Não há ponto de equilíbrio ou lógica final porque a fonte MQL não implementou esses recursos, apesar de ter parâmetros de entrada relacionados.

## Dicas de uso

1. Anexe a estratégia à segurança desejada e garanta que os dados de Nível 1 (oferta/oferta) estejam disponíveis para filtragem de spread adequada.
2. Ajuste os limites estocásticos e os multiplicadores ATR para corresponder ao perfil de volatilidade do instrumento alvo.
3. Ao otimizar, considere testar diferentes combinações de prazos se o mercado que você negocia tiver ciclos dominantes diferentes da estrutura original M1/M5/M15.
