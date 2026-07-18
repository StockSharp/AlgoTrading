# Williams Estratégia de índice direcional percentual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Williams Estratégia de índice direcional percentual** recria o MetaTrader 5 especialista "Mt5 Williams % Índice direcional EA" usando o StockSharp de alto nível do API. Ele combina o oscilador Williams %R com o índice direcional médio (ADX) para identificar mudanças de impulso e, em seguida, depende do índice de fluxo de dinheiro (MFI) e do oscilador Stochastic para sair das negociações. A implementação processa apenas velas finalizadas e usa ligações de indicadores para que cada decisão seja baseada na última barra concluída.

## Lógica de negociação
1. **Alinhamento de tendências**
   - Williams %R deve estar subindo para negociações longas ou caindo para negociações curtas. A estratégia compara os valores das duas barras finalizadas anteriormente para avaliar a inclinação do momento.
   - O componente de movimento direcional do ADX (`+DI - -DI`) deve ter cruzado zero na última barra fechada: uma transição negativa para positiva confirma o impulso de alta, enquanto uma transição positiva para negativa confirma o impulso de baixa.
2. **Regras de inscrição**
   - Se ambas as condições de alta forem satisfeitas e a posição atual for plana ou curta, a estratégia abre uma ordem de compra de mercado.
   - Se ambas as condições de baixa forem satisfeitas e a posição atual for plana ou longa, a estratégia abre uma ordem de venda no mercado.
   - Quando os sinais longos e curtos aparecem simultaneamente (raro, mas possível em valores idênticos), a negociação é ignorada para evitar instruções conflitantes.
3. **Regras de saída**
   - As posições longas fecham quando o valor MFI de duas barras atrás excede o nível de sobrecompra ou a linha principal Stochastic forma um padrão de vale local (`K[−2] > K[−1] < K[0]`).
   - As posições curtas fecham quando o valor MFI de duas barras atrás cai abaixo do nível de sobrevenda espelhado (`100 - level`) ou a linha principal Stochastic forma um padrão de pico local (`K[−2] < K[−1] > K[0]`).
4. **Tratamento de riscos**
   - A conversão mantém a mecânica de entrada e saída do consultor especialista original. Os recursos de stop-loss e trailing da fonte MQL não são reproduzidos; o controle de risco deve ser gerenciado externamente ou adicionado por meio de proteções StockSharp, se necessário.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `Candle Type` | Prazo para todos os cálculos dos indicadores. | Período de 15 minutos |
| `Williams %R Period` | Período de lookback usado no oscilador Williams %R. | 42 |
| `Directional Period` | Período para cálculos ADX (afeta +DI/−DI). | 20 |
| `MFI Period` | Length of the Money Flow Index. | 19 |
| `MFI Level` | Limite de sobrecompra usado para acionar saídas. O nível de sobrevenda é calculado como `100 - value`. | 79 |
| `Stochastic %K` | Período %K do oscilador estocástico. | 22 |
| `Stochastic %D` | Período %D do oscilador estocástico. | 16 |
| `Stochastic Smoothing` | Suavização adicional ("desaceleração") aplicada ao oscilador estocástico. | 21 |

Todos os parâmetros são expostos como valores `StrategyParam`, para que possam ser otimizados ou ajustados por meio da GUI do StockSharp.

## Notas de uso
- Vincule a estratégia a qualquer instrumento e defina um volume apropriado antes de começar.
- A estratégia processa apenas velas concluídas (`CandleStates.Finished`), garantindo que os valores dos indicadores sejam finais.
- A renderização do gráfico está habilitada: Williams %R, ADX, MFI, Stochastic e as negociações executadas são plotadas quando uma área do gráfico está disponível.
- Para recriar o comportamento original do MT5 em relação ao gerenciamento de stop, considere adicionar `StartProtection` ou lógica de risco personalizada conforme necessário.

## Diferenças da versão MQL
- A conversão StockSharp usa ligações de indicadores em vez de cópia manual do buffer, mas as verificações lógicas, incluindo validação cruzada de zero e padrões de barras múltiplas, seguem o consultor especialista MT5.
- Filtros de sessão, lógica de nova tentativa e gerenciamento de trailing stop do código MQL são intencionalmente omitidos para focar no mecanismo de sinal principal solicitado para esta conversão.
