# CBC_WS_RSI Estratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **CBC_WS_RSI Estratégia** é uma implementação StockSharp de alto nível do consultor especialista MQL5 que combina os padrões de velas "Três Soldados Brancos" e "Três Corvos Negros" com confirmação RSI. A estratégia se concentra na identificação de fortes reversões de múltiplas velas e só entra em uma negociação quando a dinâmica do mercado, medida por RSI, concorda com o padrão. As saídas são controladas por cruzamentos de limites RSI e gerenciamento de risco opcional por meio de proteções de stop-loss e take-profit.

A estratégia assina uma série de velas configuráveis e processa dados exclusivamente em velas totalmente formadas. Toda a lógica é implementada usando o API (`SubscribeCandles().Bind(...)`) de alto nível do StockSharp sem acesso direto aos buffers do indicador.

## Lógica de negociação
### Configuração longa
1. Detecta três velas de alta consecutivas formando o padrão **Três Soldados Brancos**:
   - Cada vela fecha acima da sua abertura.
   - Cada fechamento é maior que o fechamento anterior.
   - A segunda e a terceira velas abrem dentro do corpo da vela anterior.
2. Confirma que o valor RSI da vela atual está **abaixo ou igual ao nível de confirmação longa** (padrão 40).
3. Se a conta estiver estável, a estratégia compra `Volume` lotes no mercado. Se existir uma posição curta, ela será coberta antes de abrir uma nova posição longa.

### Configuração curta
1. Detecta três velas de baixa consecutivas formando o padrão **Três Corvos Negros**:
   - Cada vela fecha abaixo de sua abertura.
   - Cada fechamento é inferior ao fechamento anterior.
   - A segunda e a terceira velas abrem dentro do corpo da vela anterior.
2. Confirma que o valor RSI da vela atual está **acima ou igual ao nível de confirmação curta** (padrão 60).
3. Se a conta estiver estável, a estratégia vende `Volume` lotes no mercado. Se existir uma posição longa, ela será fechada antes de abrir uma nova posição curta.

### Regras de saída
- **Close Longs:** RSI cruzando abaixo do nível de saída superior (padrão 70) ou do nível de saída inferior (padrão 30).
- **Fechar Shorts:** RSI cruzando acima do nível de saída inferior (padrão 30) ou do nível de saída superior (padrão 70).
- **Proteção:** Valores opcionais de stop-loss e take-profit podem ser definidos como porcentagens do preço de entrada. Quando diferentes de zero, eles são gerenciados via `StartProtection`.

Todas as condições de saída usam os dois valores RSI mais recentes para detectar um cruzamento de nível, garantindo que as negociações sejam fechadas assim que o impulso contradizer a posição ativa.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `CandleType` | Tipo de dados da vela e prazo para assinatura. | Período de 1 hora |
| `RsiPeriod` | RSI período usado para confirmação. | 37 |
| `LongConfirmationLevel` | Valor máximo RSI que permite uma entrada longa. | 40 |
| `ShortConfirmationLevel` | Valor mínimo RSI que permite uma entrada curta. | 60 |
| `LowerExitLevel` | Nível RSI usado para detectar reversão de impulso próximo ao território de sobrevenda. | 30 |
| `UpperExitLevel` | Nível RSI usado para detectar reversão de impulso próximo ao território de sobrecompra. | 70 |
| `StopLossPercent` | Stop-loss opcional em percentagem; 0 desativa a proteção. | 1 |
| `TakeProfitPercent` | Take-profit opcional em porcentagem; 0 desativa a proteção. | 2 |

Todos os parâmetros numéricos podem ser otimizados por meio do otimizador integrado graças ao `SetCanOptimize(true)`.

## Visualização
Quando uma área do gráfico está disponível, a estratégia desenha:
- A série de velas selecionada.
- O indicador RSI.
- Negociações executadas, facilitando a inspeção de detecções e saídas de padrões.

## Notas de uso
- Certifique-se de que `Volume` esteja configurado antes de iniciar a estratégia.
- Funciona em qualquer instrumento que suporte dados de vela OHLC.
- A lógica de detecção de padrões filtra velas semelhantes a doji, exigindo corpos de velas diferentes de zero.
- As confirmações RSI protegem contra sinais falsos durante reversões fracas, mantendo a estratégia alinhada com o impulso.
