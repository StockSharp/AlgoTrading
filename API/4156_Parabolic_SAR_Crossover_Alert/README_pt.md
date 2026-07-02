# Parabolic SAR Estratégia de alerta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é a porta StockSharp do MetaTrader 4 consultor especialista `pSAR_alert.mq4`. O script original apenas reproduzia um som de alerta sempre que o indicador Parabolic SAR mudava de um lado para o outro do preço. A conversão mantém a mesma lógica de decisão, mas transforma os alertas em ordens de mercado reais, permitindo que o sinal seja negociado automaticamente dentro de StockSharp.

## Lógica de negociação
- A estratégia assina o tipo de vela configurado e executa um indicador Parabolic SAR com o fator de aceleração clássico (0,02) e aceleração máxima (0,2) por padrão.
- Para cada vela finalizada, a estratégia compara o valor Parabolic SAR com o fechamento da vela e também rastreia o contexto da vela anterior.
- Quando a vela anterior fechou abaixo de SAR, mas o fechamento atual está acima, o indicador caiu para baixo e uma posição longa foi aberta (ou uma posição curta existente foi revertida).
- Quando a vela anterior fechou acima de SAR, mas o fechamento atual está abaixo, o indicador subiu e uma posição curta foi aberta (ou uma posição longa existente foi revertida).
- O volume de negociação é calculado como o volume da estratégia base mais a posição atual absoluta, garantindo que as reversões saiam totalmente da negociação anterior antes de entrar na nova direção.
- `StartProtection()` é executado no início, então StockSharp gerencia automaticamente desconexões inesperadas enquanto as posições estão abertas.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `AccelerationFactor` | 0,02 | Etapa de aceleração inicial que controla a rapidez com que Parabolic SAR segue os movimentos de preços. |
| `MaxAccelerationFactor` | 0,2 | Limite superior para a etapa de aceleração, limitando a agressividade com que o SAR acelera durante tendências fortes. |
| `CandleType` | Período de 5 minutos | Tipo de dados de mercado utilizado para atualizações de indicadores; altere-o para alternar entre intervalos de tempo ou outras representações de velas. |

Todos os parâmetros são expostos por meio de `StrategyParam<T>` para que possam ser otimizados diretamente no StockSharp Designer.

## Fluxo de trabalho do indicador
1. Assine o fluxo de vela configurado via `SubscribeCandles`.
2. Vincule o fluxo a um indicador `ParabolicSar` para que StockSharp o atualize automaticamente.
3. Dentro do retorno de chamada de ligação, compare o valor SAR atual com o preço de fechamento e retenha o par SAR/close anterior.
4. Detecte cruzamentos avaliando se o SAR se moveu de cima para baixo no fechamento (mudança de alta) ou de baixo para cima (mudança de baixa).
5. Execute `BuyMarket` ou `SellMarket` adequadamente e registre mensagens descritivas para cada negociação.

## Notas práticas
- Como a estratégia reage apenas ao fechamento confirmado da vela, ela evita sinais prematuros que podem desaparecer antes do término da barra.
- Os parâmetros padrão reproduzem o comportamento do script MQL, mas você pode ajustá-los para adaptar a sensibilidade do Parabolic SAR.
- Vincular a estratégia a instrumentos que apresentem tendências limpas; a lógica de inversão SAR tem melhor desempenho quando as reversões são decisivas em vez de barulhentas.
- A visualização do gráfico é ativada automaticamente quando uma área do gráfico está disponível: velas, o indicador Parabolic SAR e negociações próprias são desenhadas para inspeção rápida.

## Arquivos
- `CS/ParabolicSarCrossoverAlertStrategy.cs` – Implementação da estratégia em C#.
- `README.md` – Esta documentação em inglês.
- `README_zh.md` – Tradução chinesa da documentação.
- `README_ru.md` – Tradução russa da documentação.
