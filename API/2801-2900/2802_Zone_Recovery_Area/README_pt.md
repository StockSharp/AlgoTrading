# Estratégia de Área de Recuperação por Zonas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Área de Recuperação por Zonas** é uma conversão direta do expert advisor do MetaTrader "Zone Recovery Area" (pacote `MQL/20266`). Recria a lógica de hedging original sobre a API de alto nível do StockSharp e adiciona parametrização exaustiva para que o comportamento possa ser ajustado sem tocar no código. A estratégia combina um filtro de tendência com uma grade de recuperação de compra/venda alternante: uma vez que uma operação primária é aberta, posições adicionais são empilhadas sempre que o preço sai ou re-entra na zona predefinida, criando uma cesta coberta que visa recuperar reduções flutuantes.

Características principais:
- Usa um cruzamento de média móvel simples rápida/lenta junto com um filtro MACD mensal para definir o viés de trading.
- Implementa a técnica de recuperação por zonas: a primeira operação estabelece um preço base, e ordens de hedge alternantes são disparadas sempre que o mercado cruza o limite da zona ou retorna ao nível base.
- Fornece controles de lucro baseados em dinheiro, porcentagem e trailing para sair da cesta assim que lucro suficiente for assegurado.
- Permite tanto o dimensionamento de posição multiplicativo (estilo martingale) quanto aditivo para cada passo de recuperação.

## Dados de mercado e indicadores
- **Candles principais:** período definido pelo usuário (padrão 30 minutos) para entradas e gerenciamento de recuperação.
- **Candles mensais:** construídos a partir de períodos menores se necessário; usados para calcular os valores de MACD (12/26/9).
- **Indicadores:**
  - Média Móvel Simples (rápida e lenta) no período principal.
  - Convergência/Divergência de Médias Móviles com linha de sinal no período mensal.

## Lógica de trading
1. **Validação de tendência**
   - Aguardar até que ambas as SMAs e o MACD mensal estejam completamente formados.
   - Uma configuração de alta requer que a SMA rápida esteja abaixo da lenta na barra anterior enquanto a linha MACD mensal está acima de seu sinal.
   - Uma configuração de baixa requer que a SMA rápida esteja acima da lenta na barra anterior enquanto a linha MACD mensal está abaixo de seu sinal.
2. **Inicialização do ciclo**
   - Quando uma configuração de alta (baixa) é detectada, abrir a posição comprada (vendida) inicial com `InitialVolume` e armazenar o preço de entrada como base do ciclo.
   - Redefinir contadores internos e rastreamento de lucro para o novo ciclo.
3. **Motor de recuperação por zonas**
   - Definir dois níveis críticos: o **limite da zona** (`ZoneRecoveryPips`) do preço base e o **nível de take-profit** (`TakeProfitPips`) na direção favorável.
   - Enquanto o ciclo estiver ativo, monitorar cada candle completado:
     - Se o preço atingir o nível de take-profit, fechar toda a exposição líquida e terminar o ciclo.
     - Se os alvos de lucro em dinheiro ou porcentagem forem atingidos, ou o bloqueio de lucro trailing for acionado, fechar o ciclo.
     - Caso contrário, avaliar se um novo hedge é necessário:
       - Para ciclos comprados: abrir um vendido adicional quando o preço cai abaixo de `base - zone`, e abrir um comprado adicional quando o preço volta acima do preço base.
       - Para ciclos vendidos: abrir um comprado adicional quando o preço sobe acima de `base + zone`, e abrir um vendido adicional quando o preço retorna abaixo do preço base.
     - A direção do hedge alterna automaticamente; o tamanho da próxima ordem é determinado multiplicando o volume anterior ou adicionando um incremento fixo.
   - O número de operações por cesta é limitado por `MaxTrades`.
4. **Gerenciamento de lucro**
   - `UseMoneyTakeProfit`: fechar a cesta assim que o lucro não realizado atingir o valor de moeda configurado.
   - `UsePercentTakeProfit`: fechar a cesta assim que o lucro não realizado igualar o percentual especificado do valor do portfólio.
   - `EnableTrailing`: uma vez que o lucro excede `TrailingStartProfit`, rastrear o pico e sair do ciclo se o lucro cair por `TrailingDrawdown`.

Todas as ordens são colocadas usando os helpers de alto nível do StockSharp (`BuyMarket`/`SellMarket`), o que mantém a implementação consistente com as melhores práticas do framework.

## Parâmetros
| Nome | Padrão | Descrição |
| ---- | ------- | --------- |
| `CandleType` | Candles de 30 minutos | Período para entradas e monitoramento de recuperação. |
| `MonthlyCandleType` | Candles de 30 dias | Período superior usado para construir o filtro de tendência MACD. |
| `FastMaLength` | 20 | Período da SMA rápida. |
| `SlowMaLength` | 200 | Período da SMA lenta. |
| `TakeProfitPips` | 150 | Distância do preço base para fechar toda a cesta em lucro. |
| `ZoneRecoveryPips` | 50 | Meia-largura da zona de hedge em torno do preço base. |
| `InitialVolume` | 1 | Volume da primeira operação em cada ciclo. |
| `UseVolumeMultiplier` | true | Se habilitado, cada novo hedge multiplica o volume anterior. |
| `VolumeMultiplier` | 2 | Fator aplicado ao volume anterior quando `UseVolumeMultiplier` é true. |
| `VolumeIncrement` | 0.5 | Incremento de volume aditivo quando `UseVolumeMultiplier` é false. |
| `MaxTrades` | 6 | Número máximo de operações por ciclo de recuperação (incluindo a inicial). |
| `UseMoneyTakeProfit` | false | Habilitar take-profit baseado em dinheiro. |
| `MoneyTakeProfit` | 40 | Alvo de lucro em moeda da conta. |
| `UsePercentTakeProfit` | false | Habilitar take-profit baseado em porcentagem. |
| `PercentTakeProfit` | 5 | Alvo de lucro como porcentagem do valor do portfólio. |
| `EnableTrailing` | true | Habilitar proteção de lucro trailing. |
| `TrailingStartProfit` | 40 | Limite de lucro necessário antes do trailing se tornar ativo. |
| `TrailingDrawdown` | 10 | Retrocesso de lucro permitido assim que o trailing está ativo. |

> **Conversão de pips:** `TakeProfitPips` e `ZoneRecoveryPips` são convertidos em offsets de preço usando o passo de preço do instrumento. Certifique-se de que o instrumento negociado fornece valores corretos de `PriceStep` e `StepPrice`.

## Notas de uso
1. Adicione a estratégia à sua solução StockSharp (Designer, API, Runner, etc.).
2. Atribua o instrumento e portfólio desejados antes de iniciar.
3. Ajuste os parâmetros para corresponder à volatilidade do instrumento, redução aceitável e tamanho da conta.
4. Certifique-se de que há dados históricos suficientes para que tanto as SMAs quanto o MACD mensal possam se aquecer antes da primeira operação.
5. Monitore cuidadosamente o uso de margem: os passos de recuperação podem aumentar rapidamente a exposição, especialmente quando o multiplicador está habilitado.

## Gestão de risco e considerações
- As técnicas de recuperação por zonas/martingale podem acumular posições muito grandes em mercados em tendência. Sempre teste com configurações conservadoras e use o parâmetro `MaxTrades` para limitar o risco.
- Como o StockSharp mantém uma única posição líquida, o cálculo interno de lucro replica o PnL da cesta usando informações de preço/passo do instrumento. Valide os valores com o feed de dados do seu broker.
- Os alvos de dinheiro e porcentagem dependem da valorização do portfólio. Ao fazer backtesting ou paper trading, certifique-se de que o modelo de portfólio fornece `BeginValue`/`CurrentValue` corretamente.
- Nenhum stop-loss duro automático é usado; o risco é gerenciado por meio da mecânica de recuperação. Considere combinar a estratégia com stops externos no nível do portfólio.

## Arquivos
- `CS/ZoneRecoveryAreaStrategy.cs` — implementação da estratégia.
- `README.md` — documentação em inglês (este arquivo).
- `README_ru.md` — documentação em russo.
- `README_zh.md` — documentação em chinês.
