# Estratégia da Aussie Surfer Ltd
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Aussie Surfer Ltd Strategy** é uma versão StockSharp de alto nível API do MetaTrader 5 consultor especialista "Aussie Surfer Ltd" (MQL pasta `43278`). A estratégia combina reversões rápidas de banda Bollinger com um filtro de tendência Alligator para automatizar a configuração discricionária usada no EA original. As negociações são realizadas no instrumento primário configurado para a estratégia e avaliadas em uma série de velas de 15 minutos por padrão.

## Indicadores e Dados
- **Bollinger Bandas (preço de fechamento, comprimento padrão 5, largura 2,5)** – detecta quando o mercado se estende temporariamente para fora das bandas e volta para dentro.
- **Média móvel suavizada (comprimento 21)** – reproduz a linha de "dentes" Alligator para avaliar a desaceleração da tendência.
- **Preço mediano de cada vela ((High + Low) / 2)** – alimenta o cálculo Alligator para que a inclinação corresponda à implementação original.

A estratégia assina um único fluxo de velas. Os valores dos indicadores são impulsionados apenas por velas finalizadas, garantindo que os sinais sejam gerados em dados confirmados.

## Lógica de negociação
1. **Configuração de entrada**
   - Quando a vela anterior abriu acima da banda Bollinger inferior e a vela atual abriu abaixo do valor da banda observado há duas barras, uma posição **longa** é aberta (após achatar qualquer exposição curta). Isso recria a lógica EA em que o preço ultrapassa a banda inferior e imediatamente volta para dentro.
   - Quando a vela anterior abriu abaixo da banda Bollinger superior e a vela atual abriu acima do valor da banda observado há duas barras, uma posição **curta** é aberta (após achatar qualquer exposição longa).
2. Saída baseada em **Alligator**
   - A linha dos dentes Alligator é monitorada uma e duas barras atrás. Uma posição longa é liquidada sempre que a inclinação diminui (o valor de duas barras atrás é maior que o valor de uma barra atrás). Uma posição curta fecha quando a inclinação sobe.
3. **Camadas de risco**
   - Um stop-loss e um take-profit fixos em pip são aplicados na entrada. Ambos são opcionais e podem ser desativados definindo a distância do pip como zero.
   - Um trailing stop opcional realinha o stop-loss com a máxima (para posições compradas) ou mínima (para posições vendidas) da vela anteriormente concluída menos/mais a distância do pip configurada. A lógica móvel só estará ativa se o stop-loss estiver ativado e `EnableTrailingStop` estiver definido como `true`.

## Gestão de risco
- **Stop-loss** – converte a distância do pip configurada em unidades de preço usando a etapa de preço do título.
- **Take-profit** – calculado uma vez na entrada e mantido estático até ser alcançado ou a posição ser fechada por outra regra.
- **Trailing stop** – avança o stop loss quando uma máxima mais favorável (para posições compradas) ou mínima (para posições vendidas) aparece na vela anterior.
- **Tratamento de reversão** – caso chegue um sinal enquanto uma posição oposta estiver aberta, a estratégia envia uma ordem de mercado dimensionada para reverter totalmente e estabelecer a nova exposição em uma única transação.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `OrderVolume` | Tamanho base da negociação em lotes ou contratos. | `0.30` |
| `StopLossPips` | Distância de parada protetora em pips. `0` desativa a parada. | `46` |
| `TakeProfitPips` | Distância alvo de lucro em pips. `0` desativa o alvo. | `0` |
| `EnableTrailingStop` | Permite o rastreamento baseado em pip quando um stop loss está ativo. | `true` |
| `BollingerPeriod` | Comprimento da janela Bollinger Bandas. | `5` |
| `BollingerDeviation` | Multiplicador de desvio padrão para as bandas. | `2.5` |
| `TeethPeriod` | Comprimento médio móvel suavizado para a linha dos dentes Alligator. | `21` |
| `CandleType` | Série de velas usada para cálculos (período de 15 minutos por padrão). | `15m` velas |

Todos os parâmetros numéricos incluem metadados de otimização para que possam ser ajustados através do Strategy Analyzer.

## Notas de implementação
- Somente velas concluídas são processadas; dados inacabados são ignorados para imitar a execução orientada por temporizador MetaTrader executada no início de cada nova barra.
- A lógica móvel requer uma distância de stop-loss positiva. Uma exceção será lançada durante a inicialização se a opção final estiver habilitada sem parar.
- As instâncias do indicador são desenhadas automaticamente quando uma área do gráfico está disponível, ajudando a validar se a porta StockSharp corresponde ao modelo MetaTrader.

## Uso
1. Carregue a estratégia em um terminal StockSharp ou ambiente de backtesting.
2. Configure o título de negociação e ajuste os parâmetros (especialmente as distâncias do pip) para corresponder às especificações do contrato da corretora.
3. Comece a estratégia. Ele assinará a série de velas configuradas, avaliará as entradas em cada vela finalizada e gerenciará a posição usando as regras descritas.

Para negociação ao vivo, certifique-se de que a corretora oferece suporte a ordens de mercado e que o símbolo `PriceStep` esteja disponível para que as conversões de pip sejam precisas.
