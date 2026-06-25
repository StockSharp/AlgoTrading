# Estratégia Arttrader v1.5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Arttrader v1.5 é um sistema de seguimento de tendência convertido do consultor especialista original do MetaTrader 5. Combina um filtro de inclinação de média móvel exponencial (EMA) de prazo superior com um modelo de entrada de ação de preço de curto prazo. A versão StockSharp mantém o comportamento de gestão de risco do código-fonte, incluindo o tratamento rigoroso de grandes lacunas de velas, janelas de tempo para ordens e saídas de emergência baseadas na distância do preço.

Dois fluxos de velas são usados simultaneamente:

- **Velas de trading** (padrão 5 minutos) geram entradas, saídas e todos os filtros baseados em preço.
- **Velas de tendência** (padrão 1 hora) alimentam a EMA que mede a inclinação da tendência do prazo superior.

A estratégia negocia um único instrumento com posições líquidas. Quando um sinal oposto aparece, a exposição existente é zerada e uma nova ordem de mercado é enviada na direção do sinal.

## Lógica de sinais
1. **Filtro de inclinação EMA**
   - A EMA horária do preço de abertura da vela deve ter uma inclinação entre `SlopeSmall` e `SlopeLarge` (convertida em unidades de preço pelo valor do ponto do instrumento).
   - Negociações compradas requerem inclinação positiva, negociações vendidas requerem inclinação negativa.
2. **Temporização intra-barra**
   - Os sinais são considerados apenas após `MinutesBegin` minutos terem decorrido na hora atual, espelhando a verificação `TimeCurrent()` do MT5.
3. **Confirmação de ação de preço**
   - Entradas compradas precisam de uma vela de baixa ou neutra que feche perto de sua mínima (`SlipBegin` define a distância aceitável).
   - Entradas vendidas precisam de uma vela de alta ou neutra que feche perto de sua máxima.
4. **Filtros de salto**
   - Qualquer lacuna de abertura de uma única vela maior que `BigJump` (em pontos ajustados) nas últimas seis velas cancela os sinais comprados e vendidos.
   - Qualquer lacuna de abertura de duas velas maior que `DoubleJump` também cancela o sinal, evitando negociações durante picos voláteis.

## Lógica de saída
1. **Stop inteligente temporizado**
   - Um preço de entrada de referência é armazenado com um deslocamento opcional `Adjust` para emular o tratamento de spread do MT5.
   - Quando o fechamento se move contra a posição em pelo menos `StopLoss`, a estratégia aguarda até que `MinutesEnd` minutos da hora tenham passado e a vela mostre um padrão de recuperação (requisito `SlipEnd`). Satisfeito, a posição é fechada a mercado.
2. **Stop de emergência**
   - Se o intervalo da vela tocar `EmergencyLoss` de distância do preço de preenchimento registrado, a posição é fechada imediatamente. Isso espelha o stop-loss do lado do corretor do especialista original.
3. **Take-profit**
   - Uma vela que toca a distância `TakeProfit` aciona uma saída imediata.
4. **Salvaguarda de volume**
   - Se o volume total da vela anterior não exceder `MinVolume`, a posição atual é fechada para evitar negociação em períodos ilíquidos.

## Parâmetros
| Nome | Padrão | Descrição |
|------|---------|-------------|
| `Volume` | 1 | Volume da ordem de mercado. Usado tanto para entradas quanto para reverter uma posição oposta. |
| `EmaPeriod` | 11 | Comprimento da EMA calculada no período de tendência (fonte de preço de abertura). |
| `BigJump` | 30 | Lacuna máxima permitida de uma única vela entre aberturas consecutivas (convertida usando o passo de preço). |
| `DoubleJump` | 55 | Lacuna máxima permitida entre aberturas separadas por uma vela. |
| `StopLoss` | 20 | Perda em pontos que habilita a lógica de saída temporizada. |
| `EmergencyLoss` | 50 | Distância de stop fixo desde a entrada, executado imediatamente quando atingido. |
| `TakeProfit` | 25 | Distância do alvo de lucro desde a entrada. |
| `SlopeSmall` | 5 | Inclinação EMA mínima (positiva para comprados, negativa para vendidos) necessária para novas negociações. |
| `SlopeLarge` | 8 | Magnitude máxima de inclinação EMA permitida para negociações. |
| `MinutesBegin` | 25 | Minutos após o topo da hora antes de novas entradas serem avaliadas. |
| `MinutesEnd` | 25 | Minutos após o topo da hora antes que a lógica de stop temporizado possa sair. |
| `SlipBegin` | 0 | Distância máxima entre o fechamento da vela e o extremo usado durante a validação de entrada. |
| `SlipEnd` | 0 | Distância máxima entre o fechamento da vela e o extremo durante a confirmação do stop. |
| `MinVolume` | 0 | Volume mínimo da vela anterior; valores mais baixos forçam uma saída. |
| `Adjust` | 1 | Ajuste aplicado ao armazenar o preço de referência de entrada interna. |
| `CandleType` | Período de 5 minutos | Velas de trading usadas para entradas e saídas. |
| `TrendCandleType` | Período de 1 hora | Tipo de vela que alimenta o filtro de inclinação EMA. |

Todos os parâmetros baseados em preço são multiplicados pelo valor do ponto do instrumento. Para símbolos FX com três ou cinco decimais, o código multiplica automaticamente o ponto por dez, correspondendo ao tratamento de pip usado na versão MetaTrader.

## Notas de implementação
- Ambos os métodos de entrada de mercado chamam `BuyMarket` ou `SellMarket` com volume suficiente para reverter uma posição existente quando necessário.
- A estratégia usa `SubscribeCandles` duas vezes apenas quando os tipos de vela de trading e tendência diferem. Quando ambos os parâmetros são iguais, uma única assinatura alimenta a EMA e a lógica de trade.
- O stop de emergência e o gerenciamento de take-profit são implementados em processo porque o StockSharp não anexa automaticamente ordens de proteção a execuções de mercado.
- A API de alto nível é usada em todo momento (assinaturas `Bind`, `StartProtection`, helpers de gráfico), garantindo que o código permaneça conciso e siga as convenções do repositório.

## Dicas de uso
- Ajuste `MinutesBegin` e `MinutesEnd` para instrumentos com diferentes estruturas de sessão. Os valores padrão são projetados para instrumentos com ritmo horário como os principais pares Forex.
- Aumente `MinVolume` em mercados onde secas repentinas de volume coincidem com preenchimentos ruins (por exemplo, commodities fora dos horários de pit).
- Como os filtros de salto dependem de apenas seis velas, certifique-se de que o período de trading não seja muito grande; caso contrário, o filtro pode ser muito permissivo.
- O filtro de inclinação EMA é sensível ao valor do ponto do instrumento. Sempre verifique se `BigJump`, `StopLoss` e parâmetros similares estão corretamente escalados para o símbolo selecionado.
